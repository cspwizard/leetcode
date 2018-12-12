using Xunit;

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
            int n = nums.Length, lo = 0, hi = n / 2;
            while (lo < hi)
            {
                int m = (lo + hi) / 2;
                if (nums[2 * m] != nums[2 * m + 1]) hi = m;
                else lo = m + 1;
            }
            return nums[2 * lo];
        }
        
        [Fact]
        public void Validate()
        {
            Assert.Equal(3, SingleNonDuplicate(new[] { 1, 1, 2, 2, 3, 4, 4 }));
            Assert.Equal(2, SingleNonDuplicate(new[] { 1, 1, 2, 3, 3, 4, 4, 8, 8 }));
            Assert.Equal(2, SingleNonDuplicate(new[] { 1, 1, 2 }));
            Assert.Equal(2, SingleNonDuplicate(new[] { 2, 3, 3 }));
            Assert.Equal(10, SingleNonDuplicate(new[] { 3, 3, 7, 7, 10, 11, 11 }));
        }
    }
}
