using CefSharp;
using CefSharp.DevTools;
using CefSharp.WinForms;
using Emgu.CV;
using Emgu.CV.OCR;
using Emgu.CV.Structure;
using GO2FlashLauncher.Model;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GO2FlashLauncher.Script.GameLogic
{
    internal class MainScreen
    {
        private readonly IBrowserHost host;
        private readonly Random rnd = new Random();
        private readonly DevToolsClient devtools;
        private readonly Tesseract ocr;

        public MainScreen(ChromiumWebBrowser browser)
        {
            devtools = browser.GetBrowser().GetDevToolsClient();
            host = browser.GetBrowser().GetHost();
            ocr = new Tesseract("libs", "eng", OcrEngineMode.TesseractLstmCombined);
            ocr.SetVariable("tessedit_char_whitelist", "1234567890");
        }
        public async Task<bool> Locate(Bitmap bmp, bool clickBase = true)
        {
            var result = bmp.FindImage(Path.GetFullPath("Images\\cancel.png"), 0.8);
            if (result == null)
            {
                for (int i = 2; i < 7; i++)
                {
                    await Task.Delay(50);
                    result = bmp.FindImage(Path.GetFullPath("Images\\cancel" + i + ".png"), 0.8);
                    if (result != null)
                    {
                        break;
                    }
                }
            }
            if (result != null)
            {
                await host.LeftClick(result.Value, rnd.Next(50, 150));
                return true;
            }
            if (clickBase)
            {
                if (result == null)
                {
                    result = bmp.FindImage(Path.GetFullPath("Images\\home.png"), 0.8);
                }
                if (result == null)
                {
                    for (int i = 2; i < 3; i++)
                    {
                        await Task.Delay(50);
                        result = bmp.FindImage(Path.GetFullPath("Images\\home" + i + ".png"), 0.8);
                        if (result != null)
                        {
                            break;
                        }
                    }
                }
                if (result != null)
                {
                    await host.LeftClick(result.Value, rnd.Next(100, 150));
                    await Task.Delay(500);
                }
                result = bmp.FindImage("Images\\groundbase.png", 0.8);
                if (result == null)
                {
                    for (int i = 2; i < 3; i++)
                    {
                        await Task.Delay(50);
                        result = bmp.FindImage(Path.GetFullPath("Images\\groundbase" + i + ".png"), 0.8);
                        if (result != null)
                        {
                            break;
                        }
                    }
                }
                if (result != null)
                {
                    await host.LeftClick(result.Value, rnd.Next(100, 150));
                    await Task.Delay(500);
                    return true;
                }
            }
            else
            {
                return true;
            }
            return false;
        }

        public async Task<bool> LocateWarehouse(Bitmap bmp)
        {
            var result = bmp.FindImage(Path.GetFullPath("Images\\warehouse.png"), 0.8);
            if (result == null)
            {
                for (int i = 2; i < 9; i++)
                {
                    await Task.Delay(50);
                    result = bmp.FindImage(Path.GetFullPath("Images\\warehouse" + i + ".png"), 0.8);
                    if (result != null)
                    {
                        break;
                    }
                }
            }
            if (result == null)
            {
                return false;
            }
            else
            {
                await host.LeftClick(result.Value, rnd.Next(10, 50));
                return true;
            }
        }

        public async Task<bool> Collect(Bitmap bmp)
        {
            var result = bmp.FindImage(Path.GetFullPath("Images\\harvest.png"), 0.8);
            if (result == null)
            {
                for (int i = 2; i < 3; i++)
                {
                    await Task.Delay(50);
                    result = bmp.FindImage(Path.GetFullPath("Images\\harvest" + i + ".png"), 0.8);
                    if (result != null)
                    {
                        break;
                    }
                }
            }
            if (result == null)
            {
                return false;
            }
            else
            {
                await host.LeftClick(result.Value.X + 10, result.Value.Y + 5, rnd.Next(100, 200));
                return true;
            }
        }

        public async Task<bool> EZRewards(Bitmap bmp)
        {
            var result = bmp.FindImage(Path.GetFullPath("Images\\EZReward.png"), 0.95);
            if (result == null)
            {
                for (int i = 2; i < 3; i++)
                {
                    await Task.Delay(50);
                    result = bmp.FindImage(Path.GetFullPath("Images\\EZReward" + i + ".png"), 0.95);
                    if (result != null)
                    {
                        break;
                    }
                }
            }
            if (result != null)
            {
                await host.LeftClick(result.Value.X + 10, result.Value.Y + 5, rnd.Next(100, 200));
                return true;
            }
            return false;
        }

        public bool DetectDisconnect(Bitmap bmp)
        {
            var result = bmp.FindImage(Path.GetFullPath("Images\\disconnected.png"), 0.8);
            return result.HasValue;
        }

        public async Task<bool> CollectMails(Bitmap bmp)
        {
            var mail = bmp.FindImage("Images\\mail.png", 0.6);
            if (mail != null)
            {
                await host.LeftClick(mail.Value, rnd.Next(100, 150));
                await Task.Delay(1000);
                bmp = await devtools.Screenshot();
                mail = bmp.FindImage("Images\\mailitemfilter.png", 0.7);
                if (mail == null)
                {
                    for (int x = 2; x < 4; x++)
                    {
                        await Task.Delay(10);
                        mail = bmp.FindImage("Images\\mailitemfilter" + x + ".png", 0.7);
                        if (mail != null)
                        {
                            break;
                        }
                    }
                }
                if (mail != null)
                {
                    await host.LeftClick(mail.Value, rnd.Next(100, 150));
                    await Task.Delay(300);
                    await host.LeftClick(mail.Value.X, mail.Value.Y + 125, rnd.Next(100, 150));
                    await Task.Delay(1000);
                    bmp = await devtools.Screenshot();
                    var collect = bmp.FindImageGrayscaled("Images\\allcharge.png", 0.7);
                    if (collect != null)
                    {
                        await host.LeftClick(collect.Value, rnd.Next(100, 150));
                        await Task.Delay(300);
                    }
                    collect = bmp.FindImageGrayscaled("Images\\maildelete.png", 0.7);
                    if (collect == null)
                    {
                        collect = bmp.FindImageGrayscaled("Images\\maildelete2.png", 0.7);
                    }
                    if (collect != null)
                    {
                        await host.LeftClick(collect.Value, rnd.Next(100, 150));
                        await Task.Delay(300);
                    }
                    //close mailbox
                    collect = bmp.FindImageGrayscaled("Images\\mailclose.png", 0.7);
                    if (collect == null)
                    {
                        collect = bmp.FindImage("Images\\mailclose2.png", 0.7);
                    }
                    if (collect != null)
                    {
                        await host.LeftClick(collect.Value, rnd.Next(100, 150));
                        await Task.Delay(1000);
                        return true;
                    }
                }
            }
            return false;
        }

        public async Task<BaseResources> DetectResource(Bitmap bmp)
        {
            var metal = await bmp.Crop(new Point(bmp.Width - 210, 22), new Size(90, 17));
            var he3 = await bmp.Crop(new Point(bmp.Width - 210, 41), new Size(90, 17));
            var gold = await bmp.Crop(new Point(bmp.Width - 106, 22), new Size(90, 17));
            var result = new BaseResources();
            Image<Gray, byte> mi = metal.ToImage<Gray, byte>();
            Image<Gray, byte> hi = he3.ToImage<Gray, byte>();
            Image<Gray, byte> gl = gold.ToImage<Gray, byte>();
            ocr.SetImage(mi);
            ocr.Recognize();
            var m = long.Parse(new String(ocr.GetUTF8Text().Where(Char.IsDigit).ToArray()));
            ocr.SetImage(hi);
            ocr.Recognize();
            var h = long.Parse(new String(ocr.GetUTF8Text().Where(Char.IsDigit).ToArray()));
            ocr.SetImage(gl);
            ocr.Recognize();
            var g = long.Parse(new String(ocr.GetUTF8Text().Where(Char.IsDigit).ToArray()));
            result.Metal = m;
            result.HE3 = h;
            result.Gold = g;
            return result;
        }
    }
}
