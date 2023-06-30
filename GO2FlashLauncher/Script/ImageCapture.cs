using CefSharp.DevTools.Page;
using CefSharp.WinForms;
using GO2FlashLauncher.Service;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace GO2FlashLauncher.Script
{
    internal static class ImageCapture
    {
        public static async Task<Bitmap> Screenshot(this PageClient pageClient)
        {
            try
            {
                Stopwatch watch = Stopwatch.StartNew();
                CaptureScreenshotResponse bmpdata = await pageClient.CaptureScreenshotAsync();
                Bitmap bmp;
                using (MemoryStream ms = new MemoryStream(bmpdata.Data))
                {
                    bmp = new Bitmap(ms);
                }
                watch.Stop();
                Logger.LogDebug("Image captured with " + bmp.Width + "x" + bmp.Height + " in " + watch.ElapsedMilliseconds + "ms");
                return bmp;
            }
            catch
            {
                return null;
            }

        }

        public static Task<Bitmap> Crop(this Bitmap bmp, Point start, Size size)
        {
            return Task.Run(() =>
            {
                Stopwatch watch = Stopwatch.StartNew();
                Rectangle rect = new Rectangle(start, size);
                BitmapData sourceBitmapData = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);

                Bitmap destBitmap = new Bitmap(rect.Width, rect.Height, PixelFormat.Format24bppRgb);
                BitmapData destBitmapData = destBitmap.LockBits(new Rectangle(0, 0, rect.Width, rect.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);

                int[] pixels = new int[rect.Width * rect.Height];
                Marshal.Copy(sourceBitmapData.Scan0, pixels, 0, pixels.Length);
                Marshal.Copy(pixels, 0, destBitmapData.Scan0, pixels.Length);

                bmp.UnlockBits(sourceBitmapData);
                destBitmap.UnlockBits(destBitmapData);
                watch.Stop();
                Logger.LogDebug("Image cropped with " + bmp.Width + "x" + bmp.Height + " in " + watch.ElapsedMilliseconds + "ms");
                return destBitmap;
            });
        }
    }
}
