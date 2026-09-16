using System;
using System.Collections.Generic;

namespace Algorithms.Core;

/// <summary>
/// Адаптер алгоритма Ахо-Корасик для бенчмарка над double[].
/// Длина текста берётся из длины входного массива.
/// </summary>
public sealed class AhoCorasickAlgorithm : Algorithm<double[]>
{
    private static int _sink;
    private readonly string[] _patterns;

    public override string Name => "Aho-Corasick";
    public override TimeComplexity Complexity => TimeComplexity.Linear; // если нет Linear — добавьте в enum

    public AhoCorasickAlgorithm(double[] data, string[]? patterns = null) : base(data)
    {
        // Набор шаблонов можно сделать фиксированным или генерировать от длины.
        _patterns = patterns ?? new[] { "aba", "aca", "abac", "abc" };
    }

    protected override void ExecuteCore(double[] array)
    {
        // Генерируем текст длины array.Length (значения массива игнорируем)
        string text = GenerateText(array.Length);

        var (matches, _) = RunAhoCorasick(text, _patterns);
        _sink = matches; // чтобы JIT не выбросил вычисления
    }

    protected override double[] CloneInput(double[] input)
    {
        return (double[])input.Clone();
    }

    public override string GetDescription()
    {
        return "Алгоритм Ахо-Корасик для поиска набора строк-шаблонов в тексте. " +
               "В бенчмарке длина текста равна длине входного среза double[].";
    }

    private static string GenerateText(int length)
    {
        var chars = new char[length];
        for (int i = 0; i < length; i++)
            chars[i] = (char)('a' + Random.Shared.Next(26));
        return new string(chars);
    }

    // Непосредственно реализация Ахо-Корасик (вынесена в отдельный метод)
    private static (int matches, List<(int index, string pattern)> found) RunAhoCorasick(
        string text, string[] patterns)
    {
        var nodes = new List<Node> { new Node() };

        // 1. Построение бора
        foreach (var pattern in patterns)
        {
            if (string.IsNullOrEmpty(pattern)) continue;
            int v = 0;
            foreach (char c in pattern)
            {
                if (!nodes[v].Next.TryGetValue(c, out int u))
                {
                    u = nodes.Count;
                    nodes[v].Next[c] = u;
                    nodes.Add(new Node());
                }
                v = u;
            }
            nodes[v].Output.Add(pattern);
        }

        // 2. Суффиксные ссылки
        var queue = new Queue<int>();
        foreach (var child in nodes[0].Next.Values)
        {
            nodes[child].Fail = 0;
            queue.Enqueue(child);
        }

        while (queue.Count > 0)
        {
            int v = queue.Dequeue();
            foreach (var kv in nodes[v].Next)
            {
                char c = kv.Key;
                int u = kv.Value;
                int f = nodes[v].Fail;
                while (f != 0 && !nodes[f].Next.ContainsKey(c))
                    f = nodes[f].Fail;

                if (nodes[f].Next.TryGetValue(c, out int failTarget) && failTarget != u)
                    nodes[u].Fail = failTarget;
                else
                    nodes[u].Fail = 0;

                nodes[u].Output.AddRange(nodes[nodes[u].Fail].Output);
                queue.Enqueue(u);
            }
        }

        // 3. Проход по тексту
        int state = 0;
        int matches = 0;
        var found = new List<(int, string)>();

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            while (state != 0 && !nodes[state].Next.ContainsKey(c))
                state = nodes[state].Fail;

            if (nodes[state].Next.TryGetValue(c, out int nextState))
                state = nextState;
            else
                state = 0;

            foreach (var pattern in nodes[state].Output)
            {
                matches++;
                found.Add((i - pattern.Length + 1, pattern));
            }
        }

        return (matches, found);
    }

    private sealed class Node
    {
        public readonly Dictionary<char, int> Next = new();
        public int Fail;
        public readonly List<string> Output = new();
    }
}