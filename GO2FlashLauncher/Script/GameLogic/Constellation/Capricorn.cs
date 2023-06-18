using CefSharp;
using CefSharp.DevTools;
using CefSharp.DevTools.Page;
using System.Drawing;

namespace GO2FlashLauncher.Script.GameLogic.Constellation
{
    internal class Capricorn : AbstractConstellation
    {
        public Capricorn(PageClient devtools, IBrowserHost host) : base(devtools, host)
        {
        }

        protected override Point Stage1(Point locate)
        {
            return new Point(locate.X - 205, locate.Y + 95);
        }

        protected override Point Stage2(Point locate)
        {
            return new Point(locate.X - 180, locate.Y + 118);
        }

        protected override Point Stage3(Point locate)
        {
            return new Point(locate.X - 137, locate.Y + 216);
        }

        protected override Point Stage4(Point locate)
        {
            return new Point(locate.X - 146, locate.Y + 276);
        }
    }
}
