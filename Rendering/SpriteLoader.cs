using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;
using System.Windows.Media.Imaging;

namespace SpriteRigEditor.Rendering
{
    public static class SpriteLoader
    {
        public static BitmapImage Load(string path)
        {
            using var img = Image.Load<Rgba32>(path);
            using var ms = new MemoryStream();

            img.SaveAsPng(ms);
            ms.Position = 0;

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.StreamSource = ms;
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();

            return bmp;
        }
    }
}
