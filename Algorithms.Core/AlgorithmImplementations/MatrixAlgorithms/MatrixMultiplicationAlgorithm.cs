namespace Algorithms.Core.MatrixAlgorithms;

/// <summary>
/// Классическое умножение матриц: A (n x k) на B (k x m) -> C (n x m).
/// Тройной вложенный цикл, O(n * k * m).
/// </summary>
public sealed class MatrixMultiplicationAlgorithm : Algorithm<(double[,] A, double[,] B)>
{
    public override string Name => "Умножение матриц (наивное)";
    public override TimeComplexity Complexity => TimeComplexity.Cubic;

    public MatrixMultiplicationAlgorithm((double[,] A, double[,] B) data) : base(data)
    {
        if (data.A.GetLength(1) != data.B.GetLength(0))
            throw new ArgumentException("Число столбцов A должно совпадать с числом строк B.");
    }

    protected override void ExecuteCore((double[,] A, double[,] B) input)
    {
        int n = input.A.GetLength(0);
        int k = input.A.GetLength(1); // = input.B.GetLength(0)
        int m = input.B.GetLength(1);

        double[,] result = new double[n, m];

        // Оптимизированный порядок циклов: i -> t -> j
        for (int i = 0; i < n; i++)
        {
            for (int t = 0; t < k; t++)
            {
                // Кэшируем значение из матрицы A, чтобы не обращаться к нему во внутреннем цикле
                double a = input.A[i, t]; 
            
                for (int j = 0; j < m; j++)
                {
                    // Теперь память массива B читается последовательно, а result пишется последовательно
                    result[i, j] += a * input.B[t, j];
                }
            }
        }
    }

    protected override (double[,] A, double[,] B) CloneInput((double[,] A, double[,] B) input)
        => ((double[,])input.A.Clone(), (double[,])input.B.Clone());

    public override string GetDescription()
        => "Вычисляет произведение матриц A (n×k) и B (k×m) прямым методом (тройной вложенный цикл) за O(n·k·m).";
}