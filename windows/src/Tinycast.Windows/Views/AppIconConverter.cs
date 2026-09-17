using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace Tinycast.Windows;

public sealed class AppIconConverter : IValueConverter
{
    public static readonly AppIconConverter Instance = new();
    readonly Dictionary<string, Bitmap> _images = new(StringComparer.OrdinalIgnoreCase);

    public object? Convert(
        object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || !File.Exists(path)) return null;
        if (_images.TryGetValue(path, out var cached)) return cached;
        try
        {
            var image = new Bitmap(path);
            _images[path] = image;
            return image;
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(
        object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class InverseBooleanConverter : IValueConverter
{
    public static readonly InverseBooleanConverter Instance = new();

    public object Convert(
        object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not true;

    public object ConvertBack(
        object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
