using CefSharp;
using CefSharp.DevTools;
using CefSharp.WinForms;
using Emgu.CV.OCR;
using GO2FlashLauncher.Model;
using GO2FlashLauncher.Service;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
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
                    await host.LeftClick(result.Value, rnd.Next(80, 100));
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
                    await host.LeftClick(result.Value, rnd.Next(80, 100));
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
            var crop = await bmp.Crop(new Point(0, 0), new Size(150, 500));
            var result = crop.FindImage(Path.GetFullPath("Images\\EZReward.png"), 0.95);
            if (result == null)
            {
                for (int i = 2; i < 3; i++)
                {
                    await Task.Delay(50);
                    result = crop.FindImage(Path.GetFullPath("Images\\EZReward" + i + ".png"), 0.95);
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
            var crop = await bmp.Crop(new Point(bmp.Width - 200, bmp.Height - 500), new Size(200, 500));
            var point = crop.FindImage("Images\\tools.png", 0.8);
            if (point == null)
            {
                for (int x = 2; x < 8; x++)
                {
                    await Task.Delay(10);
                    point = crop.FindImage("Images\\tools.png", 0.8);
                }
            }
            if (point == null)
            {
                return false;
            }
            await host.LeftClick(new Point(point.Value.X + bmp.Width - 200, point.Value.Y + bmp.Height - 500), rnd.Next(80, 100));
            bmp = await devtools.Screenshot();
            crop = await bmp.Crop(new Point(bmp.Width - 200, bmp.Height - 500), new Size(200, 500));
            var mail = crop.FindImage("Images\\toolsemailicon.png", 0.6);
            if (mail != null)
            {
                await host.LeftClick(new Point(mail.Value.X + bmp.Width - 200, mail.Value.Y + bmp.Height - 500), rnd.Next(80, 100));
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
                    await host.LeftClick(mail.Value, rnd.Next(80, 100));
                    await Task.Delay(300);
                    await host.LeftClick(mail.Value.X, mail.Value.Y + 125, rnd.Next(80, 100));
                    await Task.Delay(1000);
                    bmp = await devtools.Screenshot();
                    var collect = bmp.FindImageGrayscaled("Images\\allcharge.png", 0.7);
                    if (collect != null)
                    {
                        await host.LeftClick(collect.Value, rnd.Next(80, 100));
                        await Task.Delay(300);
                    }
                    collect = bmp.FindImageGrayscaled("Images\\maildelete.png", 0.7);
                    if (collect == null)
                    {
                        collect = bmp.FindImageGrayscaled("Images\\maildelete2.png", 0.7);
                    }
                    if (collect != null)
                    {
                        await host.LeftClick(collect.Value, rnd.Next(80, 100));
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
                        await host.LeftClick(collect.Value, rnd.Next(80, 100));
                        await Task.Delay(1000);
                        return true;
                    }
                }
            }
            return false;
        }

        public async Task<BaseResources> DetectResource(GO2HttpService httpService, int userID)
        {
            var result = await httpService.GetPlanets();
            var br = new BaseResources();
            var selectedPlanet = result.Data.FirstOrDefault(x => x.UserId == userID);
            br.HE3 = selectedPlanet.Resources.He3;
            br.Metal = selectedPlanet.Resources.Metal;
            br.Gold = selectedPlanet.Resources.Gold;
            br.MP = selectedPlanet.Resources.MallPoints;
            br.Vouchers = selectedPlanet.Resources.Vouchers;
            return br;
        }
    }
}
