using CefSharp.DevTools;
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
        [DllImport("user32.dll")]
        private static extern int GetWindowRgn(IntPtr hWnd, IntPtr hRgn);

        //Region Flags - The return value specifies the type of the region that the function obtains. It can be one of the following values.
        private const int ERROR = 0;
        private const int NULLREGION = 1;
        private const int SIMPLEREGION = 2;
        private const int COMPLEXREGION = 3;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(HandleRef hWnd, out RECT lpRect);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PrintWindow(IntPtr hwnd, IntPtr hDC, uint nFlags);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool BitBlt(IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }

            public RECT(System.Drawing.Rectangle r) : this(r.Left, r.Top, r.Right, r.Bottom) { }

            public int X
            {
                get => Left;
                set { Right -= Left - value; Left = value; }
            }

            public int Y
            {
                get => Top;
                set { Bottom -= Top - value; Top = value; }
            }

            public int Height
            {
                get => Bottom - Top;
                set => Bottom = value + Top;
            }

            public int Width
            {
                get => Right - Left;
                set => Right = value + Left;
            }

            public System.Drawing.Point Location
            {
                get => new System.Drawing.Point(Left, Top);
                set { X = value.X; Y = value.Y; }
            }

            public System.Drawing.Size Size
            {
                get => new System.Drawing.Size(Width, Height);
                set { Width = value.Width; Height = value.Height; }
            }

            public static implicit operator System.Drawing.Rectangle(RECT r)
            {
                return new System.Drawing.Rectangle(r.Left, r.Top, r.Width, r.Height);
            }

            public static implicit operator RECT(System.Drawing.Rectangle r)
            {
                return new RECT(r);
            }

            public static bool operator ==(RECT r1, RECT r2)
            {
                return r1.Equals(r2);
            }

            public static bool operator !=(RECT r1, RECT r2)
            {
                return !r1.Equals(r2);
            }

            public bool Equals(RECT r)
            {
                return r.Left == Left && r.Top == Top && r.Right == Right && r.Bottom == Bottom;
            }

            public override bool Equals(object obj)
            {
                if (obj is RECT)
                {
                    return Equals((RECT)obj);
                }
                else if (obj is System.Drawing.Rectangle)
                {
                    return Equals(new RECT((System.Drawing.Rectangle)obj));
                }

                return false;
            }

            public override int GetHashCode()
            {
                return ((System.Drawing.Rectangle)this).GetHashCode();
            }

            public override string ToString()
            {
                return string.Format(System.Globalization.CultureInfo.CurrentCulture, "{{Left={0},Top={1},Right={2},Bottom={3}}}", Left, Top, Right, Bottom);
            }
        }
        private static Bitmap GetScreenshot(IntPtr ihandle)
        {
            IntPtr hwnd = ihandle;//handle here

            _ = GetWindowRect(new HandleRef(null, hwnd), out RECT rc);

            Bitmap bmp = new Bitmap(rc.Right - rc.Left, rc.Bottom - rc.Top, PixelFormat.Format32bppArgb);
            Graphics gfxBmp = Graphics.FromImage(bmp);
            IntPtr hdcBitmap;
            try
            {
                hdcBitmap = gfxBmp.GetHdc();
            }
            catch
            {
                return null;
            }
            _ = BitBlt(hdcBitmap, 0, 0, bmp.Width, bmp.Height, ihandle, 0, 0, 0x00CC0020);
            //bool succeeded = PrintWindow(hwnd, hdcBitmap, 0);
            gfxBmp.ReleaseHdc(hdcBitmap);
            /*if (!succeeded)
            {
                gfxBmp.FillRectangle(new SolidBrush(Color.Gray), new Rectangle(Point.Empty, bmp.Size));
            }
            IntPtr hRgn = CreateRectRgn(0, 0, 0, 0);
            GetWindowRgn(hwnd, hRgn);
            Region region = Region.FromHrgn(hRgn);//err here once
            if (!region.IsEmpty(gfxBmp))
            {
                gfxBmp.ExcludeClip(region);
                gfxBmp.Clear(Color.Transparent);
            }*/
            gfxBmp.Dispose();
            return bmp;
        }

        public static Task<Bitmap> Screenshot(this ChromiumWebBrowser browser)
        {
            return Task.Run(() =>
            {
                return (Bitmap)browser.Invoke((Func<Bitmap>)delegate
                {
                    return GetScreenshot(browser.Handle);
                });
            });
        }

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
