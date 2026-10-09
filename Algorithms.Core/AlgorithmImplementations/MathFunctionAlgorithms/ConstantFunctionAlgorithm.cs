namespace Algorithms.Core.MathFunctions;

public sealed class ConstantFunctionAlgorithm : Algorithm<double[]>
{
    // Fixed amount of work: large enough to exceed the 100 ns Stopwatch resolution,
    // but independent of the input size, so the complexity stays O(1)
    private const int Iterations = 2000;

    public override string Name => "Постоянная функция (O(1))";
    public override TimeComplexity Complexity => TimeComplexity.Constant;

    // Stored so the JIT cannot eliminate the computation as dead code
    public double Result { get; private set; }

    public ConstantFunctionAlgorithm(double[] data) : base(data) { }

    protected override void ExecuteCore(double[] input)
    {
        double x = input.Length > 0 ? input[0] : 1.0;
        double acc = 0;
        for (int i = 0; i < Iterations; i++)
        {
            acc += Math.Sqrt(Math.Abs(x) + i);
        }
        Result = acc;
    }

    protected override double[] CloneInput(double[] input) => input; // Мутации нет, копия не нужна

    public override string GetDescription() =>
        $"Выполняет фиксированное число операций ({Iterations} извлечений корня) над первым элементом, независимо от размера вектора.";
}
