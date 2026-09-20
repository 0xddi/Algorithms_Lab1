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

    public void Run(IEnumerable<int> nValues, IEnumerable<int> mValues, bool useCache = true)
    {
        using var db = new AppDbContext();
        var experimentDate = DateTime.Now;

        var cachedData = useCache
            ? db.Results
                .Where(r => r.AlgorithmName == AlgorithmName && r.M != null)
                .GroupBy(r => new { r.N, M = r.M!.Value })
                .ToDictionary(
                    g => (g.Key.N, g.Key.M),
                    g => g.OrderByDescending(r => r.ExperimentDate).First().ElapsedTimeMs)
            : new Dictionary<(int N, int M), double>();

        foreach (int n in nValues)
        {
            foreach (int m in mValues)
            {
                if (useCache && cachedData.TryGetValue((n, m), out double cachedTime))
                {
                    Results.Add(new MatrixBenchmarkResult(n, m, cachedTime));
                    continue;
                }

                double avgTimeMs = _runMeasurement(n, m);
                Results.Add(new MatrixBenchmarkResult(n, m, avgTimeMs));

                db.Results.Add(new ExperimentResult
                {
                    AlgorithmName = AlgorithmName,
                    N = n,
                    M = m,
                    RunNumber = 0,
                    ElapsedTimeMs = avgTimeMs,
                    ExperimentDate = experimentDate,
                    StepCount = null
                });
            }
        }

        db.SaveChanges();
    }
}