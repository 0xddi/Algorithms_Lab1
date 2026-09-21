namespace Algorithms.Core.PowFunctionAlgorithms;

/// <summary>
/// Классический алгоритм быстрого возведения в степень.
/// Соответствует блок-схеме "Рис. 4".
/// </summary>
public sealed class ClassicFastPowerAlgorithm : Algorithm<(double x, int n)>
{
    public override string Name => "Классическое быстрое возведение в степень";
    public override TimeComplexity Complexity => TimeComplexity.Logarithmic;

    public ClassicFastPowerAlgorithm((double x, int n) data) : base(data) { }

    protected override void ExecuteCore((double x, int n) input)
    {
        double c = input.x;
        double f = 1;
        int k = input.n;

        while (k != 0)
        {
            if (k % 2 == 0)
            {
                c = c * c;
                k = k / 2;   // k = k div 2
            }
            else
            {
                f = f * c;
                k = k - 1;
            }
            Steps++;
        }
    }

    protected override (double x, int n) CloneInput((double x, int n) input) => input;

    public override string GetDescription() => "Классический быстрый алгоритм, сокращающий показатель степени вдвое на четных шагах.";
}