using System;

namespace Algorithms.Core;

/// <summary>
/// Реализация алгоритма сортировки пузырьком для целочисленного массива.
/// </summary>
public sealed class BubbleSortAlgorithm : Algorithm<double[]>
{
    /// <inheritdoc />
    public override string Name => "Bubble Sort";

    /// <inheritdoc />
    public override TimeComplexity Complexity => TimeComplexity.Quadratic;

    /// <summary>
    /// Инициализирует алгоритм сортировки переданным массивом.
    /// </summary>
    /// <param name="data">Массив, который нужно отсортировать.</param>
    public BubbleSortAlgorithm(double[] data) : base(data) 
    { 
    }

    /// <inheritdoc />
    protected override void ExecuteCore(double[] array)
    {
        int n = array.Length;
        for (int i = 0; i < n - 1; i++)
        {
            for (int j = 0; j < n - i - 1; j++)
            {
                if (array[j] > array[j + 1])
                {
                    (array[j], array[j + 1]) = (array[j + 1], array[j]);
                }
            }
        }
    }

    /// <inheritdoc />
    protected override double[] CloneInput(double[] input)
    {
        // Для одномерных массивов значимых типов (int, double) .Clone() делает точную изолированную копию.
        return (double[])input.Clone();
    }

    /// <inheritdoc />
    public override string GetDescription()
    {
        return "Простая сортировка сравнением. Итерируется по массиву, меняя местами соседние элементы, находящиеся в неправильном порядке.";
    }
}