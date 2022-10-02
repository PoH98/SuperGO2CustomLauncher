using CefSharp;
using CefSharp.DevTools;
using CefSharp.WinForms;
using GO2FlashLauncher.Model;
using System;
using System.Drawing;
using System.Threading.Tasks;

namespace GO2FlashLauncher.Script.GameLogic
{
    internal class Wheel
    {
        private readonly IBrowserHost host;
        private readonly Random rnd = new Random();
        private readonly DevToolsClient devtools;
        public Wheel(ChromiumWebBrowser browser)
        {
            this.devtools = browser.GetBrowser().GetDevToolsClient();
            host = browser.GetBrowser().GetHost();
        }

        public async Task<bool> GetIn(Bitmap bmp)
        {
            var crop = await bmp.Crop(new Point(bmp.Width - 200, bmp.Height - 500), new Size(200, 500));
            var point = crop.FindImage("Images\\wheelmenu.png", 0.7);
            if(point == null)
            {
                for(int x = 2; x < 4; x++)
                {
                    await Task.Delay(10);
                    point = crop.FindImage("Images\\wheelmenu2.png", 0.7);
                    if(point != null)
                    {
                        break;
                    }
                }
            }
            if(point != null)
            {
                await host.LeftClick(new Point(point.Value.X + bmp.Width - 200, point.Value.Y + bmp.Height - 500), rnd.Next(80, 100));
                await Task.Delay(300);
                bmp = await devtools.Screenshot();
                crop = await bmp.Crop(new Point(bmp.Width - 200, bmp.Height - 500), new Size(200, 500));
                point = crop.FindImage("Images\\wheel.png", 0.7);
                if(point == null)
                {
                    await Task.Delay(10);
                    point = crop.FindImage("Images\\wheel2.png", 0.7);
                }
                if(point != null)
                {
                    await host.LeftClick(new Point(point.Value.X + bmp.Width - 200, point.Value.Y + bmp.Height - 500), rnd.Next(80, 100));
                    return true;
                }
                else
                {
                    //click anywhere to close the wheelmenu
                    await host.LeftClick(new Point(50, 50), rnd.Next(80, 100));
                }
            }
            return false;
        }

        public async Task<bool> Spin(Bitmap bmp, BaseResources resources)
        {
            if(resources.Vouchers < 5)
            {
                //no more spins
                return true;
            }
            var point = bmp.FindImage("Images\\spin.jpg", 0.7);
            if(point == null)
            {
                point = bmp.FindImage("Images\\spin1.png", 0.7);
            }
            if(point == null)
            {
                return false;
            }
            await host.LeftClick(point.Value, rnd.Next(80, 100));
            await Task.Delay(1000);
            bmp = await devtools.Screenshot();
            point = bmp.FindImage("Images\\wheelbuyandspin.png", 0.7);
            if(point == null)
            {
                for(int x = 2; x < 5; x++)
                {
                    await Task.Delay(10);
                    point = bmp.FindImage("Images\\wheelbuyandspin"+x+".png", 0.7);
                    if(point != null)
                    {
                        break;
                    }
                }
            }
            if(point != null)
            {
                //have to use vouchers
                resources.Vouchers -= 5;
                var voucher = bmp.FindImage("Images\\vouchers.png", 0.7);
                if(voucher == null)
                {
                    return false;
                }
                await host.LeftClick(new Point(voucher.Value.X - 10, voucher.Value.Y + 2), rnd.Next(80,100));
                await Task.Delay(500);
                await host.LeftClick(point.Value, rnd.Next(80, 100)); 
            }
            int error = 0;
            do
            {
                await Task.Delay(1000);
                //scan for share
                bmp = await devtools.Screenshot();
                point = bmp.FindImage("Images\\closeshare.png", 0.7);
                if (point != null)
                {
                    break;
                }
                error++;
            } while (error < 20);
            if (point != null)
            {
                await Task.Delay(500);
                //scan for share
                bmp = await devtools.Screenshot();
                point = bmp.FindImage("Images\\closeshare.png", 0.7);
                await host.LeftClick(new Point(point.Value.X + 10, point.Value.Y), rnd.Next(80, 100));
                await Task.Delay(500);
                return true;
            }
            return true;
        }

        public async Task<bool> EndSpin(Bitmap bmp)
        {
            var point = bmp.FindImage("Images\\close18.png", 0.7);
            if(point == null)
            {
                point = bmp.FindImage("Images\\close19.png", 0.7);
            }
            if(point != null)
            {
                await host.LeftClick(point.Value, rnd.Next(80, 100));
                return true;
            }
            return false;
        }
    }
}
