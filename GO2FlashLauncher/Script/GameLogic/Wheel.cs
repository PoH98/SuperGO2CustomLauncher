﻿using CefSharp;
using CefSharp.DevTools.Page;
using GO2FlashLauncher.Model;
using System;
using System.Drawing;
using System.Threading.Tasks;

namespace GO2FlashLauncher.Script.GameLogic
{
    internal class Wheel
    {
        private readonly IBrowserHost host;
        private readonly Random rnd = new Random();
        private readonly PageClient pageClient;
        public Wheel(IBrowserHost host, PageClient pageClient)
        {
            this.host = host;
            this.pageClient = pageClient;
        }

        public async Task<bool> GetIn(Bitmap bmp)
        {
            Bitmap crop = await bmp.Crop(new Point(bmp.Width - 200, bmp.Height - 500), new Size(200, 500));
            Point? point = crop.FindImage("Images\\wheelmenu.png", 0.7);
            if (point == null)
            {
                for (int x = 2; x < 4; x++)
                {
                    await Task.Delay(10);
                    point = crop.FindImage("Images\\wheelmenu2.png", 0.7);
                    if (point != null)
                    {
                        break;
                    }
                }
            }
            if (point != null)
            {
                await host.LeftClick(new Point(point.Value.X + bmp.Width - 200, point.Value.Y + bmp.Height - 500), rnd.Next(80, 100));
                await Task.Delay(300);
                bmp = await pageClient.Screenshot();
                crop = await bmp.Crop(new Point(bmp.Width - 200, bmp.Height - 500), new Size(200, 500));
                point = crop.FindImage("Images\\wheel.png", 0.7);
                if (point == null)
                {
                    await Task.Delay(10);
                    point = crop.FindImage("Images\\wheel2.png", 0.7);
                }
                if (point != null)
                {
                    await host.LeftClick(new Point(point.Value.X + bmp.Width - 200, point.Value.Y + bmp.Height - 500), rnd.Next(80, 100));
                    return true;
                }
                else
                {
                    //click anywhere to close the wheelmenu
                    await host.LeftClick(new Point(50, 50), rnd.Next(80, 100));
                }
            }
            return false;
        }

        public async Task<SpinResult> Spin(Bitmap bmp, BaseResources resources, bool spinWithVoucher = true)
        {
            if (resources.Vouchers < 5)
            {
                //no more spins
                return SpinResult.NotEnoughVouchers;
            }
            Point? point = bmp.FindImage("Images\\spin.jpg", 0.7);
            if (point == null)
            {
                point = bmp.FindImage("Images\\spin1.png", 0.7);
            }
            if (point == null)
            {
                return SpinResult.Failed;
            }
            await host.LeftClick(point.Value, rnd.Next(80, 100));
            await Task.Delay(500);
            bmp = await pageClient.Screenshot();
            point = bmp.FindImage("Images\\wheelbuyandspin.png", 0.7);
            if (point == null)
            {
                for (int x = 2; x < 5; x++)
                {
                    await Task.Delay(10);
                    point = bmp.FindImage("Images\\wheelbuyandspin" + x + ".png", 0.7);
                    if (point != null)
                    {
                        break;
                    }
                }
            }
            SpinResult result = SpinResult.Failed;
            if (point != null && spinWithVoucher)
            {
                //have to use vouchers
                resources.Vouchers -= 5;
                Point? voucher = bmp.FindImage("Images\\vouchers.png", 0.7);
                if (voucher == null)
                {
                    return result;
                }
                await host.LeftClick(new Point(voucher.Value.X - 10, voucher.Value.Y + 2), rnd.Next(80, 100));
                await Task.Delay(100);
                await host.LeftClick(point.Value, rnd.Next(80, 100));
                result = SpinResult.Vouchers;
            }
            else if (point != null)
            {
                Point? skip = bmp.FindImage("Images\\spinskip.png", 0.7);
                if (skip == null)
                {
                    skip = bmp.FindImage("Images\\spinskip2.png", 0.7);
                }
                if (skip == null)
                {
                    skip = bmp.FindImage("Images\\spinskip3.png", 0.7);
                }
                if (skip != null)
                {
                    await host.LeftClick(skip.Value, rnd.Next(80, 100));
                }
                return SpinResult.Vouchers;
            }
            else
            {
                result = SpinResult.Success;
            }
            int error = 0;
            do
            {
                await Task.Delay(1000);
                //scan for share
                bmp = await pageClient.Screenshot();
                point = bmp.FindImage("Images\\closeshare.png", 0.7);
                if (point != null)
                {
                    break;
                }
                error++;
            } while (error < 20);
            if (point != null)
            {
                await host.LeftClick(new Point(point.Value.X + 10, point.Value.Y), rnd.Next(80, 100));
                await Task.Delay(500);
            }
            return result;
        }

        public async Task<bool> EndSpin(Bitmap bmp)
        {
            Point? point = bmp.FindImage("Images\\close18.png", 0.7);
            if (point == null)
            {
                point = bmp.FindImage("Images\\close19.png", 0.7);
            }
            if (point != null)
            {
                await host.LeftClick(point.Value, rnd.Next(80, 100));
                return true;
            }
            return false;
        }
    }

    public enum SpinResult
    {
        Success,
        Failed,
        Vouchers,
        NotEnoughVouchers
    }
}
