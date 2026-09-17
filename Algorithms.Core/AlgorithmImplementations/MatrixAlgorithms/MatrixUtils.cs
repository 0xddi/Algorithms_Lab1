namespace Algorithms.Core.MatrixAlgorithms;

public static class MatrixUtils
{
    /// <summary>
    /// Генерация рандомной матрицы
    /// </summary>
    /// <param name="rows">Число строк в матрице</param>
    /// <param name="cols">Число столбцов в матрице</param>
    /// <returns>Возвращает двумерный массив вещественных чисел - матрицу</returns>
    public static double[,] GenerateRandomMatrix(int rows, int cols)
    {
        var matrix = new double[rows, cols];
        for (int i = 0; i < rows; i++)
        for (int j = 0; j < cols; j++)
            matrix[i, j] = Random.Shared.NextDouble() * 100.0;
        return matrix;
    }
}