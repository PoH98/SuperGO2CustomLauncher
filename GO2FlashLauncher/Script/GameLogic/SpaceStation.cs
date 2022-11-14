using CefSharp;
using CefSharp.DevTools;
using CefSharp.WinForms;
using Emgu.CV;
using Emgu.CV.OCR;
using Emgu.CV.Structure;
using GO2FlashLauncher.Service;
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace GO2FlashLauncher.Script.GameLogic
{
    internal class SpaceStation
    {
        private readonly IBrowserHost host;
        private readonly DevToolsClient devtools;
        private readonly Random rnd = new Random();
        private readonly Tesseract ocr;
        public SpaceStation(ChromiumWebBrowser browser)
        {
            host = browser.GetBrowser().GetHost();
            this.devtools = browser.GetBrowser().GetDevToolsClient();
            ocr = new Tesseract("libs", "eng", OcrEngineMode.TesseractLstmCombined);
            ocr.SetVariable("tessedit_char_whitelist", "1234567890");
        }

        public async Task Enter(Bitmap bmp)
        {
            for (int x = 0; x < 10; x++)
            {
                var result = bmp.FindImage(Path.GetFullPath("Images\\spacebase.png"), 0.8);
                if (result == null)
                {
                    await Task.Delay(10);
                    result = bmp.FindImage(Path.GetFullPath("Images\\spacebase2.png"), 0.8);
                }
                if (result != null)
                {
                    await host.LeftClick(result.Value.X, result.Value.Y + 5, rnd.Next(10, 50));
                    return;
                }
            }
            throw new ImageNotFound("Spacebase button");
        }

        public async Task<Point?> Locate(Bitmap bmp)
        {
            var result = bmp.FindImage(Path.GetFullPath("Images\\station.png"), 0.8);
            if (result == null)
            {
                for (int i = 2; i < 8; i++)
                {
                    await Task.Delay(10);
                    result = bmp.FindImage(Path.GetFullPath("Images\\station" + i + ".png"), 0.8);
                    if (result != null)
                    {
                        break;
                    }
                }
            }
            return result;
        }
        public async Task<InstanceEnterState> EnterInstance(Bitmap bmp, int instanceLv)
        {
            var result = bmp.FindImage(Path.GetFullPath("Images\\instance.png"), 0.8);
            if (result == null)
            {
                await Task.Delay(10);
                result = bmp.FindImage(Path.GetFullPath("Images\\instance2.png"), 0.8);
            }
            if (result != null)
            {
                await host.LeftClick(result.Value, rnd.Next(10, 50));
                await Task.Delay(800);
                bmp = await devtools.Screenshot();

            }
            //detect already in instance
            if (bmp.FindImage("Images\\instanceWindowMarker.png", 0.65) != null)
            {
                result = bmp.FindImage("Images\\i" + instanceLv + "i.png", 0.8);
                if (result == null && File.Exists("Images\\i" + instanceLv + "i2.png"))
                {
                    result = bmp.FindImage("Images\\i" + instanceLv + "i2.png", 0.8);
                }
                if (result != null)
                {
                    await Task.Delay(500);
                    await host.LeftClick(result.Value, rnd.Next(80, 100));
                    await Task.Delay(500);
                    return InstanceEnterState.IncreaseFleet;
                }
            }
            else if (bmp.FindImage("Images\\instancestop.png", 0.8) != null)
            {
                //in stage
                return InstanceEnterState.InStage;
            }
            return InstanceEnterState.Error;
        }

        public async Task<InstanceEnterState> EnterRestrict(Bitmap bmp, int instanceLv)
        {
            var result = bmp.FindImage(Path.GetFullPath("Images\\instance.png"), 0.8);
            if (result == null)
            {
                await Task.Delay(10);
                result = bmp.FindImage(Path.GetFullPath("Images\\instance2.png"), 0.8);
            }
            if (result != null)
            {
                await host.LeftClick(result.Value, rnd.Next(10, 50));
                await Task.Delay(1000);
                bmp = await devtools.Screenshot();
            }
            //detect already in instance
            if (bmp.FindImage("Images\\instanceWindowMarker.png", 0.65) != null)
            {
                result = bmp.FindImage("Images\\restricted.png", 0.7);
                if (result != null)
                {
                    await host.LeftClick(result.Value, rnd.Next(10, 50));
                    await Task.Delay(300);
                    bmp = await devtools.Screenshot();
                }
                //read OCR restrict count left
                var ocrPoint = bmp.FindImage("Images\\restrictRemaining.png", 0.7);
                if (ocrPoint != null)
                {
                    var crop = await bmp.Crop(new Point(ocrPoint.Value.X + 61, ocrPoint.Value.Y), new Size(15, 20));
                    ocr.SetImage(crop.ToImage<Gray, byte>());
                    var str = ocr.GetUTF8Text();
                    int.TryParse(str, out int count);
                    if (count == 0)
                    {
                        throw new ArgumentException("Already out of chance");
                    }
                    Logger.LogInfo("Restrict remaining chance: " + count);
                }
                result = bmp.FindImage("Images\\r" + instanceLv + "r.png", 0.8);
                if (result == null && File.Exists("Images\\r" + instanceLv + "r1.png"))
                {
                    result = bmp.FindImage("Images\\r" + instanceLv + "r1.png", 0.8);
                }
                if (result != null)
                {
                    await Task.Delay(500);
                    await host.LeftClick(result.Value, rnd.Next(80, 100));
                    await Task.Delay(500);
                    return InstanceEnterState.IncreaseFleet;
                }
            }
            else if (bmp.FindImage("Images\\instancestop.png", 0.8) != null)
            {
                //in stage
                return InstanceEnterState.InStage;
            }
            return InstanceEnterState.Error;
        }

        public async Task<(InstanceEnterState, int)> EnterTrial(Bitmap bmp)
        {
            var result = bmp.FindImage(Path.GetFullPath("Images\\instance.png"), 0.8);
            if (result == null)
            {
                await Task.Delay(10);
                result = bmp.FindImage(Path.GetFullPath("Images\\instance2.png"), 0.8);
            }
            if (result != null)
            {
                await host.LeftClick(result.Value, rnd.Next(10, 50));
                await Task.Delay(1000);
                bmp = await devtools.Screenshot();
            }
            //detect already in instance
            if (bmp.FindImage("Images\\instanceWindowMarker.png", 0.65) != null)
            {
                result = bmp.FindImage("Images\\trial.png", 0.7);
                if (result != null)
                {
                    await host.LeftClick(result.Value, rnd.Next(10, 50));
                    await Task.Delay(300);
                    bmp = await devtools.Screenshot();
                    for (int i = 1; i <= 10; i++)
                    {
                        result = bmp.FindImage("Images\\t" + i + "t.png", 0.8);
                        if (result == null)
                        {
                            if(i - 1 == 0)
                            {
                                return (InstanceEnterState.InstanceCompleted, 0);
                            }
                            return (InstanceEnterState.IncreaseFleet, i - 1);
                        }
                    }
                    return (InstanceEnterState.IncreaseFleet, 1);
                }
            }
            else if (bmp.FindImage("Images\\instancestop.png", 0.8) != null)
            {
                //in stage
                return (InstanceEnterState.InStage, -1);
            }
            return (InstanceEnterState.Error, -1);
        }

        public async Task<InstanceEnterState> EnterConstellations(Bitmap bmp, Constellations constellations, int stage)
        {
            var result = bmp.FindImage(Path.GetFullPath("Images\\instance.png"), 0.8);
            if (result == null)
            {
                await Task.Delay(10);
                result = bmp.FindImage(Path.GetFullPath("Images\\instance2.png"), 0.8);
            }
            if (result != null)
            {
                await host.LeftClick(result.Value, rnd.Next(10, 50));
                await Task.Delay(1000);
                bmp = await devtools.Screenshot();
            }
            //detect already in instance
            if (bmp.FindImage("Images\\instanceWindowMarker.png", 0.65) != null)
            {
                result = bmp.FindImage("Images\\Constellations.png", 0.7);
                if(result == null)
                {
                    result = bmp.FindImage("Images\\Constellations2.png", 0.7);
                }
                if (result == null)
                {
                    result = bmp.FindImage("Images\\Constellations3.png", 0.7);
                }
                if (result != null)
                {
                    await host.LeftClick(result.Value, rnd.Next(10, 50));
                    await Task.Delay(200);
                    bmp = await devtools.Screenshot();
                    result = bmp.FindImage("Images\\Const"+constellations.ToString() + ".png", 0.8);
                    if(result != null)
                    {
                        await host.LeftClick(result.Value, rnd.Next(10, 50));
                        await Task.Delay(200);
                        bmp = await devtools.Screenshot();
                    }
                }
            }
            return InstanceEnterState.Error;
        }
    }

    public enum InstanceEnterState
    {
        Error,
        InStage,
        IncreaseFleet,
        InstanceCompleted
    }

    public enum Constellations
    {
        Aquarius,
        Aries,
        Cancer,
        Capricorn,
        Leo,
        Libra,
        Pisces,
        Sagitarius,
        Scorpio,
        Taurus,
        Virgo
    }
}
