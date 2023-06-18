using CefSharp;
using CefSharp.DevTools;
using CefSharp.DevTools.Page;
using System.Drawing;

namespace GO2FlashLauncher.Script.GameLogic.Constellation
{
    internal class Scorpio : AbstractConstellation
    {
        public Scorpio(PageClient devtools, IBrowserHost host) : base(devtools, host)
        {
        }

        protected override Point Stage1(Point locate)
        {
            return new Point(locate.X - 6, locate.Y + 240);
        }

        protected override Point Stage2(Point locate)
        {
            return new Point(locate.X + 36, locate.Y + 241);
        }

        protected override Point Stage3(Point locate)
        {
            return new Point(locate.X + 54, locate.Y + 302);
        }

        protected override Point Stage4(Point locate)
        {
            return new Point(locate.X - 16, locate.Y + 313);
        }
    }
}
