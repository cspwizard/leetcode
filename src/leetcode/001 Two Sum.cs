// Two Sum simple bruteforce
using System;

namespace leetcode.TwoSum
{
    public class Solution
    {
        public int[] TwoSum(int[] nums, int target)
        {
            var count = nums.Length;
            for (int i = 0; i < count; i++)
            {
                for (int j = i + 1; j < count; j++)
                {
                    if (nums[i] + nums[j] == target)
                    {
                        return new int[] { i, j };
                    }
                }
            }

            throw new ArgumentException();
        }
    }
}