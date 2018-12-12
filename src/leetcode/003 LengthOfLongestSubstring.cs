using System.Collections.Generic;
using Xunit;

namespace leetcode
{
    /// <summary>
    /// Your runtime beats 71.20 %  of csharp submissions.
    /// </summary>
    public class _003_LengthOfLongestSubstring
    {
        public int LengthOfLongestSubstring(string s)
        {
            int maxLen = 0, last = 0;

            var charMap = new Dictionary<char, int>();
            var indexChar = new Dictionary<int, char>();

            for (var i = 0; i < s.Length; i++)
            {
                var c = s[i];

                if (charMap.TryGetValue(c, out int j))
                {
                    if (charMap.Count > maxLen)
                    {
                        maxLen = charMap.Count;
                    }

                    while (last < j + 1)
                    {
                        charMap.Remove(s[last++]);
                    }
                }

                charMap[c] = i;
            }

            if (charMap.Count > maxLen)
            {
                return charMap.Count;
            }

            return maxLen;
        }

        [Fact]
        public void Validate()
        {
            Assert.Equal(3, LengthOfLongestSubstring("abcabcbb"));
            Assert.Equal(1, LengthOfLongestSubstring("bbbbb"));
            Assert.Equal(5, LengthOfLongestSubstring("abcde"));
            Assert.Equal(5, LengthOfLongestSubstring("abcdea"));
            Assert.Equal(3, LengthOfLongestSubstring("pwwkew"));
            Assert.Equal(2, LengthOfLongestSubstring("aab"));
            Assert.Equal(3, LengthOfLongestSubstring("dvdf"));
            Assert.Equal(7, LengthOfLongestSubstring("bpfbhmipx"));
        }
    }
}