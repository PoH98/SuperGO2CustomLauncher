using CefSharp;
using System.Drawing;
using System.Threading.Tasks;

namespace GO2FlashLauncher.Script
{
    internal static class Clicker
    {
        public static Task LeftClick(this IBrowserHost browser, Point point, int interval)
        {
            return LeftClick(browser, point.X, point.Y, interval);
        }
        public static async Task LeftClick(this IBrowserHost browser, int x, int y, int interval)
        {
            browser.SendMouseClickEvent(x, y, MouseButtonType.Left, false, 1, CefEventFlags.None);
            await Task.Delay(interval);
            browser.SendMouseClickEvent(x, y, MouseButtonType.Left, true, 1, CefEventFlags.None);
        }

        public static async Task RightClick(this IBrowserHost browser, int x, int y, int interval)
        {
            browser.SendMouseClickEvent(x, y, MouseButtonType.Right, false, 1, CefEventFlags.None);
            await Task.Delay(interval);
            browser.SendMouseClickEvent(x, y, MouseButtonType.Right, true, 1, CefEventFlags.None);
        }
    }
}
