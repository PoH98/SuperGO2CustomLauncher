using Emgu.CV;
using Emgu.CV.Structure;
using System.Drawing;

namespace GO2FlashLauncher.Script.GameLogic
{
    internal class LagDetection
    {

        public bool IsLagging(Bitmap bmp1, Bitmap bmp2, bool inStage)
        {
            using (Image<Gray, byte> sourceImage = bmp1.ToImage<Gray, byte>())
            {
                using (Image<Gray, byte> templateImage = bmp2.ToImage<Gray, byte>())
                {
                    Image<Gray, byte> resultImage = new Image<Gray, byte>(bmp1.Width / 2, bmp1.Height / 2);
                    CvInvoke.AbsDiff(sourceImage, templateImage, resultImage);
                    //resultImage.Save(@"some path" + "imagename.jpeg");
                    int diff = CvInvoke.CountNonZero(resultImage);
                    //if diff = 0 exact match, otherwise there are some difference.
                    if (inStage)
                    {
                        if(diff < 2000)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if(diff < 30000)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
