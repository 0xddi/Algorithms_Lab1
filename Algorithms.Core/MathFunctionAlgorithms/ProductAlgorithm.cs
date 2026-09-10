namespace Algorithms.Core.MathFunctions;

public sealed class ProductAlgorithm : Algorithm<double[]>
{
    public override string Name => "Произведение элементов";
    public override TimeComplexity Complexity => TimeComplexity.Linear;

    public ProductAlgorithm(double[] data) : base(data) { }

    protected override void ExecuteCore(double[] input)
    {
        double product = 1;
        for (int i = 0; i < input.Length; i++)
        {
            product *= input[i];
        }
    }

    protected override double[] CloneInput(double[] input) => input;

    public override string GetDescription() => "Линейный проход по массиву с перемножением всех элементов.";
}