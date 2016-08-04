// Add Two Numbers solution //Your runtime beats 95.29% of csharpsubmissions
namespace leetcode.AddTwoNumbers
{
    // Definition for singly-linked list.
    public class ListNode
    {
        public ListNode next;
        public int val;

        public ListNode(int x)
        {
            val = x;
        }
    }

    public class Solution
    {
        public ListNode AddTwoNumbers(ListNode l1, ListNode l2)
        {
            ListNode resultHead = new ListNode(0);
            var digitNode = resultHead;
            var lastNode = digitNode;

            var t = 0;
            while (l1 != null && l2 != null)
            {
                var sum = l1.val + l2.val + t;

                if (sum / (float)10 >= 1)
                {
                    sum = sum % 10;
                    t = 1;
                }
                else
                {
                    t = 0;
                }

                digitNode = new ListNode(sum);

                lastNode.next = digitNode;
                lastNode = digitNode;

                l1 = l1.next;
                l2 = l2.next;
            }

            TransferRest(l1, ref digitNode, ref lastNode, ref t);

            TransferRest(l2, ref digitNode, ref lastNode, ref t);

            if (t != 0)
            {
                digitNode = new ListNode(t);

                if (lastNode != null)
                {
                    lastNode.next = digitNode;
                }
            }
            return resultHead.next;
        }

        private static void TransferRest(ListNode l, ref ListNode digitNode, ref ListNode lastNode, ref int t)
        {
            while (l != null)
            {
                var sum = l.val + t;

                if (sum / (float)10 >= 1)
                {
                    sum = sum % 10;
                    t = 1;
                }
                else
                {
                    t = 0;
                }

                digitNode = new ListNode(sum);

                lastNode.next = digitNode;
                lastNode = digitNode;

                l = l.next;
            }
        }
    }
}