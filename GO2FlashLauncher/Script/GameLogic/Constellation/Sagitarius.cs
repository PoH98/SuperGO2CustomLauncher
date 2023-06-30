using CefSharp;
using CefSharp.DevTools.Page;
using System.Drawing;

namespace GO2FlashLauncher.Script.GameLogic.Constellation
{
    internal class Sagitarius : AbstractConstellation
    {
        public Sagitarius(PageClient devtools, IBrowserHost host) : base(devtools, host)
        {
        }

        protected override Point Stage1(Point locate)
        {
            return new Point(locate.X - 33, locate.Y + 228);
        }

        protected override Point Stage2(Point locate)
        {
            return new Point(locate.X - 69, locate.Y + 281);
        }

        protected override Point Stage3(Point locate)
        {
            return new Point(locate.X - 100, locate.Y + 337);
        }

        protected override Point Stage4(Point locate)
        {
            return new Point(locate.X - 176, locate.Y + 395);
        }
    }
}
