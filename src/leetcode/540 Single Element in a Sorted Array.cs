using System;
using System.Collections.Generic;
using System.Text;

namespace leetcode
{
    /// <summary>
    /// Your runtime beats 84.38 % of csharp submissions.
    /// </summary>
    public class _540_Single_Element_in_a_Sorted_Array
    {
        /// <summary>
        /// Given a sorted array consisting of only integers where every element appears twice except for one element which appears once. Find this single element that appears only once.
        /// </summary>
        /// <param name="nums"></param>
        /// <returns></returns>
        public int SingleNonDuplicate(int[] nums)
        {
            return SingleNonDuplicate(nums, 0, nums.Length);

        }

        private int SingleNonDuplicate(int[] nums, int start, int length)
        {
            var midCeil = length / 2 + 1;
            var mid = midCeil - 1;

            if (nums[start + mid] == nums[start + midCeil])
            {
                --mid;
                --midCeil;
            }

            if (length == 3)
            {
                if (mid == 0)
                {
                    return nums[start];
                }
                else
                {
                    return nums[start + midCeil];
                }
            }

            if (mid % 2 != 0)
            {
                return SingleNonDuplicate(nums, start + mid + 1, length - mid - 1);
            }
            else
            {
                return SingleNonDuplicate(nums, start, mid + 1);
            }
        }
    }
}
