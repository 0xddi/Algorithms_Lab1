namespace Algorithms.Core;

public sealed class SelectionSortAlgorithm : Algorithm<double[]>
{
    public override string Name => "Selection Sort";
    public override TimeComplexity Complexity => TimeComplexity.Quadratic; // Убедитесь, что Quadratic есть в вашем enum

    public SelectionSortAlgorithm(double[] data) : base(data) { }

    protected override void ExecuteCore(double[] array)
    {
        int n = array.Length;
        for (int i = 0; i < n - 1; i++)
        {
            int minIndex = i;
            for (int j = i + 1; j < n; j++)
            {
                if (array[j] < array[minIndex])
                {
                    minIndex = j;
                }
            }
            
            if (minIndex != i)
            {
                (array[i], array[minIndex]) = (array[minIndex], array[i]);
            }
        }
    }

    protected override double[] CloneInput(double[] input)
    {
        return (double[])input.Clone();
    }

    public override string GetDescription()
    {
        return "Сортировка выбором. Находит минимальный элемент в неотсортированной части и меняет его местами с первым неотсортированным элементом.";
    }
}