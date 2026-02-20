static int CanSplitArray(int[] a)
{
    if (a.Length == 0) return -1;

    int leftSum = 0; int rightSum = 0;
    int left = 0; int right = a.Length - 1;

    while (left <= right)
    {
        if (leftSum <= rightSum)
        {
            leftSum += a[left];
            left++;
        }
        else
        {
            rightSum += a[right];
            right--;
        }
    }

    // return pointer (1st element of right side)
    return (leftSum == rightSum) ? left : -1;
}