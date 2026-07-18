using System.Globalization;
using System.Windows;

namespace GrblPlotter.Wpf.Services;

/// <summary>Simple en/zh ResourceDictionary switch (Phase 6).</summary>
public static class LocalizationService
{
    public static string Current { get; private set; } = "en";

    public static void Apply(string culture)
    {
        Current = culture.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? "zh" : "en";
        var dict = new ResourceDictionary
        {
            Source = new Uri($"Themes/Strings.{Current}.xaml", UriKind.Relative)
        };
        var app = Application.Current;
        // Replace or insert strings dictionary
        for (int i = app.Resources.MergedDictionaries.Count - 1; i >= 0; i--)
        {
            var src = app.Resources.MergedDictionaries[i].Source?.OriginalString ?? "";
            if (src.Contains("Strings.", StringComparison.OrdinalIgnoreCase))
                app.Resources.MergedDictionaries.RemoveAt(i);
        }
        app.Resources.MergedDictionaries.Insert(0, dict);
        try
        {
            CultureInfo.CurrentUICulture = Current == "zh" ? new CultureInfo("zh-CN") : new CultureInfo("en-US");
        }
        catch { /* ignore */ }
    }
}
