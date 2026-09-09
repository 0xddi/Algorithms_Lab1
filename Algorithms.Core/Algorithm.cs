using System;
using System.Diagnostics;

namespace Algorithms.Core;

/// <summary>
/// Абстрактный базовый класс для алгоритмов и математических функций.
/// Хранит исходные данные и предоставляет встроенную инфраструктуру для замера времени выполнения.
/// </summary>
/// <typeparam name="TInput">Тип входных данных для алгоритма (например, int[], double[] или (int[,], int[,])).</typeparam>
public abstract class Algorithm<TInput>
{
    /// <summary>
    /// Ссылка на исходные немодифицированные данные, переданные при создании.
    /// </summary>
    protected TInput Data { get; }

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
    /// Выполняет серию замеров времени работы алгоритма и возвращает среднее арифметическое значение.
    /// </summary>
    /// <param name="benchCycles">Количество прогонов для усреднения (по умолчанию 5).</param>
    /// <returns>Среднее время выполнения алгоритма.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Вызывается, если количество прогонов меньше или равно 0.</exception>
    public TimeSpan RunBench(int benchCycles)
    {
        if (benchCycles <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(benchCycles), "Количество прогонов должно быть больше нуля.");
        }

        // Прогрев JIT-компилятора (JIT Warmup) на копии данных, чтобы первый запуск не искажал статистику
        TInput warmupData = CloneInput(Data);
        ExecuteCore(warmupData);

        long totalTicks = 0;

        for (int i = 0; i < benchCycles; i++)
        {
            // Каждый прогон цикла получает чистую копию данных
            TInput isolatedInput = CloneInput(Data);

            var stopwatch = Stopwatch.StartNew();
            ExecuteCore(isolatedInput);
            stopwatch.Stop();

            totalTicks += stopwatch.ElapsedTicks;
        }

        return TimeSpan.FromTicks(totalTicks / benchCycles);
    }
}