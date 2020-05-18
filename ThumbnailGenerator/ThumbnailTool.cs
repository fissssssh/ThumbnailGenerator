using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThumbnailGenerator
{
    public class ThumbnailTool
    {
        public async Task<Image> GenerateAsync(Image img, int maxWidth)
        {
            var thumbnail = await img.ResizeAsync(maxWidth);
            img.Dispose();
            return thumbnail;
        }
        public async Task<Image> GenerateAsync(Stream stream, int maxWidth)
        {
            var img = Image.FromStream(stream);
            return await GenerateAsync(img, maxWidth);
        }

        public async Task<Image> GenerateAsync(string fileName, int maxWidth)
        {
            var img = Image.FromFile(fileName);
            return await GenerateAsync(img, maxWidth);
        }
        public async Task GenerateAndSaveAsync(string fileName, int maxWidth, string saveFileName)
        {
            var img = await GenerateAsync(fileName, maxWidth);
            img.SaveAndReleaseAsync(saveFileName);
        }

        public async Task GenerateAndSaveAsync(Stream stream, int maxWidth, string saveFileName)
        {
            var img = await GenerateAsync(stream, maxWidth);
            img.SaveAndReleaseAsync(saveFileName);
        }

        public async Task GenerateAndSaveAsync(Image img, int maxWidth, string saveFileName)
        {
            var thumbnail = await GenerateAsync(img, maxWidth);
            img.SaveAndReleaseAsync(saveFileName);
        }
    }

    public static class ImageExtensions
    {
        public static async Task<Image> ResizeAsync(this Image img, int maxWidth)
        {
            if (img.Width <= maxWidth)
            {
                var bitmap = new Bitmap(img);
                return bitmap;
            }
            var maxHeight = (int)((float)maxWidth / img.Width * img.Height);
            var newBm = new Bitmap(maxWidth, maxHeight);
            await Task.Run(() =>
            {
                using (var g = Graphics.FromImage(newBm))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
                    g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.DrawImage(img, new Rectangle(0, 0, maxWidth, maxHeight), new Rectangle(0, 0, img.Width, img.Height), GraphicsUnit.Pixel);
                }
            });
            return newBm;
        }

        public static async void SaveAndReleaseAsync(this Image img, string filename)
        {
            await Task.Run(() =>
            {
                img.Save(filename);
            });
            img.Dispose();
        }
    }
}
