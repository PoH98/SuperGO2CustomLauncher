using CefSharp;
using CefSharp.DevTools.Page;
using System.Drawing;

namespace GO2FlashLauncher.Script.GameLogic.Constellation
{
    internal class Aquarius : AbstractConstellation
    {
        public Aquarius(PageClient devtools, IBrowserHost host) : base(devtools, host)
        {
        }

        protected override Point Stage1(Point locate)
        {
            return new Point(locate.X - 40, locate.Y + 230);
        }

        protected override Point Stage2(Point locate)
        {
            return new Point(locate.X - 105, locate.Y + 286);
        }

        protected override Point Stage3(Point locate)
        {
            return new Point(locate.X - 129, locate.Y + 154);
        }

        protected override Point Stage4(Point locate)
        {
            return new Point(locate.X - 123, locate.Y + 83);
        }
    }
}
