using CefSharp;
using CefSharp.DevTools.Page;
using System.Drawing;

namespace GO2FlashLauncher.Script.GameLogic.Constellation
{
    internal class Taurus : AbstractConstellation
    {
        public Taurus(PageClient devtools, IBrowserHost host) : base(devtools, host)
        {
        }

        protected override Point Stage1(Point locate)
        {
            return new Point(locate.X - 201, locate.Y + 271);
        }

        protected override Point Stage2(Point locate)
        {
            return new Point(locate.X - 97, locate.Y + 335);
        }

        protected override Point Stage3(Point locate)
        {
            return new Point(locate.X - 244, locate.Y + 336);
        }

        protected override Point Stage4(Point locate)
        {
            return new Point(locate.X - 183, locate.Y + 335);
        }
    }
}
