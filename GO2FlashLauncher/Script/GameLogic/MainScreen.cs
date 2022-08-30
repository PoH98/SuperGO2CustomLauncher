using CefSharp;
using CefSharp.DevTools;
using CefSharp.WinForms;
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace GO2FlashLauncher.Script.GameLogic
{
    internal class MainScreen
    {
        private readonly IBrowserHost host;
        private readonly Random rnd = new Random();
        private readonly DevToolsClient devtools;
        public MainScreen(ChromiumWebBrowser browser)
        {
            devtools = browser.GetBrowser().GetDevToolsClient();
            host = browser.GetBrowser().GetHost();
        }
        public async Task<bool> Locate(Bitmap bmp)
        {
            var result = bmp.FindImage(Path.GetFullPath("Images\\cancel.png"), 0.8);
            if(result == null)
            {
                for(int i = 2; i < 7; i++)
                {
                    await Task.Delay(50);
                    result = bmp.FindImage(Path.GetFullPath("Images\\cancel"+i+".png"), 0.8);
                    if(result != null)
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
            if(result != null)
            {
                await host.LeftClick(result.Value, rnd.Next(100, 150));
                await Task.Delay(500);
                return true;
            }
            return false;
        }

        public async Task<bool> LocateWarehouse(Bitmap bmp)
        {
            var result = bmp.FindImage(Path.GetFullPath("Images\\warehouse.png"), 0.8);
            if(result == null)
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
            if(result == null)
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
            if(result != null)
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
                if(mail == null)
                {
                    for (int x = 2; x < 4; x++)
                    {
                        await Task.Delay(10);
                        mail = bmp.FindImage("Images\\mailitemfilter"+x+".png", 0.7);
                        if(mail != null)
                        {
                            break;
                        }
                    }
                }
                if(mail != null)
                {
                    await host.LeftClick(mail.Value, rnd.Next(100, 150));
                    await Task.Delay(300);
                    while (true)
                    {
                        await host.LeftClick(mail.Value.X, mail.Value.Y + 125, rnd.Next(100, 150));
                        await Task.Delay(1000);
                        bmp = await devtools.Screenshot();
                        var collect = bmp.FindImageGrayscaled("Images\\allcharge.png", 0.7);
                        if(collect != null)
                        {
                            await host.LeftClick(collect.Value, rnd.Next(100, 150));
                            await Task.Delay(300);
                        }
                        collect = bmp.FindImageGrayscaled("Images\\maildelete.png", 0.7);
                        if(collect == null)
                        {
                            collect = bmp.FindImageGrayscaled("Images\\maildelete2.png", 0.7);
                        }
                        if(collect != null)
                        {
                            await host.LeftClick(collect.Value, rnd.Next(100, 150));
                            await Task.Delay(300);
                        }
                        else
                        {
                            //no more mails found as no more can be deleted
                            collect = bmp.FindImageGrayscaled("Images\\mailclose.png", 0.7);
                            if(collect == null)
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
                        //collected one mail, check if have another one
                        await Task.Delay(1000);
                    }

                }
            }
            return false;
        }
    }
}
