using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ChtotibDocsPrintNET.Services;

public static class AppBranding
{
    private const string AssemblyName = "ChtotibDocsPrintNET";

    public static ImageSource? TryLoadLogo()
    {
        foreach (var uri in GetLogoUriCandidates())
        {
            var source = TryLoadBitmap(uri);
            if (source != null)
                return source;
        }

        return null;
    }

    public static ImageSource? TryLoadWindowIcon()
    {
        foreach (var uri in GetIconUriCandidates())
        {
            try
            {
                var frame = BitmapFrame.Create(uri);
                frame.Freeze();
                return frame;
            }
            catch
            {
                // пробуем следующий источник
            }
        }

        return null;
    }

    private static IEnumerable<Uri> GetLogoUriCandidates()
    {
        yield return PackUri($"pack://application:,,,/{AssemblyName};component/Resources/logo.png");
        yield return PackUri("pack://application:,,,/Resources/logo.png");

        var diskPath = Path.Combine(AppContext.BaseDirectory, "Resources", "logo.png");
        if (File.Exists(diskPath))
            yield return new Uri(diskPath, UriKind.Absolute);
    }

    private static IEnumerable<Uri> GetIconUriCandidates()
    {
        foreach (var uri in GetLogoUriCandidates())
            yield return uri;

        yield return PackUri($"pack://application:,,,/{AssemblyName};component/Resources/app.ico");
        yield return PackUri("pack://application:,,,/Resources/app.ico");

        var diskPath = Path.Combine(AppContext.BaseDirectory, "Resources", "app.ico");
        if (File.Exists(diskPath))
            yield return new Uri(diskPath, UriKind.Absolute);
    }

    private static Uri PackUri(string value) => new(value, UriKind.Absolute);

    private static BitmapImage? TryLoadBitmap(Uri uri)
    {
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = uri;
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }
}
