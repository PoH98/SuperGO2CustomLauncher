﻿using CefSharp;
using CefSharp.DevTools.Page;
using System.Drawing;

namespace GO2FlashLauncher.Script.GameLogic.Constellation
{
    internal class Pisces : AbstractConstellation
    {
        public Pisces(PageClient devtools, IBrowserHost host) : base(devtools, host)
        {
        }

        protected override Point Stage1(Point locate)
        {
            return new Point(locate.X + 25, locate.Y + 206);
        }

        protected override Point Stage2(Point locate)
        {
            return new Point(locate.X - 11, locate.Y + 238);
        }

        protected override Point Stage3(Point locate)
        {
            return new Point(locate.X, locate.Y + 280);
        }

        protected override Point Stage4(Point locate)
        {
            return new Point(locate.X + 44, locate.Y + 273);
        }
    }
}
