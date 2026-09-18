using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Algorithms.Core.MatrixAlgorithms;
using HelixToolkit;
using HelixToolkit.Maths;
using MeshGeometry3D = HelixToolkit.SharpDX.MeshGeometry3D;

namespace Algorithms.GUI.Views;

public static class MatrixSurfaceBuilder
{
    public static MeshGeometry3D BuildSurfaceMesh(List<MatrixBenchmarkResult> results, out Color4Collection colors)
    {
        var nValues = results.Select(r => r.N).Distinct().OrderBy(x => x).ToArray();
        var mValues = results.Select(r => r.M).Distinct().OrderBy(x => x).ToArray();

        int rows = nValues.Length;
        int cols = mValues.Length;

        var timeGrid = new double[rows, cols];
        foreach (var r in results)
        {
            int i = Array.IndexOf(nValues, r.N);
            int j = Array.IndexOf(mValues, r.M);
            timeGrid[i, j] = r.TimeMs;
        }

        double maxTime = results.Max(r => r.TimeMs);
        double heightScale = maxTime > 0 ? 50.0 / maxTime : 1.0; // визуально разумная высота холма

        var positions = new Vector3Collection();
        colors = new Color4Collection();
        var indices = new IntCollection();

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                float x = i;
                float z = j;
                float y = (float)(timeGrid[i, j] * heightScale); // высота = время

                positions.Add(new Vector3(x, y, z));
                colors.Add(HeightToColor(maxTime > 0 ? timeGrid[i, j] / maxTime : 0));
            }
        }

        // Триангуляция сетки: каждая ячейка -> 2 треугольника
        for (int i = 0; i < rows - 1; i++)
        {
            for (int j = 0; j < cols - 1; j++)
            {
                int topLeft = i * cols + j;
                int topRight = i * cols + (j + 1);
                int bottomLeft = (i + 1) * cols + j;
                int bottomRight = (i + 1) * cols + (j + 1);

                indices.Add(topLeft); indices.Add(bottomLeft); indices.Add(topRight);
                indices.Add(topRight); indices.Add(bottomLeft); indices.Add(bottomRight);
            }
        }

        var mesh = new MeshGeometry3D
        {
            Positions = positions,
            Indices = indices,
            Colors = colors,
            Normals = CalculateFlatNormals(positions, indices)
        };

        return mesh;
    }

    private static Vector3Collection CalculateFlatNormals(Vector3Collection positions, IntCollection indices)
    {
        var accum = new Vector3[positions.Count];

        for (int t = 0; t < indices.Count; t += 3)
        {
            int i0 = indices[t];
            int i1 = indices[t + 1];
            int i2 = indices[t + 2];

            Vector3 p0 = positions[i0];
            Vector3 p1 = positions[i1];
            Vector3 p2 = positions[i2];

            Vector3 faceNormal = Vector3.Cross(p1 - p0, p2 - p0);

            accum[i0] += faceNormal;
            accum[i1] += faceNormal;
            accum[i2] += faceNormal;
        }

        var result = new Vector3Collection();
        foreach (var n in accum)
        {
            result.Add(n.LengthSquared() > 1e-12f ? Vector3.Normalize(n) : new Vector3(0, 1, 0));
        }

        return result;
    }

    private static Color4 HeightToColor(double t)
    {
        t = Math.Clamp(t, 0, 1);
        // синий (медленно) -> зелёный -> жёлтый/красный (медленно/долго)
        if (t < 0.5)
        {
            double k = t / 0.5;
            return new Color4(0f, (float)k, (float)(1 - k), 1f);
        }
        double k2 = (t - 0.5) / 0.5;
        return new Color4((float)k2, (float)(1 - k2), 0f, 1f);
    }
}