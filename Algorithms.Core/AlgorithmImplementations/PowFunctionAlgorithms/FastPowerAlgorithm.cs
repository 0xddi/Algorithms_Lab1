namespace Algorithms.Core.PowFunctionAlgorithms;

/// <summary>
/// Быстрый алгоритм возведения в степень.
/// Соответствует блок-схеме "Рис. 3".
/// </summary>
public sealed class FastPowerAlgorithm : Algorithm<(double x, int n)>
{
    public override string Name => "Быстрое возведение в степень";
    public override TimeComplexity Complexity => TimeComplexity.Logarithmic;

    public FastPowerAlgorithm((double x, int n) data) : base(data) { }

    protected override void ExecuteCore((double x, int n) input)
    {
        double c = input.x;
        int k = input.n;
        double f;

        if (k % 2 == 1)
        {
            f = c;
        }
        else
        {
            f = 1;
        }

        while (true)
        {
            k = k / 2;       // k = k div 2
            c = c * c;
            
            if (k % 2 == 1)
            {
                f = f * c;
            }
            
            if (k == 0)
            {
                break;
            }
        }
    }

    protected override (double x, int n) CloneInput((double x, int n) input) => input;

    public override string GetDescription() => "Оптимизированный итеративный алгоритм, использующий двоичное разложение показателя степени.";
}