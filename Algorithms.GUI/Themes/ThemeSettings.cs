using System;
using System.IO;
using Avalonia.Styling;

namespace Algorithms.GUI.Themes;

/// <summary>
/// Remembers the theme the user picked. Until the user picks one, the app follows the system theme.
/// </summary>
public static class ThemeSettings
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Algorithms_Lab1", "theme.txt");

    /// <summary>Returns the saved theme, or <see cref="ThemeVariant.Default"/> (follow system) if none.</summary>
    public static ThemeVariant Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return ThemeVariant.Default;

            return File.ReadAllText(FilePath).Trim() switch
            {
                "Light" => ThemeVariant.Light,
                "Dark" => ThemeVariant.Dark,
                _ => ThemeVariant.Default
            };
        }
        catch (Exception)
        {
            // A broken settings file must not prevent the app from starting
            return ThemeVariant.Default;
        }
    }

    public static void Save(ThemeVariant variant)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, variant == ThemeVariant.Light ? "Light" : "Dark");
        }
        catch (Exception)
        {
            // Not critical: the theme simply won't be remembered
        }
    }
}
