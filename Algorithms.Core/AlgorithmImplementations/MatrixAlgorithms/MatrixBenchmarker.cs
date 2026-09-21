using Algorithms.Core.Database;

namespace Algorithms.Core.MatrixAlgorithms;

public record MatrixBenchmarkResult(int N, int M, double TimeMs);

public class MatrixBenchmarker
{
    public string AlgorithmName { get; }
    private readonly Func<int, int, double> _runMeasurement; // (n, m) -> avgTimeMs

    public List<MatrixBenchmarkResult> Results { get; } = new();

    public MatrixBenchmarker(string algorithmName, Func<int, int, double> runMeasurement)
    {
        AlgorithmName = algorithmName;
        _runMeasurement = runMeasurement;
    }

    // Обновленная сигнатура метода Run
    public void Run(IEnumerable<int> nValues, IEnumerable<int> mValues, bool useCache = true,
                    CancellationToken token = default, 
                    IProgress<BenchmarkProgressState>? progress = null, 
                    BenchmarkProgressState? progressState = null)
    {
        using var db = new AppDbContext();
        db.ChangeTracker.AutoDetectChangesEnabled = false;
        var experimentDate = DateTime.Now;

        var cachedData = useCache
            ? db.Results
                .Where(r => r.AlgorithmName == AlgorithmName && r.M != null)
                .AsEnumerable() 
                .GroupBy(r => new { r.N, M = r.M!.Value })
                .ToDictionary(g => (g.Key.N, g.Key.M), g => g.OrderByDescending(r => r.ExperimentDate).First().ElapsedTimeMs)
            : new Dictionary<(int N, int M), double>();

        var newRecords = new List<ExperimentResult>();

        try
        {
            foreach (int n in nValues)
            {
                foreach (int m in mValues)
                {
                    // Точка отмены
                    token.ThrowIfCancellationRequested();

                    if (useCache && cachedData.TryGetValue((n, m), out double cachedTime))
                    {
                        Results.Add(new MatrixBenchmarkResult(n, m, cachedTime));
                    }
                    else
                    {
                        double timeMs = _runMeasurement(n, m);
                        Results.Add(new MatrixBenchmarkResult(n, m, timeMs));

                        newRecords.Add(new ExperimentResult
                        {
                            AlgorithmName = AlgorithmName,
                            N = n,
                            M = m,
                            RunNumber = 0,
                            ElapsedTimeMs = timeMs,
                            ExperimentDate = experimentDate, 
                            StepCount = null
                        });

                        if (newRecords.Count >= 5000)
                        {
                            db.Results.AddRange(newRecords);
                            db.SaveChanges();
                            db.ChangeTracker.Clear();
                            newRecords.Clear();
                        }
                    }

                    // Обновляем прогресс-бар
                    if (progressState != null && progress != null)
                    {
                        progressState.CurrentStep++;
                        progressState.CurrentTaskName = AlgorithmName;
                        progress.Report(progressState);
                    }
                }
            }

            if (newRecords.Any())
            {
                db.Results.AddRange(newRecords);
                db.SaveChanges();
            }
        }
        catch (OperationCanceledException)
        {
            // Если вычисления матриц отменили - сохраняем промежуточный пак данных
            if (newRecords.Any())
            {
                db.Results.AddRange(newRecords);
                db.SaveChanges();
            }
            throw;
        }
        finally
        {
            db.ChangeTracker.AutoDetectChangesEnabled = true;
        }
    }
}