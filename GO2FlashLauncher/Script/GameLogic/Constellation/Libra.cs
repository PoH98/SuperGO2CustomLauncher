using CefSharp;
using CefSharp.DevTools;
using System.Drawing;

namespace GO2FlashLauncher.Script.GameLogic.Constellation
{
    internal class Libra : AbstractConstellation
    {
        public Libra(DevToolsClient devtools, IBrowserHost host) : base(devtools, host)
        {
        }

        protected override Point Stage1(Point locate)
        {
            return new Point(locate.X - 20, locate.Y + 98);
        }

        protected override Point Stage2(Point locate)
        {
            return new Point(locate.X - 160, locate.Y + 108);
        }

        protected override Point Stage3(Point locate)
        {
            return new Point(locate.X - 45, locate.Y + 201);
        }

        protected override Point Stage4(Point locate)
        {
            return new Point(locate.X - 215, locate.Y + 216);
        }
    }
}
