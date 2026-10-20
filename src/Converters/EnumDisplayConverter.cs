using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia.Data.Converters;

namespace PdfBooklet.Converters;

/// <summary>Turns enum member names into readable labels: <c>FitToArea</c> → "Fit to area", <c>Pdf17</c> → "PDF 1.7".</summary>
public sealed partial class EnumDisplayConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var name = value?.ToString();
        if (string.IsNullOrEmpty(name))
        {
            return string.Empty;
        }

        var pdf = PdfVersionPattern().Match(name);
        if (pdf.Success)
        {
            return $"PDF {pdf.Groups[1].Value}.{pdf.Groups[2].Value}";
        }

        var builder = new StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            var current = name[i];
            if (i > 0 && char.IsUpper(current) && char.IsLower(name[i - 1]))
            {
                builder.Append(' ').Append(char.ToLower(current, culture));
            }
            else
            {
                builder.Append(current);
            }
        }

        return builder.ToString();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("Enum display conversion is one-way.");

    [GeneratedRegex(@"^Pdf(\d)(\d)$")]
    private static partial Regex PdfVersionPattern();
}
