using CefSharp;
using CefSharp.DevTools;
using System.Drawing;

namespace GO2FlashLauncher.Script.GameLogic.Constellation
{
    internal class Virgo : AbstractConstellation
    {
        public Virgo(DevToolsClient devtools, IBrowserHost host) : base(devtools, host)
        {
        }

        protected override Point Stage1(Point locate)
        {
            return new Point(locate.X - 94, locate.Y + 188);
        }

        protected override Point Stage2(Point locate)
        {
            return new Point(locate.X - 69, locate.Y + 248);
        }

        protected override Point Stage3(Point locate)
        {
            return new Point(locate.X - 90, locate.Y + 347);
        }

        protected override Point Stage4(Point locate)
        {
            return new Point(locate.X - 152, locate.Y + 235);
        }
    }
}
