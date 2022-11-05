using CefSharp;
using CefSharp.WinForms;
using GO2FlashLauncher.Model;
using GO2FlashLauncher.Script.GameLogic;
using GO2FlashLauncher.Service;
using System;
using System.Threading.Tasks;

namespace GO2FlashLauncher.Script
{
    internal class WheelScript : AbstractScript
    {
        public WheelScript(BotSettings settings) : base(settings)
        {
        }

        public override Task Run(ChromiumWebBrowser browser, int userID, GO2HttpService httpService)
        {
            return Task.Run(async () =>
            {
                if (IsRunning)
                {
                    //Avoid double run
                    return;
                }
                IsRunning = true;
                BotStartTime = DateTime.Now;
                var devTools = browser.GetBrowser().GetDevToolsClient();
                var host = browser.GetBrowser().GetHost(); 
                try
                {
                    var m = new MainScreen(browser);
                    var w = new Wheel(browser);
                    var error = 0;
                    bool mainScreenLocated = false;
                    bool inWheel = false;
                    do
                    {
                        try
                        {
                            Cancellation.ThrowIfCancellationRequested();
                            //error too much
                            if (error > 5)
                            {
                                var lagging = await devTools.Screenshot();
                                if (m.DetectDisconnect(lagging))
                                {
                                    Logger.LogError("Please refresh the browser! Server disconnected! Maybe is in server maintainance! ");
                                    Logger.LogInfo("Bot Stopped");
                                    return;
                                }
                                else
                                {
                                    //start over again
                                    mainScreenLocated = false;
                                    inWheel = false;
                                }
                            }
                            var bmp = await devTools.Screenshot();
                            if (bmp.FindImageGrayscaled("Images\\friendrequesttext.png", 0.7).HasValue)
                            {
                                var friendClose = bmp.FindImage("Images\\friendrequestclose.png", 0.8);
                                if (friendClose.HasValue)
                                {
                                    await host.LeftClick(friendClose.Value, 100);
                                    await Task.Delay(botSettings.Delays);
                                    bmp = await devTools.Screenshot();
                                }
                            }
                            if (!mainScreenLocated)
                            {
                                //locate planet base view
                                if (await m.Locate(bmp))
                                {
                                    for (int x = 0; x < 3; x++)
                                    {
                                        bmp = await devTools.Screenshot();
                                        if (x <= 1)
                                        {
                                            //don't click home base
                                            await m.Locate(bmp, false);
                                        }
                                        else
                                        {
                                            await m.Locate(bmp);
                                        }
                                        await Task.Delay(100);
                                    }
                                    Logger.LogInfo("Mainscreen located");
                                    resources = await m.DetectResource(httpService, userID);
                                    Logger.LogInfo("Detected Vouchers: " + resources.Vouchers);

                                    mainScreenLocated = true;
                                    error = 0;
                                }
                                else
                                {
                                    if (error == 0)
                                    {
                                        Logger.LogInfo("Locating Mainscreen");
                                    }
                                    error++;
                                    if (error < 20)
                                    {
                                        await Task.Delay(botSettings.Delays);
                                        continue;
                                    }
                                }
                            }
                            else if (!inWheel)
                            {
                                inWheel = await w.GetIn(bmp);
                                await Task.Delay(botSettings.Delays);
                            }
                            else
                            {
                                //entered wheel
                                Cancellation.ThrowIfCancellationRequested();
                                var spinResult = await w.Spin(bmp, resources);
                                if (spinResult != SpinResult.Failed)
                                {
                                    Logger.LogInfo("Detected Vouchers: " + resources.Vouchers);
                                }
                                else if(spinResult == SpinResult.Failed)
                                {
                                    error++;
                                    if(error > 10)
                                    {
                                        inWheel = false;
                                        mainScreenLocated = false;
                                    }
                                }
                                else if(spinResult == SpinResult.NotEnoughVouchers)
                                {
                                    Logger.LogError("No more vouchers, stop spinning...");
                                    return;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            if (ex is OperationCanceledException)
                            {
                                throw;
                            }
                            Logger.LogError(ex.ToString());
                        }
                    }
                    while (true);
                }
                catch (OperationCanceledException)
                {
                    IsRunning = false;
                }
            });
        }
    }
}
