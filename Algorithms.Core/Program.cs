using Algorithms.Core.MathFunctions;
using Algorithms.Core.PolynomialAlgorithms;
using Algorithms.Core.PowFunctionAlgorithms;

namespace Algorithms.Core;

class Program
{
    static void Main(string[] args)
    {
        double[] numbers = new double[2000];
        for (int i = 0; i < numbers.Length; i++)
        {
            numbers[i] = Random.Shared.NextDouble() * 100.0;
        }

        var bench = new Benchmarker(numbers);

        // ==========================================
        // 1. Сортировки (SortingAlgorithms)
        // ==========================================
        bench.AddTask(new BenchmarkTask("Bubble Sort", slice => new BubbleSortAlgorithm(slice).RunBench(5)));
        bench.AddTask(new BenchmarkTask("Quick Sort", slice => new QuickSortAlgorithm(slice).RunBench(5)));
        bench.AddTask(new BenchmarkTask("Tim Sort", slice => new TimSortAlgorithm(slice).RunBench(5)));

        // ==========================================
        // 2. Математические функции над вектором (MathFunctionAlgorithms)
        // ==========================================
        bench.AddTask(new BenchmarkTask("Constant Function f(v)=1", slice => new ConstantFunctionAlgorithm(slice).RunBench(5)));
        bench.AddTask(new BenchmarkTask("Sum Algorithm", slice => new SumAlgorithm(slice).RunBench(5)));
        bench.AddTask(new BenchmarkTask("Product Algorithm", slice => new ProductAlgorithm(slice).RunBench(5)));

        // ==========================================
        // 3. Вычисление полиномов при x = 1.5 (PolynomialAlgorithms)
        // ==========================================
        bench.AddTask(new BenchmarkTask("Naive Polynomial", slice => new NaivePolynomialAlgorithm(slice).RunBench(5)));
        bench.AddTask(new BenchmarkTask("Horner Polynomial", slice => new HornerPolynomialAlgorithm(slice).RunBench(5)));

        // ==========================================
        // 4. Возведение в степень x^n (PowFunctionAlgorithms)
        // Передается кортеж (x = 1.5, n = длина среза)
        // ==========================================
        const double baseX = 1.5;

        bench.AddTask(new BenchmarkTask("Simple Pow (x^n)", 
            slice => new SimplePowAlgorithm((x: baseX, n: slice.Length)).RunBench(5)));

        bench.AddTask(new BenchmarkTask("Recursive Pow", 
            slice => new RecursivePowerAlgorithm((x: baseX, n: slice.Length)).RunBench(5)));

        bench.AddTask(new BenchmarkTask("Fast Pow", 
            slice => new FastPowerAlgorithm((x: baseX, n: slice.Length)).RunBench(5)));

        bench.AddTask(new BenchmarkTask("Classic Fast Pow", 
            slice => new ClassicFastPowerAlgorithm((x: baseX, n: slice.Length)).RunBench(5)));

        // ==========================================
        // Запуск и вывод результатов
        // ==========================================
        bench.Run();
        bench.DebugPrintResults();
    }
}   