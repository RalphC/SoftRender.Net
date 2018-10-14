using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml.Media.Imaging;
using SharpDX;

namespace SoftRender.Net
{
    public class Texture
    {
        private byte[] internalBuffer;
        private int width, height;

        public Texture(string filename, int width, int height)
        {
            this.height = height;
            this.width = width;
            Load(filename);
        }

        async void Load(string filename)
        {
            var file = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFileAsync(filename);

            using (var stream = await file.OpenReadAsync())
            {
                var bmp = new WriteableBitmap(width, height);
                bmp.SetSource(stream);

                internalBuffer = bmp.PixelBuffer.ToArray();
            }
        }

        public Color4 Map(float tu, float tv)
        {
            if (internalBuffer == null)
                return Color4.White;

            int u = Math.Abs((int)(tu * width) % width);
            int v = Math.Abs((int)(tv * height) % height);

            int pos = (u + v * width) * 4;

            byte b = internalBuffer[pos];
            byte g = internalBuffer[pos + 1];
            byte r = internalBuffer[pos + 2];
            byte a = internalBuffer[pos + 3];
            return new Color4(r / 255f, g / 255f, b / 255f, a / 255f);
        }
    }
}
