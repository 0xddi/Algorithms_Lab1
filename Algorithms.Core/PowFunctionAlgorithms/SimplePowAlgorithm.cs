namespace Algorithms.Core.PowFunctionAlgorithms;

/// <summary>
/// Простой алгоритм возведения в степень последовательным умножением.
/// Соответствует блок-схеме "Рис. 1".
/// </summary>
public sealed class SimplePowerAlgorithm : Algorithm<(double x, int n)>
{
    public override string Name => "Простое возведение в степень";
    public override TimeComplexity Complexity => TimeComplexity.Linear;

    public SimplePowerAlgorithm((double x, int n) data) : base(data) { }

    protected override void ExecuteCore((double x, int n) input)
    {
        double f = 1;
        int k = 0;
        
        while (k < input.n)
        {
            f = f * input.x;
            k = k + 1;
        }
    }

    protected override (double x, int n) CloneInput((double x, int n) input) => input;

    public override string GetDescription() => "Вычисляет степень путем n-кратного умножения основания (O(n)).";
}