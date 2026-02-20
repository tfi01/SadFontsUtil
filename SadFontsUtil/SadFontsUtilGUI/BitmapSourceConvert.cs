using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace SadFontsUtilGUI;

/// <summary>
/// Helper class for converting GDI+ Bitmap to WPF BitmapImage
/// </summary>
public static class BitmapSourceConvert
{
    /// <summary>
    /// Converts a System.Drawing.Bitmap to a WPF BitmapImage
    /// </summary>
    public static BitmapImage ToBitmapImage(this Bitmap bitmap)
    {
        using (var stream = new MemoryStream())
        {
            bitmap.Save(stream, ImageFormat.Png);
            stream.Position = 0;

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = stream;
            bitmapImage.EndInit();
            bitmapImage.Freeze();
            return bitmapImage;
        }
    }
}
