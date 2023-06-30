using CefSharp;
using CefSharp.DevTools.Page;
using System.Drawing;

namespace GO2FlashLauncher.Script.GameLogic.Constellation
{
    internal class Leo : AbstractConstellation
    {
        public Leo(PageClient devtools, IBrowserHost host) : base(devtools, host)
        {
        }

        protected override Point Stage1(Point locate)
        {
            return new Point(locate.X + 62, locate.Y + 191);
        }

        protected override Point Stage2(Point locate)
        {
            return new Point(locate.X + 36, locate.Y + 150);
        }

        protected override Point Stage3(Point locate)
        {
            return new Point(locate.X - 26, locate.Y + 109);
        }

        protected override Point Stage4(Point locate)
        {
            return new Point(locate.X - 64, locate.Y + 160);
        }
    }
}
