using CefSharp;
using CefSharp.DevTools;
using CefSharp.WinForms;
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
        public SpaceStation(ChromiumWebBrowser browser)
        {
            host = browser.GetBrowser().GetHost();
            this.devtools = browser.GetBrowser().GetDevToolsClient();
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
                await Task.Delay(800);
                bmp = await devtools.Screenshot();
            }
            result = bmp.FindImage("Images\\restricted.png", 0.7);
            if(result != null)
            {
                await host.LeftClick(result.Value, rnd.Next(10, 50));
                await Task.Delay(300);
                bmp = await devtools.Screenshot();
            }
            //detect already in instance
            if (bmp.FindImage("Images\\instanceWindowMarker.png", 0.65) != null)
            {
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
    }

    public enum InstanceEnterState
    {
        Error,
        InStage,
        IncreaseFleet
    }
}
