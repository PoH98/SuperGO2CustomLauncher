using CefSharp;
using CefSharp.DevTools;
using System.Drawing;


namespace GO2FlashLauncher.Script.GameLogic.Constellation
{
    internal class Aries : AbstractConstellation
    {
        public Aries(DevToolsClient devtools, IBrowserHost host) : base(devtools, host)
        {
        }

        protected override Point Stage1(Point locate)
        {
            return new Point(locate.X - 15, locate.Y + 165);
        }

        protected override Point Stage2(Point locate)
        {
            return new Point(locate.X - 39, locate.Y + 186);
        }

        protected override Point Stage3(Point locate)
        {
            return new Point(locate.X - 71, locate.Y + 151);
        }

        protected override Point Stage4(Point locate)
        {
            return new Point(locate.X - 64, locate.Y + 221);
        }
    }
}
