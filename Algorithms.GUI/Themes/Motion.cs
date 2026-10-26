using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace Algorithms.GUI.Themes;

/// <summary>
/// Entrance animations for content that appears after a run. Respects the OS
/// "show animations" setting, so users who turned motion off get the final state at once.
/// </summary>
public static class Motion
{
    private static readonly TimeSpan EnterDuration = TimeSpan.FromMilliseconds(280);
    private static readonly TimeSpan StaggerStep = TimeSpan.FromMilliseconds(40);
    private const int MaxStaggeredItems = 8; // later items share the last delay, so the whole reveal stays short
    private const double EnterOffset = 10;   // px; small enough to read as a fade, not a slide

    public static bool IsReduced { get; } = DetectReducedMotion();

    /// <summary>Fades the control in and lifts it slightly into place.</summary>
    public static void FadeIn(Control control, int index = 0)
    {
        if (IsReduced) return;

        var translate = new TranslateTransform();
        control.RenderTransform = translate;
        control.Opacity = 0;

        var animation = new Animation
        {
            Duration = EnterDuration,
            Delay = StaggerStep * Math.Min(index, MaxStaggeredItems),
            Easing = new CubicEaseOut(), // decelerate when arriving
            FillMode = FillMode.Both,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0),
                    Setters = { new Setter(Visual.OpacityProperty, 0d), new Setter(TranslateTransform.YProperty, EnterOffset) }
                },
                new KeyFrame
                {
                    Cue = new Cue(1),
                    Setters = { new Setter(Visual.OpacityProperty, 1d), new Setter(TranslateTransform.YProperty, 0d) }
                }
            }
        };

        // The final state is also set explicitly, so correctness never depends on the animation finishing
        _ = animation.RunAsync(control).ContinueWith(_ => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            control.Opacity = 1;
            translate.Y = 0;
        }));
    }

    private static bool DetectReducedMotion()
    {
        if (!OperatingSystem.IsWindows()) return false;
        try
        {
            // SPI_GETCLIENTAREAANIMATION: false when "Show animations in Windows" is off
            return SystemParametersInfo(0x1042, 0, out bool enabled, 0) && !enabled;
        }
        catch (Exception)
        {
            return false;
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint action, uint param, out bool value, uint winIni);
}
