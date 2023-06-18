using CefSharp;
using CefSharp.DevTools;
using CefSharp.DevTools.Page;
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
        private readonly PageClient pageClient;

        public Battle(IBrowserHost host, PageClient pageClient)
        {
            this.host = host;
            this.pageClient = pageClient;
        }
        public async Task<bool> SelectFleet(Bitmap bmp, List<Fleet> fleets, SelectFleetType fleetType, int instanceLv, Constellations constellations)
        {
            Bitmap crop = await bmp.Crop(new Point(200, 150), new Size(bmp.Width - 300, bmp.Height - 300));
            Point[] detectedFleets = crop.FindImageArray("Images\\fleettransmittimemarker.png", 0.75);
            if (detectedFleets.Length < 1)
            {
                //no fleet
                return false;
            }
            Point firstFleet = detectedFleets.OrderBy(x => x.X).OrderBy(x => x.Y).First();
            Point clickPoint = new Point(firstFleet.X + 250, firstFleet.Y + 150);
            int maxFleetNum = 15;
            switch (fleetType)
            {
                case SelectFleetType.Restrict:
                    switch (instanceLv)
                    {
                        case 1:
                            maxFleetNum = 10;
                            break;
                        case 2:
                            maxFleetNum = 12;
                            break;
                        case 3:
                            maxFleetNum = 15;
                            break;
                        case 4:
                            maxFleetNum = 18;
                            break;
                        case 5:
                        case 6:
                        case 7:
                            maxFleetNum = 20;
                            break;
                        case 8:
                            maxFleetNum = 21;
                            break;
                        case 9:
                        case 10:
                            maxFleetNum = 25;
                            break;
                    }
                    break;
                case SelectFleetType.Instance:
                    switch (instanceLv)
                    {
                        case 1:
                            maxFleetNum = 3;
                            break;
                        case 2:
                        case 3:
                            maxFleetNum = 4;
                            break;
                        case 4:
                        case 5:
                            maxFleetNum = 5;
                            break;
                        case 6:
                            maxFleetNum = 6;
                            break;
                        case 7:
                        case 8:
                        case 9:
                            maxFleetNum = 8;
                            break;
                        case 10:
                        case 11:
                        case 12:
                        case 13:
                        case 14:
                            maxFleetNum = 10;
                            break;
                        case 15:
                        case 16:
                        case 17:
                        case 18:
                        case 19:
                        case 20:
                            maxFleetNum = 12;
                            break;
                    }
                    break;
                case SelectFleetType.Trial:
                    maxFleetNum = 4;
                    break;
                case SelectFleetType.Constellation:
                    switch (constellations)
                    {
                        case Constellations.Aquarius:
                            switch (instanceLv)
                            {
                                case 1:
                                case 2:
                                    maxFleetNum = 6;
                                    break;
                                case 3:
                                    maxFleetNum = 1;
                                    break;
                                case 4:
                                    maxFleetNum = 8;
                                    break;
                            }
                            break;
                        case Constellations.Aries:
                            switch (instanceLv)
                            {
                                case 1:
                                case 2:
                                    maxFleetNum = 8;
                                    break;
                                case 3:
                                    maxFleetNum = 10;
                                    break;
                                case 4:
                                    maxFleetNum = 12;
                                    break;
                            }
                            break;
                        case Constellations.Cancer:
                            switch (instanceLv)
                            {
                                case 1:
                                case 2:
                                    maxFleetNum = 8;
                                    break;
                                case 3:
                                    maxFleetNum = 10;
                                    break;
                                case 4:
                                    maxFleetNum = 12;
                                    break;
                            }
                            break;
                        case Constellations.Capricorn:
                            maxFleetNum = 6;
                            break;
                    }
                    break;
            }
            int currentPage = 0;
            bmp = await pageClient.Screenshot();
            IEnumerable<Fleet> selectedFleets = fleets.Where((x) =>
            {
                switch (fleetType)
                {
                    case SelectFleetType.Instance:
                        return x.Order >= 0;
                    case SelectFleetType.Trial:
                        return x.TrialOrder >= 0;
                    case SelectFleetType.Restrict:
                        return x.RestrictOrder >= 0;
                    default:
                        return x.ConstellationOrder >= 0;
                }
            }).OrderBy((x) =>
            {
                switch (fleetType)
                {
                    case SelectFleetType.Instance:
                        return x.Order;
                    case SelectFleetType.Trial:
                        return x.TrialOrder;
                    case SelectFleetType.Restrict:
                        return x.RestrictOrder;
                    default:
                        return x.ConstellationOrder;
                }
            }).Take(maxFleetNum);
            foreach (Fleet f in selectedFleets)
            {
                int index = fleets.IndexOf(f);
                int page = index / 9;
                if (index >= 9)
                {
                    index %= 9;
                }
                if (currentPage != page)
                {
                    if (currentPage > page)
                    {
                        Point? prev = bmp.FindImage("Images\\selectshipsprevpage.png", 0.6);
                        if (prev == null)
                        {
                            await Task.Delay(100);
                            prev = bmp.FindImage("Images\\selectshipsprevpage2.png", 0.6);
                        }
                        if (prev != null)
                        {
                            await host.LeftClick(prev.Value, rnd.Next(40, 80));
                            await Task.Delay(300);
                        }
                        else
                        {
                            throw new ImageNotFound("SelectShipsPrevPage");
                        }
                    }
                    else if (currentPage < page)
                    {
                        Point? next = bmp.FindImage("Images\\selectshipsnextpage.png", 0.6);
                        if (next == null)
                        {
                            await Task.Delay(100);
                            next = bmp.FindImage("Images\\selectshipsnextpage2.png", 0.6);
                        }
                        if (next != null)
                        {
                            await host.LeftClick(next.Value, rnd.Next(40, 80));
                            await Task.Delay(300);
                        }
                        else
                        {
                            throw new ImageNotFound("SelectShipsNextPage");
                        }
                    }
                }
                currentPage = page;
                switch (index)
                {
                    case 0:
                        await host.LeftClick(rnd.Next(clickPoint.X, clickPoint.X + 10), rnd.Next(clickPoint.Y, clickPoint.Y + 5), rnd.Next(39, 50));
                        break;
                    case 1:
                        await host.LeftClick(rnd.Next(clickPoint.X + 170, clickPoint.X + 180), rnd.Next(clickPoint.Y, clickPoint.Y + 5), rnd.Next(39, 50));
                        break;
                    case 2:
                        await host.LeftClick(rnd.Next(clickPoint.X + 380, clickPoint.X + 390), rnd.Next(clickPoint.Y, clickPoint.Y + 5), rnd.Next(39, 50));
                        break;
                    case 3:
                        await host.LeftClick(rnd.Next(clickPoint.X, clickPoint.X + 10), rnd.Next(clickPoint.Y + 100, clickPoint.Y + 105), rnd.Next(39, 50));
                        break;
                    case 4:
                        await host.LeftClick(rnd.Next(clickPoint.X + 170, clickPoint.X + 180), rnd.Next(clickPoint.Y + 100, clickPoint.Y + 105), rnd.Next(39, 50));
                        break;
                    case 5:
                        await host.LeftClick(rnd.Next(clickPoint.X + 380, clickPoint.X + 390), rnd.Next(clickPoint.Y + 100, clickPoint.Y + 105), rnd.Next(39, 50));
                        break;
                    case 6:
                        await host.LeftClick(rnd.Next(clickPoint.X, clickPoint.X + 10), rnd.Next(clickPoint.Y + 200, clickPoint.Y + 205), rnd.Next(39, 50));
                        break;
                    case 7:
                        await host.LeftClick(rnd.Next(clickPoint.X + 170, clickPoint.X + 180), rnd.Next(clickPoint.Y + 200, clickPoint.Y + 205), rnd.Next(39, 50));
                        break;
                    case 8:
                        await host.LeftClick(rnd.Next(clickPoint.X + 380, clickPoint.X + 390), rnd.Next(clickPoint.Y + 200, clickPoint.Y + 205), rnd.Next(39, 50));
                        break;
                }
                await Task.Delay(rnd.Next(20, 50));
            }
            if (fleetType == SelectFleetType.Instance || fleetType == SelectFleetType.Restrict || fleetType == SelectFleetType.Trial || fleetType == SelectFleetType.Constellation)
            {
                bmp = await pageClient.Screenshot();
                Point? result = bmp.FindImageGrayscaled("Images\\OK.png", 0.7);
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
                    await host.LeftClick(result.Value, rnd.Next(39, 50));
                    return true;
                }
            }
            return false;
        }

        public async Task<bool> BattleEnds(Bitmap bmp)
        {
            Point? result = bmp.FindImageGrayscaled(Path.GetFullPath("Images\\win.png"), 0.7);
            return result != null && await CloseButtons(bmp);
        }

        public async Task<bool> CloseButtons(Bitmap bmp)
        {
            Point? result = bmp.FindImageGrayscaled("Images\\close.png", 0.7);
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
                await host.LeftClick(result.Value, rnd.Next(39, 50));
                await Task.Delay(500);
                return true;
            }
            return false;
        }

        public async Task<bool> RefillHE3(Bitmap bmp, BaseResources resources, decimal HaltOn)
        {
            bool fullysupply = true;
            if (resources.HE3 <= HaltOn)
            {
                return false;
            }
            Point? result = bmp.FindImage(Path.GetFullPath("Images\\fleetsupplies.png"), 0.8);
            if (result == null)
            {
                result = bmp.FindImage(Path.GetFullPath("Images\\fleetsupplies2.png"), 0.8);
            }
            if (result != null)
            {
                await host.LeftClick(result.Value, rnd.Next(39, 50));
                await Task.Delay(800);
                bmp = await pageClient.Screenshot();
                result = bmp.FindImage("Images\\supplyall.png", 0.8);
                if (result != null)
                {
                    await host.LeftClick(result.Value, rnd.Next(39, 50));
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
                    await host.LeftClick(result.Value, rnd.Next(39, 50));
                }
            }
            return fullysupply;
        }

        public async Task<bool> IncreaseFleet(Bitmap bmp)
        {
            Point? result = bmp.FindImageGrayscaled("Images\\increasefleet.png", 0.6);
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
                await host.LeftClick(result.Value, rnd.Next(39, 50));
                await Task.Delay(1000);
                return true;
            }
            return false;
        }
    }

    internal enum SelectFleetType
    {
        Instance,
        Restrict,
        Trial,
        Constellation
    }
}
