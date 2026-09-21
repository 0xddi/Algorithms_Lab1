namespace Algorithms.Core;

public sealed class HeapSortAlgorithm : Algorithm<double[]>
{
    public override string Name => "Heap Sort";
    public override TimeComplexity Complexity => TimeComplexity.Linearithmic;

    public HeapSortAlgorithm(double[] data) : base(data) { }

    protected override void ExecuteCore(double[] input)
    {
        int n = input.Length;

        // 1. Построение max-кучи (от последнего родителя к корню)
        for (int i = n / 2 - 1; i >= 0; i--)
        {
            Heapify(input, n, i);
        }

        // 2. Поочерёдно переносим максимум в конец и восстанавливаем кучу
        for (int end = n - 1; end > 0; end--)
        {
            (input[0], input[end]) = (input[end], input[0]);
            Heapify(input, end, 0);
        }
    }

    // Просеивание вниз (итеративно, без рекурсии)
    private static void Heapify(double[] arr, int heapSize, int root)
    {
        while (true)
        {
            int largest = root;
            int left = 2 * root + 1;
            int right = 2 * root + 2;

            if (left < heapSize && arr[left] > arr[largest]) largest = left;
            if (right < heapSize && arr[right] > arr[largest]) largest = right;

            if (largest == root) return;

            (arr[root], arr[largest]) = (arr[largest], arr[root]);
            root = largest;
        }
    }

    protected override double[] CloneInput(double[] input) => (double[])input.Clone();

    public override string GetDescription()
        => "Строит двоичную max-кучу в массиве, затем по одному извлекает максимум в конец. O(n log n) всегда, O(1) доп. памяти.";
}