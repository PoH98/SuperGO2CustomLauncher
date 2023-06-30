using CefSharp;
using CefSharp.DevTools.Page;
using System;
using System.Drawing;
using System.Threading.Tasks;

namespace GO2FlashLauncher.Script.GameLogic
{
    internal class Inventory
    {
        private readonly IBrowserHost host;
        private readonly Random rnd = new Random();
        private readonly PageClient pageClient;

        public Inventory(IBrowserHost host, PageClient pageClient)
        {
            this.host = host;
            this.pageClient = pageClient;
        }
        /// <summary>
        /// Open inventory window
        /// </summary>
        /// <returns></returns>
        public async Task<bool> OpenInventory(Bitmap bmp)
        {
            Bitmap crop = await bmp.Crop(new Point(bmp.Width - 200, bmp.Height - 500), new Size(200, 500));
            Point? point = crop.FindImage("Images\\tools.png", 0.8);
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
            await Task.Delay(300);
            bmp = await pageClient.Screenshot();
            crop = await bmp.Crop(new Point(bmp.Width - 300, bmp.Height - 500), new Size(300, 500));
            point = crop.FindImageGrayscaled("Images\\bag.png", 0.8);
            if (point == null)
            {
                for (int x = 2; x < 4; x++)
                {
                    await Task.Delay(10);
                    point = crop.FindImageGrayscaled("Images\\bag" + x + ".png", 0.8);
                    if (point != null)
                    {
                        break;
                    }
                }
            }
            if (point == null)
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
            for (int y = 0; y < loopCount; y++)
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
                        await Task.Delay(200);
                    }
                    bmp = await pageClient.Screenshot();
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
                bmp = await pageClient.Screenshot();
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
                bmp = await pageClient.Screenshot();
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
                await Task.Delay(800);
            }
            //success
            return true;
        }

        public async Task<bool> OpenTruce(Bitmap bmp)
        {
            Point? point;
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
            await host.LeftClick(point.Value, rnd.Next(80, 100));
            await Task.Delay(200);
            bmp = await pageClient.Screenshot();
            point = bmp.FindImage("Images\\truced.png", 0.7);
            if (point == null)
            {
                for (int x = 2; x < 4; x++)
                {
                    point = bmp.FindImage("Images\\truced" + x + ".png", 0.7);
                    if (point != null)
                    {
                        break;
                    }
                }
            }
            if (point == null)
            {
                //error
                return false;
            }
            await host.LeftClick(point.Value, rnd.Next(80, 100));
            await Task.Delay(100);
            bmp = await pageClient.Screenshot();
            Point? bagpoint = bmp.FindImage("Images\\returnbag.png", 0.7);
            if (bagpoint != null)
            {
                await host.LeftClick(point.Value, rnd.Next(80, 100));
                await Task.Delay(100);
                bmp = await pageClient.Screenshot();
                point = bmp.FindImage("Images\\OK.png", 0.7);
                if (point == null)
                {
                    for (int x = 2; x < 5; x++)
                    {
                        point = bmp.FindImage("Images\\OK" + x + ".png", 0.7);
                        if (point != null)
                        {
                            break;
                        }
                    }
                }
                if (point == null)
                {
                    //error
                    throw new ArgumentException("Weird error occurs");
                }
                await host.LeftClick(point.Value, rnd.Next(80, 100));
                await Task.Delay(100);
                bmp = await pageClient.Screenshot();
                point = bmp.FindImage("Images\\truced.png", 0.7);
                if (point == null)
                {
                    for (int x = 2; x < 4; x++)
                    {
                        point = bmp.FindImage("Images\\truced" + x + ".png", 0.7);
                        if (point != null)
                        {
                            break;
                        }
                    }
                }
                if (point == null)
                {
                    //error
                    return false;
                }
                await host.LeftClick(point.Value, rnd.Next(80, 100));
            }
            await Task.Delay(50);
            bmp = await pageClient.Screenshot();
            point = bmp.FindImage("Images//usebtn.png", 0.8);
            if (point == null)
            {
                //error
                return false;
            }
            await host.LeftClick(point.Value, rnd.Next(80, 100));
            return true;
        }
    }
}
