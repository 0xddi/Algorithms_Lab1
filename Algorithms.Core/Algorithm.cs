using System.Diagnostics;

namespace Algorithms.Core;

/// <summary>
/// Абстрактный базовый класс для алгоритмов и математических функций.
/// Хранит исходные данные и предоставляет встроенную инфраструктуру для замера времени выполнения.
/// </summary>
/// <typeparam name="TInput">Тип входных данных для алгоритма (например, int[], double[] и т.д.).</typeparam>
public abstract class Algorithm<TInput>

{
    
    /// <summary>
    /// Ссылка на исходные немодифицированные данные, переданные при создании.
    /// </summary>
    private TInput Data { get; }

    /// <summary>
    /// Человекочитаемое название алгоритма (например, "Bubble Sort" или "Метод Горнера").
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Теоретическая асимптотическая сложность алгоритма.
    /// </summary>
    public abstract TimeComplexity Complexity { get; }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="Algorithm{TInput}"/>.
    /// </summary>
    /// <param name="data">Входные данные, над которыми будут производиться операции.</param>
    protected Algorithm(TInput data)
    {
        Data = data;
    }

    /// <summary>
    /// Непосредственная реализация логики алгоритма. Вызывается внутри механизма бенчмаркинга.
    /// </summary>
    /// <param name="input">Копия входных данных (для защиты исходного состояния).</param>
    protected abstract void ExecuteCore(TInput input);

    /// <summary>
    /// Создает изолированную копию входных данных перед каждым запуском, 
    /// чтобы исключить влияние мутации данных (например, повторной сортировки) на замеры.
    /// </summary>
    /// <param name="input">Исходные данные.</param>
    /// <returns>Глубокая копия или новый экземпляр входных данных.</returns>
    protected abstract TInput CloneInput(TInput input);

    /// <summary>
    /// Возвращает подробное описание алгоритма и специфики его реализации.
    /// </summary>
    /// <returns>Строка с текстовым описанием алгоритма.</returns>
    public abstract string GetDescription();

    /// <summary>
    /// Выполняет один замер времени работы алгоритма над копией внутренних данных.
    /// </summary>
    /// <returns>Время, затраченное на выполнение одного прогона.</returns>
    public TimeSpan RunBench()
    {
        // Клонируем сохраненные внутренние данные, чтобы не испортить их
        TInput isolatedInput = CloneInput(Data);

        var stopwatch = Stopwatch.StartNew();
        ExecuteCore(isolatedInput);
        stopwatch.Stop();

        return stopwatch.Elapsed;
    }

    /// <summary>
    /// Runs the algorithm several times and returns the median time. The mean is not used: one run
    /// interrupted by the OS scheduler or a GC pause can take thousands of times longer and would
    /// turn the whole point into a spike, while the median ignores up to half of such runs.
    /// </summary>
    /// <param name="benchCycles">Количество прогонов (по умолчанию 5).</param>
    /// <returns>Медианное время выполнения алгоритма в миллисекундах.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Вызывается, если количество прогонов меньше или равно 0.</exception>
    public double RunBench(int benchCycles)
    {
        if (benchCycles <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(benchCycles), "Количество прогонов должно быть больше нуля.");
        }

        // Прогрев JIT-компилятора (JIT Warmup) на копии данных, чтобы первый запуск не искажал статистику
        TInput warmupData = CloneInput(Data);
        ExecuteCore(warmupData);

        var runSeconds = new double[benchCycles];

        for (int i = 0; i < benchCycles; i++)
        {
            TInput isolatedInput = CloneInput(Data);
            ScrubBranchPredictor();

            long start = Stopwatch.GetTimestamp();
            ExecuteCore(isolatedInput);
            long end = Stopwatch.GetTimestamp();

            // Считаем точное время прогона в секундах
            runSeconds[i] = (double)(end - start) / Stopwatch.Frequency;
        }

        Array.Sort(runSeconds);
        int mid = benchCycles / 2;
        double medianSeconds = benchCycles % 2 == 1
            ? runSeconds[mid]
            : (runSeconds[mid - 1] + runSeconds[mid]) / 2;

        // Возвращаем медианное время в миллисекундах
        return medianSeconds * 1000.0;
    }

    private const int ScrubSize = 4096;
    private static readonly double[] ScrubBuffer = new double[ScrubSize];

    /// <summary>
    /// The same input is sorted on every cycle, so the CPU branch predictor learns its comparison
    /// outcomes and small N run several times faster than they should. Sorting fresh random data
    /// between cycles overwrites that history, so each timed run sees the input as new.
    /// </summary>
    private static void ScrubBranchPredictor()
    {
        for (int i = 0; i < ScrubSize; i++)
        {
            ScrubBuffer[i] = Random.Shared.NextDouble();
        }

        Array.Sort(ScrubBuffer);
    }
    
    
    
    public void Execute()
    {
        TInput isolatedInput = CloneInput(Data);
        ExecuteCore(isolatedInput);
    }
}