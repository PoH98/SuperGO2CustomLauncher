using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.Collections.Generic;
using System.Drawing;

namespace GO2FlashLauncher.Script
{
    internal static class ImageDectection
    {
        public static Point? FindImage(this Bitmap bitmap, string findPath, double matchRadius)
        {
            Image<Rgb, byte> source = bitmap.ToImage<Rgb, byte>();
            Image<Rgb, byte> find = new Image<Rgb, byte>(findPath);
            using (Image<Gray, float> result = source.MatchTemplate(find, TemplateMatchingType.CcoeffNormed))
            {
                result.MinMax(out double[] minValues, out double[] maxValues, out Point[] minLocations, out Point[] maxLocations);

                // You can try different values of the threshold. I guess somewhere between 0.85 and 0.95 would be good.
                if (maxValues[0] > matchRadius)
                {
                    return maxLocations[0];
                }
            }
            return null;
        }

        public static Point[] FindImageArray(this Bitmap bitmap, string findPath, double matchRadius)
        {
            Image<Rgb, byte> source = bitmap.ToImage<Rgb, byte>();
            Image<Rgb, byte> find = new Image<Rgb, byte>(findPath);
            List<Point> points = new List<Point>();
            using (Image<Gray, float> result = source.MatchTemplate(find, TemplateMatchingType.CcoeffNormed))
            {
                CvInvoke.Threshold(result, result, matchRadius, 1, ThresholdType.ToZero);
                result.MinMax(out double[] minValues, out double[] maxValues, out Point[] minLocations, out Point[] maxLocations);

                // You can try different values of the threshold. I guess somewhere between 0.85 and 0.95 would be good.
                for (int x = 0; x < maxValues.Length; x++)
                {
                    if (maxValues[x] > matchRadius)
                    {
                        points.Add(maxLocations[x]);
                    }
                }
            }
            return points.ToArray();
        }

        public static Point? FindImageGrayscaled(this Bitmap bitmap, string findPath, double matchRadius)
        {
            Image<Gray, byte> source = bitmap.ToImage<Gray, byte>();
            Image<Gray, byte> find = new Image<Gray, byte>(findPath);
            using (Image<Gray, float> result = source.MatchTemplate(find, TemplateMatchingType.CcoeffNormed))
            {
                result.MinMax(out double[] minValues, out double[] maxValues, out Point[] minLocations, out Point[] maxLocations);

                // You can try different values of the threshold. I guess somewhere between 0.85 and 0.95 would be good.
                if (maxValues[0] > matchRadius)
                {
                    return maxLocations[0];
                }
            }
            return null;
        }
    }
}
