namespace Algorithms.Core.PowFunctionAlgorithms;

/// <summary>
/// Рекурсивный алгоритм возведения в степень.
/// Соответствует блок-схеме "Рис. 2".
/// </summary>
public sealed class RecursivePowerAlgorithm : Algorithm<(double x, int n)>
{
    public override string Name => "Рекурсивное возведение в степень";
    public override TimeComplexity Complexity => TimeComplexity.Logarithmic;

    public RecursivePowerAlgorithm((double x, int n) data) : base(data) { }

    protected override void ExecuteCore((double x, int n) input)
    {
        RecPow(input.x, input.n);
    }

    private double RecPow(double x, int n)
    {
        if (n == 0)
        {
            return 1;
        }

        double f = RecPow(x, n / 2); // Целочисленное деление (div 2)

        if (n % 2 == 1) // Проверка на нечетность (mod 2 = 1)
        {
            return f * f * x;
        }
        else
        {
            return f * f;
        }
    }

    protected override (double x, int n) CloneInput((double x, int n) input) => input;

    public override string GetDescription() => "Использует рекурсивное разбиение показателя степени надвое (O(log n)).";
}