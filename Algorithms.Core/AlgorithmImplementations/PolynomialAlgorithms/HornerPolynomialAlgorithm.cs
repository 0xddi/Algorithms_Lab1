namespace Algorithms.Core.PolynomialAlgorithms;

public sealed class HornerPolynomialAlgorithm : Algorithm<double[]>
{
    public override string Name => "Метод Горнера";
    public override TimeComplexity Complexity => TimeComplexity.Linear;

    public HornerPolynomialAlgorithm(double[] data) : base(data) { }

    protected override void ExecuteCore(double[] input)
    {
        double result = 0;
        double x = 1.5;
        
        for (int i = input.Length - 1; i >= 0; i--)
        {
            result = result * x + input[i];
        }
    }

    protected override double[] CloneInput(double[] input) => input;

    public override string GetDescription() => "Оптимизированное вычисление полинома вынесением общего множителя за скобки (O(n)).";
}