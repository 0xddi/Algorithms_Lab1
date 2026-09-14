namespace Algorithms.Core;

class Program
{
    static void Main(string[] args)
    {
        double[] numbers = new double[2000];

        for (int i = 0; i < numbers.Length; i++)
        {
            // Generates numbers between 1 and 100 (inclusive min, exclusive max)
            numbers[i] = Random.Shared.Next(1, 101); 
        }
        
        var test = new Benchmarker(numbers);
        test.Run();
        test.PrintResults();
    }
}