using System;

namespace Algorithms.Core;

/// <summary>
/// Реализация алгоритма сортировки пузырьком для целочисленного массива.
/// </summary>
public sealed class BubbleSortAlgorithm : Algorithm<int[]>
{
    /// <inheritdoc />
    public override string Name => "Bubble Sort";

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="BubbleSortAlgorithm"/>.
    /// </summary>
    public BubbleSortAlgorithm() : base(TimeComplexity.Quadratic) { }

    /// <inheritdoc />
    protected override void ExecuteCore(int[] array)
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
    protected override int[] CloneInput(int[] input)
    {
        return (int[])input.Clone();
    }

    /// <inheritdoc />
    public override string GetDescription()
    {
        return "Простая сортировка сравнением. Итерируется по массива, меняя местами соседние элементы, находящиеся в неправильном порядке.";
    }
}