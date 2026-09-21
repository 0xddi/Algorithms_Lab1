using System;
using System.Collections.Generic;
using System.Linq;
using Algorithms.Core.Database;

namespace Algorithms.Core;


public class BenchmarkProgressState
{
    public int CurrentStep { get; set; }
    public int TotalSteps { get; set; }
    public string CurrentTaskName { get; set; } = string.Empty;
}

public class Benchmarker
{
    private readonly double[] _masterData;
    public List<BenchmarkTask> Tasks { get; } = new List<BenchmarkTask>();

    public Benchmarker(double[] masterData)
    {
        _masterData = masterData;
    }

    public void AddTask(BenchmarkTask task) => Tasks.Add(task);

    // Добавлен флаг useCache
    // Измененная сигнатура с поддержкой отмены и прогресса
    public void RunFiltered(IEnumerable<BenchmarkTask> tasksToRun, bool useCache = true, int benchCycles = 5,
                            CancellationToken token = default, 
                            IProgress<BenchmarkProgressState>? progress = null, 
                            BenchmarkProgressState? progressState = null)
    {
        using var db = new AppDbContext();
        var experimentDate = DateTime.Now;

        foreach (var task in tasksToRun)
        {
            task.Results.Clear();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var cachedData = useCache
                ? db.Results
                    .Where(r => r.AlgorithmName == task.Name)
                    .GroupBy(r => r.N)
                    .ToDictionary(g => g.Key,
                        g => g.OrderByDescending(r => r.ExperimentDate).First())
                : new Dictionary<int, ExperimentResult>();

            try
            {
                for (int i = 0; i < _masterData.Length; i++)
                {
                    // Проверка на то, была ли запрошена отмена
                    token.ThrowIfCancellationRequested();

                    int currentN = i + 1;

                    if (useCache && cachedData.TryGetValue(currentN, out var cached))
                    {
                        task.Results.Add((currentN, cached.ElapsedTimeMs, cached.StepCount ?? 0));
                    }
                    else
                    {
                        var currentDataSlice = _masterData[0..currentN];
                        var (avgTimeMs, steps) = task.RunMeasurement(currentDataSlice);

                        task.Results.Add((currentN, avgTimeMs, steps));

                        db.Results.Add(new ExperimentResult
                        {
                            AlgorithmName = task.Name,
                            N = currentN,
                            RunNumber = 0,
                            ElapsedTimeMs = avgTimeMs,
                            ExperimentDate = experimentDate,
                            StepCount = steps          // ← было null
                        });
                    }

                    // Обновляем UI
                    if (progressState != null && progress != null)
                    {
                        progressState.CurrentStep++;
                        progressState.CurrentTaskName = task.Name;
                        progress.Report(progressState);
                    }
                }
                
                db.SaveChanges(); // Сохраняем завершенный алгоритм
            }
            catch (OperationCanceledException)
            {
                // Аккуратная работа с БД: если нажали отмену, сохраняем всё, что успели посчитать!
                db.SaveChanges(); 
                throw; // Пробрасываем ошибку дальше для остановки
            }
        }
    }
}