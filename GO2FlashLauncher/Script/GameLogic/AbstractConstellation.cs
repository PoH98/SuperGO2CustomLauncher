using CefSharp;
using CefSharp.DevTools;
using System;
using System.Drawing;
using System.Threading.Tasks;

namespace GO2FlashLauncher.Script.GameLogic
{
    internal abstract class AbstractConstellation
    {
        private readonly DevToolsClient devtools;
        private readonly IBrowserHost host;
        private readonly Random rnd = new Random();
        public AbstractConstellation(DevToolsClient devtools, IBrowserHost host)
        {
            this.host = host;
            this.devtools = devtools;
        }

        protected abstract Point Stage1(Point locate);
        protected abstract Point Stage2(Point locate);
        protected abstract Point Stage3(Point locate);
        protected abstract Point Stage4(Point locate);
        public async Task EnterStage(int stage)
        {
            Bitmap bmp = await devtools.Screenshot();
            Point locateP = AllocateConstellation(bmp);
            switch (stage)
            {
                case 0:
                    Point p = Stage1(locateP);
                    await host.LeftClick(p, rnd.Next(50, 80));
                    break;
                case 1:
                    p = Stage2(locateP);
                    await host.LeftClick(p, rnd.Next(50, 80));
                    break;
                case 2:
                    p = Stage3(locateP);
                    await host.LeftClick(p, rnd.Next(50, 80));
                    break;
                case 3:
                    p = Stage4(locateP);
                    await host.LeftClick(p, rnd.Next(50, 80));
                    break;
            }
            bmp.Dispose();
        }

        protected virtual Point AllocateConstellation(Bitmap bmp)
        {
            Point? result = bmp.FindImage("Images\\Constellations.png", 0.7);
            if (result == null)
            {
                result = bmp.FindImage("Images\\Constellations2.png", 0.7);
            }
            if (result == null)
            {
                result = bmp.FindImage("Images\\Constellations3.png", 0.7);
            }
            return result == null ? throw new ArgumentException("Locate Constellation failed") : result.Value;
        }


    }
}
