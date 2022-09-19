using CefSharp;
using CefSharp.DevTools;
using CefSharp.WinForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GO2FlashLauncher.Script.GameLogic
{
    internal class Inventory
    {
        private readonly IBrowserHost host;
        private readonly Random rnd = new Random();
        private readonly DevToolsClient devtools;

        public Inventory(ChromiumWebBrowser browser)
        {
            devtools = browser.GetBrowser().GetDevToolsClient();
            host = browser.GetBrowser().GetHost();
        }
        /// <summary>
        /// Open inventory window
        /// </summary>
        /// <returns></returns>
        public async Task<bool> OpenInventory(Bitmap bmp)
        {
            var crop = await bmp.Crop(new Point(bmp.Width - 200, bmp.Height - 500), new Size(200, 500));
            var point = crop.FindImage("Images\\tools.png", 0.8);
            if (point == null)
            {
                for(int x = 2;x < 8; x++)
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
            await Task.Delay(300);
            bmp = await devtools.Screenshot();
            crop = await bmp.Crop(new Point(bmp.Width - 300, bmp.Height - 500), new Size(300, 500));
            point = crop.FindImageGrayscaled("Images\\bag.png", 0.8);
            if(point == null)
            {
                for(int x = 2; x < 4; x++)
                {
                    await Task.Delay(10);
                    point = crop.FindImageGrayscaled("Images\\bag"+x+".png", 0.8);
                    if(point != null)
                    {
                        break;
                    }
                }
            }
            if(point == null)
            {
                return false;
            }
            await host.LeftClick(new Point(point.Value.X + bmp.Width - 300, point.Value.Y + bmp.Height - 500), rnd.Next(80, 100));
            await Task.Delay(1500);
            return true;
        }

        public async Task<bool> OpenTreasury(Bitmap bmp, int loopCount = 5)
        {
            bool filtered = false;
            for(int y = 0; y < loopCount; y++)
            {
                Point? point;
                int loop = 0;
                do
                {
                    if (!filtered)
                    {
                        //click on filter
                        point = bmp.FindImage("Images\\bagitemfilter.png", 0.7);
                        if (point == null)
                        {
                            point = bmp.FindImage("Images\\bagitemfilter2.png", 0.7);
                            if (point == null)
                            {
                                //error
                                return false;
                            }
                        }
                        filtered = true;
                        await host.LeftClick(point.Value, rnd.Next(80, 100));
                        await Task.Delay(500);
                    }
                    bmp = await devtools.Screenshot();
                    //find treasure box
                    point = bmp.FindImage("Images\\treasurebox.png", 0.8);
                    if (point == null)
                    {
                        for (int x = 2; x < 8; x++)
                        {
                            await Task.Delay(10);
                            point = bmp.FindImage("Images\\treasurebox" + x + ".png", 0.8);
                            if (point != null)
                            {
                                break;
                            }
                        }
                    }
                    if (point == null)
                    {
                        //maybe is in next page
                        point = bmp.FindImage("Images\\bagnext.png", 0.8);
                        if (point != null)
                        {
                            await host.LeftClick(point.Value, rnd.Next(80, 100));
                        }
                    }
                    else
                    {
                        //we already found the box, lets start open it
                        break;
                    }
                    loop++;
                }
                while (loop < 5);
                if (point == null)
                {
                    //nothing found, exit
                    return false;
                }
                await host.LeftClick(point.Value, rnd.Next(80, 100));
                await Task.Delay(500);
                //open button
                bmp = await devtools.Screenshot();
                point = bmp.FindImage("Images\\usebtn.png", 0.8);
                if (point == null)
                {
                    //click away
                    await host.LeftClick(new Point(250, 250), rnd.Next(80, 100));
                    //exit
                    return false;
                }
                await host.LeftClick(point.Value, rnd.Next(80, 100));
                //clicked use
                await Task.Delay(1000);
                bmp = await devtools.Screenshot();
                //close reward window
                point = bmp.FindImage("Images\\treasurerewardconfirm.png", 0.8);
                if (point == null)
                {
                    point = bmp.FindImage("Images\\treasurerewardconfirm2.png", 0.8);
                }
                if (point == null)
                {
                    //error, exit
                    return false;
                }
                await host.LeftClick(point.Value, rnd.Next(80, 100));
            }
            //success
            return true;
        }
    }
}
