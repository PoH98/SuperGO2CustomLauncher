using CefSharp;
using CefSharp.DevTools;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GO2FlashLauncher.Script.GameLogic.Constellation
{
    internal class Libra : AbstractConstellation
    {
        public Libra(DevToolsClient devtools, IBrowserHost host) : base(devtools, host)
        {
        }

        protected override Point Stage1(Point locate)
        {
            return new Point(locate.X - 20, locate.Y + 215);
        }

        protected override Point Stage2(Point locate)
        {
            return new Point(locate.X - 155, locate.Y + 221);
        }

        protected override Point Stage3(Point locate)
        {
            return new Point(locate.X - 45, locate.Y + 333);
        }

        protected override Point Stage4(Point locate)
        {
            return new Point(locate.X - 206, locate.Y + 316);
        }
    }
}
