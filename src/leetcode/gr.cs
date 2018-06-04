using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;

namespace GeocoderResolver.GeocoderResolver
{
    internal class GoogleApiClient : IGoogleApiClient
    {
        public GoogleApiClient(GeocoderConfig config)
        {
            HttpClient.DefaultRequestHeaders.ConnectionClose = false;
            HttpClient.Timeout = TimeSpan.FromMilliseconds(config.GeocoderApiRequestTimeOut);

            Log = LogManager.GetLogger(nameof(GoogleApiClient));
        }

        public enum Status
        {
            Succeed,
            ResolutionFailed,
            TooManyRequsts
        }

        public Logger Log { get; }

        private static HttpClient HttpClient { get; } = new HttpClient(new HttpClientHandler { Proxy = null, UseProxy = false, MaxConnectionsPerServer = 10000 });

        public async Task<ResolveResult> ResolveCoordinatesAsync(string apiKey, string address)
        {
            var requestUrl = $"https://maps.googleapis.com/maps/api/geocode/json?address={WebUtility.UrlEncode(address)}&key={WebUtility.UrlEncode(apiKey)}";
            var message = await HttpClient.GetAsync(requestUrl).ConfigureAwait(false);
            string rawResponse = string.Empty;

            if (message.IsSuccessStatusCode)
            {
                try
                {
                    rawResponse = await message.Content.ReadAsStringAsync().ConfigureAwait(false);

                    var response = JsonConvert.DeserializeObject<GeocodeApiResponse>(rawResponse);
                    if (response.Status == Const.Ok)
                    {
                        return new ResolveResult
                        {
                            Location = new Location { Lat = response.Results[0].Geometry.Location.Lat, Long = response.Results[0].Geometry.Location.Lng },
                            Status = Status.Succeed
                        };
                    }

                    if (response.Status == Const.ZeroResults)
                    {
                        return new ResolveResult
                        {
                            Status = Status.Succeed
                        };
                    }

                    if (response.Status == Const.OverQueryLimit || response.Status == Const.RequestDenied)
                    {
                        return new ResolveResult { Status = Status.TooManyRequsts };
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"URL: {requestUrl} Raw response: {rawResponse}");
                }
            }

            return new ResolveResult { Status = Status.ResolutionFailed };
        }

        public class ResolveResult
        {
            public Location Location { get; set; }

            public Status Status { get; set; }
        }

        private static class Const
        {
            public const string InvalidRequst = "INVALID_REQUEST";
            public const string Ok = "OK";
            public const string OverQueryLimit = "OVER_QUERY_LIMIT";
            public const string RequestDenied = "REQUEST_DENIED";
            public const string UnknownError = "UNKNOWN_ERROR";
            public const string ZeroResults = "ZERO_RESULTS";
        }

        private class GeocodeApiResponse
        {
            [JsonProperty("results")]
            public ApiResult[] Results { get; set; }

            [JsonProperty("status")]
            public string Status { get; set; }

            public class ApiResult
            {
                [JsonProperty("geometry")]
                public Geometry Geometry { get; set; }
            }

            public class Geometry
            {
                [JsonProperty("location")]
                public Location Location { get; set; }
            }

            public class Location
            {
                [JsonProperty("lat")]
                public double Lat { get; set; }

                [JsonProperty("lng")]
                public double Lng { get; set; }
            }
        }
    }
}


using NLog;
using SimpleInjector;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace GeocoderResolver.GeocoderResolver
{
    internal class LocationResolverWorker
    {
        public LocationResolverWorker(IGadgetRepository gadgetRepository,
            IContactRepository contactRepository,
            IGoogleApiClient googleApiClient,
            Container container)
        {
            GadgetRepository = gadgetRepository;
            ContactRepository = contactRepository;
            GoogleApiClient = googleApiClient;
            ServiceProvider = container;
        }

        public Logger Logger { get; } = LogManager.GetLogger(nameof(LocationResolverWorker));

        public Container ServiceProvider { get; }

        private GeocoderConfig Config => ServiceProvider.GetInstance<GeocoderConfig>();

        private IContactRepository ContactRepository { get; }

        private IGadgetRepository GadgetRepository { get; }

        private IGoogleApiClient GoogleApiClient { get; }

        private ConcurrentDictionary<int, Task> RunningTasks { get; } = new ConcurrentDictionary<int, Task>();

        public IPAddress[] GetAllLocalIPv4()
        {
            List<IPAddress> ipAddrList = new List<IPAddress>();
            foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (item.NetworkInterfaceType == NetworkInterfaceType.Ethernet && item.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            ipAddrList.Add(ip.Address);
                        }
                    }
                }
            }
            return ipAddrList.ToArray();
        }

        public Task ProcessBatch(GoogleApiKeyThrottling apiKeyThrottling, int gadgetId, int servicesCount, int serviceIndex, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                try
                {
                    var contacts = ContactRepository.GetUnprocessedContacts(gadgetId, servicesCount, serviceIndex, Config.GeocoderApiBatchSize);
                    var failCount = 0;
                    var processedCount = 0;
                    foreach (var c in contacts)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        var res = await GoogleApiClient.ResolveCoordinatesAsync(apiKeyThrottling.ApiKey, c.Address).ConfigureAwait(false);
                        ++processedCount;

                        if (res.Status == GeocoderResolver.GoogleApiClient.Status.ResolutionFailed)
                        {
                            ++failCount;
                        }

                        if (res.Status == GeocoderResolver.GoogleApiClient.Status.TooManyRequsts || processedCount - failCount > Config.ThrottleOnFailureCount)
                        {
                            var retryCount = ++apiKeyThrottling.RetryCount;
                            var suspendPeriod = retryCount * retryCount * 10;
                            suspendPeriod = suspendPeriod < Config.GeocoderApiThrottlingMaxPeriod ? suspendPeriod : Config.GeocoderApiThrottlingMaxPeriod;
                            apiKeyThrottling.LastActive = DateTime.UtcNow;

                            apiKeyThrottling.ThrottlingTime = apiKeyThrottling.ThrottlingTime.Add(TimeSpan.FromSeconds(suspendPeriod));

                            GadgetRepository.UpsertApiKeyThrottling(apiKeyThrottling);

                            break;
                        }

                        GadgetRepository.RemoveApiKeyTimeout(apiKeyThrottling.ApiKey);

                        if (res.Status == GeocoderResolver.GoogleApiClient.Status.Succeed)
                        {
                            c.IsProcessed = true;
                            c.Location = res.Location;

                            ContactRepository.UpdateContactLocation(c);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }

                RunningTasks.TryRemove(gadgetId, out var t);
            });
        }

        public void StartProcessingRun(CancellationToken cancelationToken)
        {
            if (cancelationToken.IsCancellationRequested)
            {
                return;
            }

            var keysTimeOuts = GadgetRepository.GetApiKeysThrottling().ToDictionary(k => k.ApiKey);

            var gadgets = GadgetRepository.GetGadgetsSettings();

            var gadgetsToProcess = gadgets.Where(g => !keysTimeOuts.ContainsKey(g.GoogleApiGeocodingKey)
                    || keysTimeOuts[g.GoogleApiGeocodingKey].LastActive + keysTimeOuts[g.GoogleApiGeocodingKey].ThrottlingTime < DateTime.UtcNow);

            var serviceIndex = 0;
            var servicesCount = 1;
            try
            {
                var localAddresses = GetAllLocalIPv4();
#pragma warning disable CS0618 // Type or member is obsolete. New version doesn't work...
                var hostEntry = Dns.Resolve($"ml-{nameof(MemberLocation.GeocoderResolver)}");
#pragma warning restore CS0618 // Type or member is obsolete
                servicesCount = hostEntry.AddressList.Length > 0 ? hostEntry.AddressList.Length : 1;

                var sortedList = hostEntry.AddressList.OrderBy(c => c);

                foreach (var s in sortedList)
                {
                    if (localAddresses.Any(l => l.Equals(s)))
                    {
                        break;
                    }

                    ++serviceIndex;
                }
            }
            catch
            {
            }

            foreach (var gadget in gadgetsToProcess)
            {
                if (!RunningTasks.ContainsKey(gadget.Id))
                {
                    var kt = keysTimeOuts.ContainsKey(gadget.GoogleApiGeocodingKey) ? keysTimeOuts[gadget.GoogleApiGeocodingKey] : new GoogleApiKeyThrottling { ApiKey = gadget.GoogleApiGeocodingKey, ThrottlingTime = TimeSpan.Zero };
                    RunningTasks.TryAdd(gadget.Id, ProcessBatch(kt, gadget.Id, servicesCount, serviceIndex, cancelationToken));
                }
            }
        }
    }
}
