using System.Collections.Generic;
using System.Numerics;
using Algorithms.Core.MatrixAlgorithms;
using Avalonia.Controls;
using HelixToolkit.Avalonia.SharpDX;
using HelixToolkit.SharpDX;

namespace Algorithms.GUI.Views;

public partial class MatrixSurfaceWindow : Window
{
    public MatrixSurfaceWindow() => InitializeComponent();

    public MatrixSurfaceWindow(List<MatrixBenchmarkResult> results) : this()
    {
        var effectsManager = new DefaultEffectsManager();
        Viewport.EffectsManager = effectsManager;
        Viewport.Camera = new PerspectiveCamera
        {
            Position = new Vector3(60, 80, 60),
            LookDirection = new Vector3(-60, -80, -60),
            UpDirection = new Vector3(0, 1, 0)
        };

        Viewport.Items.Add(new DirectionalLight3D { Direction = new Vector3(-1, -1, -1) });

        var mesh = MatrixSurfaceBuilder.BuildSurfaceMesh(results, out _);

        Viewport.Items.Add(new MeshGeometryModel3D
        {
            Geometry = mesh,
            Material = PhongMaterials.White
        });
    }
}