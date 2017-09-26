using System.Collections.Generic;

namespace leetcode
{
    internal class _003_LengthOfLongestSubstring
    {
        public int LengthOfLongestSubstring(string s)
        {
            int maxLen = 0, counter = 0;
            ListItem newHead, head = null, tail = new ListItem();
            
            var dic = new Dictionary<char, ListItem>();

            for (var i = 0; i < s.Length; i++)
            {
                var c = s[i];

                if (dic.TryGetValue(c, out newHead))
                {
                    if (counter > maxLen)
                    {
                        maxLen = counter;
                    }

                    while (head != newHead.Next)
                    {
                        dic.Remove(head.Value);
                        --counter;
                        head = head.Next;
                    }
                }

                ++counter;
                tail.Next = new ListItem(c);
                dic.Add(c, tail.Next);
                tail = tail.Next;
                if (head == null)
                {
                    head = tail;
                }
            }

            if (counter > maxLen)
            {
                maxLen = counter;
            }

            return maxLen;
        }

        private class ListItem
        {
            public ListItem()
            {
            }

            public ListItem(char value) : this()
            {
                Value = value;
            }

            public ListItem Next { get; set; }

            public char Value { get; set; }
        }
    }
}