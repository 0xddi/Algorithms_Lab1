namespace Algorithms.Core.PolynomialAlgorithms;

public sealed class NaivePolynomialAlgorithm : Algorithm<double[]>
{
    public override string Name => "Наивное вычисление полинома";
    public override TimeComplexity Complexity => TimeComplexity.Quadratic; 

    public NaivePolynomialAlgorithm(double[] data) : base(data) { }

    protected override void ExecuteCore(double[] input)
    {
        double sum = 0;
        double x = 1.5;
        
        for (int i = 0; i < input.Length; i++)
        {
            double term = 1;
            // Наивное вычисление степени x^(i)
            for (int j = 0; j < i; j++) 
            {
                term *= x;
            }
            sum += input[i] * term;
        }
    }

    protected override double[] CloneInput(double[] input) => input;

    public override string GetDescription() => "Вычисляет значение многочлена, последовательно умножая x для каждого члена (O(n²)).";
}