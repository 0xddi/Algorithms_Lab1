using System;
using System.Diagnostics;

namespace Algorithms.Core;

/// <summary>
/// Абстрактный базовый класс для алгоритмов и математических функций.
/// Предоставляет встроенную инфраструктуру для замера времени выполнения.
/// </summary>
/// <typeparam name="TInput">Тип входных данных для алгоритма (например, int[], double[] или (int[,], int[,])).</typeparam>
public abstract class Algorithm<TInput>
{
    /// <summary>
    /// Человекочитаемое название алгоритма (например, "Bubble Sort" или "Метод Горнера").
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Теоретическая асимптотическая сложность алгоритма.
    /// </summary>
    public TimeComplexity Complexity { get; }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="Algorithm{TInput}"/>.
    /// </summary>
    /// <param name="complexity">Теоретическая сложность алгоритма в нотации O-большое.</param>
    protected Algorithm(TimeComplexity complexity)
    {
        Complexity = complexity;
    }

    /// <summary>
    /// Непосредственная реализация логики алгоритма. Вызывается внутри механизма бенчмаркинга.
    /// </summary>
    /// <param name="input">Подготовленные входные данные.</param>
    protected abstract void ExecuteCore(TInput input);

    /// <summary>
    /// Создает изолированную копию входных данных перед каждым запуском, 
    /// чтобы исключить влияние мутации данных (например, повторной сортировки) на замеры.
    /// </summary>
    /// <param name="input">Исходные входные данные.</param>
    /// <returns>Глубокая копия или новый экземпляр входных данных.</returns>
    protected abstract TInput CloneInput(TInput input);

    /// <summary>
    /// Возвращает подробное описание алгоритма и специфики его реализации.
    /// </summary>
    /// <returns>Строка с текстовым описанием алгоритма.</returns>
    public abstract string GetDescription();

    /// <summary>
    /// Выполняет один замер времени работы алгоритма над копией входных данных.
    /// </summary>
    /// <param name="input">Входные данные для алгоритма.</param>
    /// <returns>Время, затраченное на выполнение одного прогона.</returns>
    public TimeSpan RunBench(TInput input)
    {
        TInput isolatedInput = CloneInput(input);

        var stopwatch = Stopwatch.StartNew();
        ExecuteCore(isolatedInput);
        stopwatch.Stop();

        return stopwatch.Elapsed;
    }

    /// <summary>
    /// Выполняет серию замеров времени работы алгоритма и возвращает среднее арифметическое значение.
    /// </summary>
    /// <param name="input">Исходные входные данные.</param>
    /// <param name="benchCycles">Количество прогонов для усреднения (по умолчанию 5).</param>
    /// <returns>Среднее время выполнения алгоритма.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Вызывается, если количество прогонов меньше или равно 0.</exception>
    public TimeSpan RunBench(TInput input, int benchCycles = 5)
    {
        if (benchCycles <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(benchCycles), "Количество прогонов должно быть больше нуля.");
        }

        // Прогрев JIT-компилятора (JIT Warmup), чтобы первый запуск не искажал статистику
        TInput warmupData = CloneInput(input);
        ExecuteCore(warmupData);

        long totalTicks = 0;

        for (int i = 0; i < benchCycles; i++)
        {
            TInput isolatedInput = CloneInput(input);

            var stopwatch = Stopwatch.StartNew();
            ExecuteCore(isolatedInput);
            stopwatch.Stop();

            totalTicks += stopwatch.ElapsedTicks;
        }

        return TimeSpan.FromTicks(totalTicks / benchCycles);
    }
}