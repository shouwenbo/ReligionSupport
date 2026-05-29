using System.Windows;
using System.Windows.Media.Imaging;

namespace WeChatPublisher;

public static class AppIcon
{
    private static BitmapSource? _cached;

    public static void Set(Window window)
    {
        if (_cached == null)
        {
            var stream = Application.GetResourceStream(
                new Uri("pack://application:,,,/Assets/app.ico"))?.Stream;
            if (stream != null)
            {
                var decoder = new IconBitmapDecoder(stream,
                    BitmapCreateOptions.None, BitmapCacheOption.Default);
                _cached = decoder.Frames[0];
            }
        }
        if (_cached != null)
            window.Icon = _cached;
    }
}
