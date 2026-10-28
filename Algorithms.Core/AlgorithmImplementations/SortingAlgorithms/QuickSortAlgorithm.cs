namespace Algorithms.Core;

public sealed class QuickSortAlgorithm : Algorithm<double[]>
{
    public override string Name => "Quick Sort";
    public override TimeComplexity Complexity => TimeComplexity.Linearithmic;

    public QuickSortAlgorithm(double[] data) : base(data) { }

    protected override void ExecuteCore(double[] input)
    {
        QuickSort(input, 0, input.Length - 1);
    }

    private void QuickSort(double[] arr, int left, int right)
    {
        if (left < right)
        {
            int pivotIndex = Partition(arr, left, right);
            QuickSort(arr, left, pivotIndex - 1);
            QuickSort(arr, pivotIndex + 1, right);
        }
    }

    private int Partition(double[] arr, int left, int right)
    {
        // Median-of-three pivot: on sorted input the last element is the maximum,
        // which degrades Lomuto partitioning to O(n^2)
        int mid = left + (right - left) / 2;
        if (arr[mid] < arr[left]) (arr[mid], arr[left]) = (arr[left], arr[mid]);
        if (arr[right] < arr[left]) (arr[right], arr[left]) = (arr[left], arr[right]);
        if (arr[mid] < arr[right]) (arr[mid], arr[right]) = (arr[right], arr[mid]);

        double pivot = arr[right];
        int i = left - 1;
        for (int j = left; j < right; j++)
        {
            if (arr[j] <= pivot)
            {
                i++;
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
        }
        (arr[i + 1], arr[right]) = (arr[right], arr[i + 1]);
        return i + 1;
    }

    protected override double[] CloneInput(double[] input) => (double[])input.Clone();

    public override string GetDescription() => "Рекурсивный алгоритм сортировки 'разделяй и властвуй'.";
}