using CefSharp;
using CefSharp.DevTools.Page;
using System.Drawing;


namespace GO2FlashLauncher.Script.GameLogic.Constellation
{
    internal class Cancer : AbstractConstellation
    {
        public Cancer(PageClient devtools, IBrowserHost host) : base(devtools, host)
        {
        }

        protected override Point Stage1(Point locate)
        {
            return new Point(locate.X - 224, locate.Y + 316);
        }

        protected override Point Stage2(Point locate)
        {
            return new Point(locate.X - 189, locate.Y + 231);
        }

        protected override Point Stage3(Point locate)
        {
            return new Point(locate.X - 126, locate.Y + 190);
        }

        protected override Point Stage4(Point locate)
        {
            return new Point(locate.X - 16, locate.Y + 185);
        }
    }
}
