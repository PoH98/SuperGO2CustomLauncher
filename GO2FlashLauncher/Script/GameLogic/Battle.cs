﻿using CefSharp;
using CefSharp.DevTools;
using CefSharp.WinForms;
using GO2FlashLauncher.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GO2FlashLauncher.Script.GameLogic
{
    internal class Battle
    {
        private readonly IBrowserHost host;
        private readonly Random rnd = new Random();
        private readonly DevToolsClient devtools;
        public Battle(ChromiumWebBrowser browser)
        {
            this.devtools = browser.GetBrowser().GetDevToolsClient();
            host = browser.GetBrowser().GetHost();
        }
        public async Task<bool> SelectFleet(Bitmap bmp, List<Fleet> fleets, SelectFleetType fleetType)
        {
            var crop = await bmp.Crop(new Point(250, 150), new Size(bmp.Width - 300, bmp.Height - 300));
            var detectedFleets = crop.FindImageArray("Images\\fleettransmittimemarker.png", 0.8);
            if (detectedFleets.Length < 1)
            {
                //no fleet
                return false;
            }
            var firstFleet = detectedFleets.OrderBy(x => x.X).First();
            var clickPoint = new Point(firstFleet.X + 300, firstFleet.Y + 20);
            var currentPage = 0;
            bmp = await devtools.Screenshot();
            foreach (Fleet f in fleets.OrderBy(x => x.Order))
            {
                if (f.Order < 0)
                {
                    continue;
                }
                var index = fleets.IndexOf(f);
                var page = index / 9;
                if (index >= 9)
                {
                    index = index % 9;
                }
                if (currentPage != page)
                {
                    if (currentPage > page)
                    {
                        var prev = bmp.FindImage("Images\\selectshipsprevpage.png", 0.7);
                        if (prev == null)
                        {
                            await Task.Delay(100);
                            prev = bmp.FindImage("Images\\selectshipsprevpage2.png", 0.7);
                        }
                        if (prev != null)
                        {
                            await host.LeftClick(prev.Value, rnd.Next(120, 150));
                            await Task.Delay(500);
                        }
                    }
                    else if (currentPage < page)
                    {
                        var next = bmp.FindImage("Images\\selectshipsnextpage.png", 0.7);
                        if (next == null)
                        {
                            await Task.Delay(100);
                            next = bmp.FindImage("Images\\selectshipsnextpage2.png", 0.7);
                        }
                        if (next != null)
                        {
                            await host.LeftClick(next.Value, rnd.Next(120, 150));
                            await Task.Delay(500);
                        }
                    }
                }
                currentPage = page;
                switch (index)
                {
                    case 0:
                        await host.LeftClick(rnd.Next(clickPoint.X, clickPoint.X + 10), rnd.Next(clickPoint.Y, clickPoint.Y + 5), rnd.Next(120, 150));
                        break;
                    case 1:
                        await host.LeftClick(rnd.Next(clickPoint.X + 170, clickPoint.X + 180), rnd.Next(clickPoint.Y, clickPoint.Y + 5), rnd.Next(120, 150));
                        break;
                    case 2:
                        await host.LeftClick(rnd.Next(clickPoint.X + 380, clickPoint.X + 390), rnd.Next(clickPoint.Y, clickPoint.Y + 5), rnd.Next(120, 150));
                        break;
                    case 3:
                        await host.LeftClick(rnd.Next(clickPoint.X, clickPoint.X + 10), rnd.Next(clickPoint.Y + 100, clickPoint.Y + 105), rnd.Next(120, 150));
                        break;
                    case 4:
                        await host.LeftClick(rnd.Next(clickPoint.X + 170, clickPoint.X + 180), rnd.Next(clickPoint.Y + 100, clickPoint.Y + 105), rnd.Next(120, 150));
                        break;
                    case 5:
                        await host.LeftClick(rnd.Next(clickPoint.X + 380, clickPoint.X + 390), rnd.Next(clickPoint.Y + 100, clickPoint.Y + 105), rnd.Next(120, 150));
                        break;
                    case 6:
                        await host.LeftClick(rnd.Next(clickPoint.X, clickPoint.X + 10), rnd.Next(clickPoint.Y + 200, clickPoint.Y + 205), rnd.Next(120, 150));
                        break;
                    case 7:
                        await host.LeftClick(rnd.Next(clickPoint.X + 170, clickPoint.X + 180), rnd.Next(clickPoint.Y + 200, clickPoint.Y + 205), rnd.Next(120, 150));
                        break;
                    case 8:
                        await host.LeftClick(rnd.Next(clickPoint.X + 380, clickPoint.X + 390), rnd.Next(clickPoint.Y + 200, clickPoint.Y + 205), rnd.Next(120, 150));
                        break;
                }
                await Task.Delay(rnd.Next(100, 120));
            }
            if (fleetType == SelectFleetType.Instance)
            {
                bmp = await devtools.Screenshot();
                var result = bmp.FindImageGrayscaled("Images\\OK.png", 0.7);
                if (result == null)
                {
                    for (int i = 2; i < 7; i++)
                    {
                        await Task.Delay(10);
                        result = bmp.FindImageGrayscaled("Images\\OK" + i + ".png", 0.7);
                        if (result != null)
                        {
                            break;
                        }
                    }
                }
                if (result != null)
                {
                    await host.LeftClick(result.Value, rnd.Next(120, 150));
                    return true;
                }
            }
            return false;
        }

        public async Task<bool> BattleEnds(Bitmap bmp)
        {
            var result = bmp.FindImageGrayscaled(Path.GetFullPath("Images\\win.png"), 0.7);
            if (result != null)
            {
                return await CloseButtons(bmp);
            }
            return false;
        }

        public async Task<bool> CloseButtons(Bitmap bmp)
        {
            var result = bmp.FindImageGrayscaled("Images\\close.png", 0.7);
            if (result == null)
            {
                for (int x = 2; x < 20; x++)
                {
                    await Task.Delay(10);
                    result = bmp.FindImageGrayscaled("Images\\close" + x + ".png", 0.7);
                    if (result != null)
                    {
                        break;
                    }
                }
            }
            if (result != null)
            {
                await host.LeftClick(result.Value, rnd.Next(120, 150));
                await Task.Delay(1000);
                return true;
            }
            return false;
        }

        public async Task<bool> RefillHE3(Bitmap bmp, BaseResources resources, long HaltOn)
        {
            var fullysupply = true;
            if(resources.HE3 <= HaltOn)
            {
                return false;
            }
            var result = bmp.FindImage(Path.GetFullPath("Images\\fleetsupplies.png"), 0.8);
            if (result == null)
            {
                result = bmp.FindImage(Path.GetFullPath("Images\\fleetsupplies2.png"), 0.8);
            }
            if (result != null)
            {
                await host.LeftClick(result.Value, rnd.Next(120, 150));
                await Task.Delay(1000);
                bmp = await devtools.Screenshot();
                result = bmp.FindImage("Images\\supplyall.png", 0.8);
                if (result != null)
                {
                    await host.LeftClick(result.Value, rnd.Next(120, 150));
                }
                result = bmp.FindImage("Images\\supplyconfirm.png", 0.8);
                if (result == null)
                {
                    for (int i = 1; i < 2; i++)
                    {
                        await Task.Delay(50);
                        result = bmp.FindImage(Path.GetFullPath("Images\\supplyconfirm" + i + ".png"), 0.8);
                        if (result != null)
                        {
                            break;
                        }
                    }
                }
                if (result != null)
                {
                    await host.LeftClick(result.Value, rnd.Next(120, 150));
                }
            }
            return fullysupply;
        }

        public async Task<bool> IncreaseFleet(Bitmap bmp)
        {
            var result = bmp.FindImageGrayscaled("Images\\increasefleet.png", 0.6);
            if (result == null)
            {
                for (int i = 2; i < 7; i++)
                {
                    await Task.Delay(10);
                    result = bmp.FindImageGrayscaled(Path.GetFullPath("Images\\increasefleet" + i + ".png"), 0.6);
                    if (result != null)
                    {
                        break;
                    }
                }
            }
            if (result != null)
            {
                await host.LeftClick(result.Value, rnd.Next(120, 150));
                await Task.Delay(2000);
                return true;
            }
            return false;
        }
    }

    internal enum SelectFleetType
    {
        Instance
    }
}
