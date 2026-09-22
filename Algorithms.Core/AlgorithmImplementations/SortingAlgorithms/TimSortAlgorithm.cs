namespace Algorithms.Core;

public sealed class TimSortAlgorithm : Algorithm<double[]>
{
    private const int Run = 32;

    public override string Name => "Tim Sort";
    public override TimeComplexity Complexity => TimeComplexity.Linearithmic;

    public TimSortAlgorithm(double[] data) : base(data) { }

    protected override void ExecuteCore(double[] input) // 
    {
        int n = input.Length;

        // Сортируем подмассивы размером Run сортировкой вставками
        for (int i = 0; i < n; i += Run)
        {
            InsertionSort(input, i, Math.Min(i + Run - 1, n - 1));
        }

        // Сливаем отсортированные подмассивы
        for (int size = Run; size < n; size = 2 * size)
        {
            for (int left = 0; left < n; left += 2 * size)
            {
                int mid = left + size - 1;
                int right = Math.Min(left + 2 * size - 1, n - 1);

                if (mid < right)
                {
                    Merge(input, left, mid, right);
                }
            }
        }
    }

    private void InsertionSort(double[] arr, int left, int right)
    {
        for (int i = left + 1; i <= right; i++)
        {
            double temp = arr[i];
            int j = i - 1;
            while (j >= left && arr[j] > temp)
            {
                arr[j + 1] = arr[j];
                j--;
            }
            arr[j + 1] = temp;
        }
    }

    private void Merge(double[] arr, int left, int mid, int right)
    {
        int len1 = mid - left + 1, len2 = right - mid;
        double[] leftArr = new double[len1];
        double[] rightArr = new double[len2];

        Array.Copy(arr, left, leftArr, 0, len1);
        Array.Copy(arr, mid + 1, rightArr, 0, len2);

        int i = 0, j = 0, k = left;
        while (i < len1 && j < len2)
        {
            if (leftArr[i] <= rightArr[j]) arr[k++] = leftArr[i++];
            else arr[k++] = rightArr[j++];
        }
        while (i < len1) arr[k++] = leftArr[i++];
        while (j < len2) arr[k++] = rightArr[j++];
    }

    protected override double[] CloneInput(double[] input) => (double[])input.Clone();

    public override string GetDescription() => "Гибридный алгоритм (Сортировка вставками + Сортировка слиянием). Оптимизирован для реальных данных.";
}