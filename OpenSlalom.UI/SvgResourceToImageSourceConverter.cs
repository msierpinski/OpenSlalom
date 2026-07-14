using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Xml.Linq;

namespace OpenSlalom.UI;

public class ZeitstrafeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return string.Empty;

        decimal number;

        try
        {
            number = System.Convert.ToDecimal(value);
        }
        catch
        {
            return value.ToString();
        }

        // Optionaler Präfix aus XAML, sonst Standardtext:
        //string prefix = parameter?.ToString() ?? "Wert: ";

        return number > 0 ? $"+{number:N2} Sek." : number.ToString("N2");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

public sealed class GreaterThanZeroConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is IConvertible)
        {
            return System.Convert.ToDecimal(value) > 0;
        }

        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}


public sealed class SvgResourceToImageSourceConverter : IValueConverter
{
    private static readonly Dictionary<string, ImageSource> Cache = new(StringComparer.OrdinalIgnoreCase);

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var path = value as string;
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        if (Cache.TryGetValue(path, out var cached))
        {
            return cached;
        }

        try
        {
            var uri = path.StartsWith("pack://", StringComparison.OrdinalIgnoreCase)
                ? new Uri(path, UriKind.Absolute)
                : new Uri($"pack://application:,,,{path}", UriKind.Absolute);

            var resource = Application.GetResourceStream(uri);
            if (resource?.Stream is null)
            {
                return null;
            }

            using var stream = resource.Stream;
            var document = XDocument.Load(stream);
            var drawingGroup = new DrawingGroup();

            foreach (var pathElement in document.Descendants().Where(x => x.Name.LocalName == "path"))
            {
                var data = pathElement.Attribute("d")?.Value;
                if (string.IsNullOrWhiteSpace(data))
                {
                    continue;
                }

                Brush? brush = Brushes.Black;
                var fillValue = pathElement.Attribute("fill")?.Value;
                if (!string.IsNullOrWhiteSpace(fillValue) &&
                    ColorConverter.ConvertFromString(fillValue) is Color color)
                {
                    brush = new SolidColorBrush(color);
                    brush.Freeze();
                }

                var geometry = Geometry.Parse(data);
                geometry.Freeze();

                var drawing = new GeometryDrawing(brush, null, geometry);
                drawing.Freeze();
                drawingGroup.Children.Add(drawing);
            }

            drawingGroup.Freeze();
            var image = new DrawingImage(drawingGroup);
            image.Freeze();
            Cache[path] = image;
            return image;
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
