namespace Algorithms.Core.MathFunctions;

public sealed class ConstantFunctionAlgorithm : Algorithm<double[]>
{
    public override string Name => "Постоянная функция (f(v) = 1)";
    public override TimeComplexity Complexity => TimeComplexity.Constant;

    public ConstantFunctionAlgorithm(double[] data) : base(data) { }

    protected override void ExecuteCore(double[] input)
    {
        double result = 1.0; 
    }

    protected override double[] CloneInput(double[] input) => input; // Мутации нет, копия не нужна

    public override string GetDescription() => "Возвращает константу 1 независимо от размера вектора.";
}