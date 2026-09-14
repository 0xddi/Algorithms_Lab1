namespace Algorithms.Core.MathFunctions;

public sealed class SumAlgorithm : Algorithm<double[]>
{
    public override string Name => "Сумма элементов";
    public override TimeComplexity Complexity => TimeComplexity.Linear;

    public SumAlgorithm(double[] data) : base(data) { }

    protected override void ExecuteCore(double[] input)
    {
        double sum = 0;
        for (int i = 0; i < input.Length; i++)
        {
            sum += input[i];
        }
    }

    protected override double[] CloneInput(double[] input) => input;

    public override string GetDescription() => "Линейный проход по массиву с суммированием всех элементов.";
}