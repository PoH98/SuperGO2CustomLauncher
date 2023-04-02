using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using GO2FlashLauncher.Service;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace GO2FlashLauncher.Script
{
    internal static class ImageDectection
    {
        public static Point? FindImage(this Bitmap bitmap, string findPath, double matchRadius)
        {
            Stopwatch watch = Stopwatch.StartNew();
            Image<Rgb, byte> source = bitmap.ToImage<Rgb, byte>();
            Image<Rgb, byte> find = new Image<Rgb, byte>(findPath);
            using (Image<Gray, float> result = source.MatchTemplate(find, TemplateMatchingType.CcoeffNormed))
            {
                result.MinMax(out double[] minValues, out double[] maxValues, out Point[] minLocations, out Point[] maxLocations);

                // You can try different values of the threshold. I guess somewhere between 0.85 and 0.95 would be good.
                if (maxValues[0] > matchRadius)
                {
                    watch.Stop();
                    Logger.LogDebug("Image detected with X:" + maxLocations[0].X + " Y:" + maxLocations[0].Y + " in " + watch.ElapsedMilliseconds + "ms");
                    return maxLocations[0];
                }
            }
            watch.Stop();
            find.Dispose();
            Logger.LogDebug("Image detection failed in " + watch.ElapsedMilliseconds + "ms");
            return null;
        }

        public static Point[] FindImageArray(this Bitmap bitmap, string findPath, double matchRadius)
        {
            Stopwatch watch = Stopwatch.StartNew();
            Image<Rgb, byte> source = bitmap.ToImage<Rgb, byte>();
            Image<Rgb, byte> find = new Image<Rgb, byte>(findPath);
            List<Point> points = new List<Point>();
            using (Image<Gray, float> result = source.MatchTemplate(find, TemplateMatchingType.CcoeffNormed))
            {
                _ = CvInvoke.Threshold(result, result, matchRadius, 1, ThresholdType.ToZero);
                Image<Gray, float> resultWithPadding = new Image<Gray, float>(source.Size);
                int heightOfPadding = (source.Height - result.Height) / 2;
                int widthOfPadding = (source.Width - result.Width) / 2;
                resultWithPadding.ROI = new Rectangle() { X = heightOfPadding, Y = widthOfPadding, Width = result.Width, Height = result.Height };
                result.CopyTo(resultWithPadding);
                resultWithPadding.ROI = Rectangle.Empty;

                for (int i = 0; i < resultWithPadding.Width; i++)
                {
                    for (int j = 0; j < resultWithPadding.Height; j++)
                    {
                        Point centerOfRoi = new Point() { X = i + (find.Width / 2), Y = j + (find.Height / 2) };
                        Rectangle roi = new Rectangle() { X = i, Y = j, Width = find.Width, Height = find.Height };
                        resultWithPadding.ROI = roi;
                        resultWithPadding.MinMax(out _, out _, out _, out Point[] maxLocations);
                        resultWithPadding.ROI = Rectangle.Empty;
                        Point maxLocation = maxLocations.First();
                        if (maxLocation.X == roi.Width / 2 && maxLocation.Y == roi.Height / 2)
                        {
                            Point point = new Point() { X = centerOfRoi.X, Y = centerOfRoi.Y };
                            points.Add(point);
                        }

                    }
                }
            }
            find.Dispose();
            watch.Stop();
            Logger.LogDebug("Image detected with " + points.Count + " points in " + watch.ElapsedMilliseconds + "ms");
            return points.ToArray();
        }

        public static Point? FindImageGrayscaled(this Bitmap bitmap, string findPath, double matchRadius)
        {
            Stopwatch watch = Stopwatch.StartNew();
            Image<Gray, byte> source = bitmap.ToImage<Gray, byte>();
            Image<Gray, byte> find = new Image<Gray, byte>(findPath);
            using (Image<Gray, float> result = source.MatchTemplate(find, TemplateMatchingType.CcoeffNormed))
            {
                result.MinMax(out double[] minValues, out double[] maxValues, out Point[] minLocations, out Point[] maxLocations);

                // You can try different values of the threshold. I guess somewhere between 0.85 and 0.95 would be good.
                if (maxValues[0] > matchRadius)
                {
                    watch.Stop();
                    Logger.LogDebug("Image detected with X:" + maxLocations[0].X + " Y:" + maxLocations[0].Y + " in " + watch.ElapsedMilliseconds + "ms");
                    return maxLocations[0];
                }
            }
            find.Dispose();
            watch.Stop();
            Logger.LogDebug("Image detection failed in " + watch.ElapsedMilliseconds + "ms");
            return null;
        }
    }
}
