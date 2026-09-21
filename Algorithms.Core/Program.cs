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

        // Вспомогательные методы для краткости
        void AddTimeTask(string name, Func<double[], double> measurement)
        {
            bench.AddTask(new BenchmarkTask(name, slice => (measurement(slice), 0L)));
        }

        void AddStepTask(string name, Func<double[], Algorithm<(double x, int n)>> factory)
        {
            bench.AddTask(new BenchmarkTask(name, slice =>
            {
                var algo = factory(slice);
                double t = algo.RunBench(5);
                return (t, algo.Steps);
            }));
        }

        // ==========================================
        // 1. Сортировки
        // ==========================================
        AddTimeTask("Bubble Sort", slice => new BubbleSortAlgorithm(slice).RunBench(5));
        AddTimeTask("Quick Sort",  slice => new QuickSortAlgorithm(slice).RunBench(5));
        AddTimeTask("Tim Sort",    slice => new TimSortAlgorithm(slice).RunBench(5));

        // ==========================================
        // 2. Математические функции над вектором
        // ==========================================
        AddTimeTask("Constant Function f(v)=1", slice => new ConstantFunctionAlgorithm(slice).RunBench(5));
        AddTimeTask("Sum Algorithm",            slice => new SumAlgorithm(slice).RunBench(5));
        AddTimeTask("Product Algorithm",        slice => new ProductAlgorithm(slice).RunBench(5));

        // ==========================================
        // 3. Полиномы при x = 1.5
        // ==========================================
        AddTimeTask("Naive Polynomial",  slice => new NaivePolynomialAlgorithm(slice).RunBench(5));
        AddTimeTask("Horner Polynomial", slice => new HornerPolynomialAlgorithm(slice).RunBench(5));

        // ==========================================
        // 4. Возведение в степень x^n
        // ==========================================
        const double baseX = 1.5;

        AddStepTask("Simple Pow (x^n)",
            slice => new SimplePowAlgorithm((x: baseX, n: slice.Length)));

        AddStepTask("Recursive Pow",
            slice => new RecursivePowerAlgorithm((x: baseX, n: slice.Length)));

        AddStepTask("Fast Pow",
            slice => new FastPowerAlgorithm((x: baseX, n: slice.Length)));

        AddStepTask("Classic Fast Pow",
            slice => new ClassicFastPowerAlgorithm((x: baseX, n: slice.Length)));

        // ==========================================
        // 5. Запуск всех задач
        // ==========================================
        bench.RunFiltered(bench.Tasks, useCache: true, benchCycles: 5);

        // Небольшой вывод, чтобы убедиться, что всё отработало
        foreach (var task in bench.Tasks)
        {
            Console.WriteLine($"{task.Name}: {task.Results.Count} точек");
        }
    }
}