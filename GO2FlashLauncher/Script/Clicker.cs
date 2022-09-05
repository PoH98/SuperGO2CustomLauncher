using CefSharp;
using System;
using System.Drawing;
using System.Threading.Tasks;

namespace GO2FlashLauncher.Script
{
    internal static class Clicker
    {
        private static readonly Random rnd = new Random();
        public static Task LeftClick(this IBrowserHost browser, Point point, int interval)
        {
            return LeftClick(browser, point.X, point.Y, interval);
        }
        public static async Task LeftClick(this IBrowserHost browser, int x, int y, int interval)
        {
            var rndX = rnd.Next(x - 1, x + 1);
            var rndY = rnd.Next(y - 1, y + 1);
            browser.SendMouseClickEvent(rndX, rndY, MouseButtonType.Left, false, 1, CefEventFlags.None);
            await Task.Delay(interval);
            browser.SendMouseClickEvent(rndX, rndY, MouseButtonType.Left, true, 1, CefEventFlags.None);
        }

        public static async Task RightClick(this IBrowserHost browser, int x, int y, int interval)
        {
            var rndX = rnd.Next(x - 1, x + 1);
            var rndY = rnd.Next(y - 1, y + 1);
            browser.SendMouseClickEvent(rndX, rndY, MouseButtonType.Right, false, 1, CefEventFlags.None);
            await Task.Delay(interval);
            browser.SendMouseClickEvent(rndX, rndY, MouseButtonType.Right, true, 1, CefEventFlags.None);
        }
    }
}
