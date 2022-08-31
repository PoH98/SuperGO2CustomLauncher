using CefSharp;
using CefSharp.WinForms;
using GO2FlashLauncher.Model;
using GO2FlashLauncher.Script.GameLogic;
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GO2FlashLauncher.Script
{
    internal class MainScript
    {
        private bool IsRunning;
        private readonly RichTextBox logger;
        private readonly BotSettings botSettings;
        public MainScript(RichTextBox logger, BotSettings settings)
        {
            this.logger = logger;
            this.botSettings = settings;
        }

        public bool Running
        {
            get
            {
                return IsRunning;
            }
        }
        public Task Run(CancellationToken token, ChromiumWebBrowser browser)
        {
            return Task.Run(async () =>
            {
                if (IsRunning)
                {
                    return;
                }
                IsRunning = true;
                var devTools = browser.GetBrowser().GetDevToolsClient();
                var host = browser.GetBrowser().GetHost();
                try
                {
                    MainScreen m = new MainScreen(browser);
                    SpaceStation s = new SpaceStation(browser);
                    Battle b = new Battle(browser);
                    bool mainScreenLocated = false;
                    bool spaceStationLocated = false;
                    bool collectedResources = false;
                    bool inStage = false;
                    int stageCount = 0;
                    DateTime lastCollectTime = DateTime.MinValue;
                    int error = 0;
                    int lag = 0;
                    Bitmap lastbmp = null;
                    ClearLog();
                    do
                    {
                        try
                        {
                            token.ThrowIfCancellationRequested();
                            var bmp = await devTools.Screenshot();
                            //var bmp = await browser.Screenshot();
                            if (!mainScreenLocated)
                            {
                                if (await m.Locate(bmp))
                                {
                                    for(int x = 0; x < 3; x++)
                                    {
                                        bmp = await devTools.Screenshot();
                                        await m.Locate(bmp);
                                        await Task.Delay(100);
                                    }
                                    LogInfo("Mainscreen located");
                                    mainScreenLocated = true;
                                    error = 0;
                                }
                                else
                                {
                                    if(error == 0)
                                    {
                                        LogInfo("Locating Mainscreen");
                                    }
                                    error++;
                                    if(error < 20)
                                    {
                                        await Task.Delay(1000);
                                        continue;
                                    }
                                }
                            }
                            else if(collectedResources && (DateTime.Now - lastCollectTime).TotalHours >= 1)
                            {
                                collectedResources = false;
                                mainScreenLocated = false;
                                spaceStationLocated = false;
                                inStage = false;
                            }
                            else if (!collectedResources)
                            {

                                LogInfo("Collecting Resources");
                                if (await m.Locate(bmp))
                                {
                                    await Task.Delay(1000);
                                    await m.LocateWarehouse(bmp);
                                    await Task.Delay(1500);
                                    bmp = await devTools.Screenshot();
                                    await m.Collect(bmp);
                                    collectedResources = true;
                                    lastCollectTime = DateTime.Now;
                                }
                            }
                            else if(!spaceStationLocated)
                            {
                                await Task.Delay(1000);
                                await s.Enter(bmp);
                                LogInfo("Going to space station");
                                await Task.Delay(1000);
                                bmp = await devTools.Screenshot();
                                var spaceStationLocation = await s.Locate(bmp);
                                if (spaceStationLocation.HasValue)
                                {
                                    LogInfo("Space station located");
                                    if(stageCount >= 50)
                                    {
                                        //mail full since mail system not yet done
                                        LogError("Mail is full! Bot stopped!");
                                        return;
                                    }
                                    spaceStationLocated = true;
                                    error = 0;
                                    await Task.Delay(1000);
                                    await host.LeftClick(spaceStationLocation.Value, 100);
                                    await Task.Delay(200);
                                    LogInfo("Entering Instance");
                                    bool loop = true;
                                    while (loop)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        try
                                        {
                                            await Task.Delay(500);
                                            bmp = await devTools.Screenshot();
                                            switch (await s.EnterInstance(bmp, botSettings.Instance))
                                            {
                                                case InstanceEnterState.Error:
                                                    error++;
                                                    LogError("Error on entering instance!");
                                                    break;
                                                case InstanceEnterState.IncreaseFleet:
                                                    LogInfo("Selecting fleets");
                                                    while (!await b.IncreaseFleet(bmp))
                                                    {
                                                        bmp = await devTools.Screenshot();
                                                        await Task.Delay(100);
                                                        token.ThrowIfCancellationRequested();
                                                    }
                                                    await Task.Delay(1000);
                                                    bmp = await devTools.Screenshot();
                                                    LogInfo("Refilling fleets");
                                                    if(!await b.RefillHE3(bmp))
                                                    {
                                                        //no HE3
                                                        LogError("Out of HE3!");
                                                        browser.Reload();
                                                        inStage = false;
                                                        mainScreenLocated = false;
                                                        spaceStationLocated = false;
                                                        LogInfo("Waiting for resources...");
                                                        await Task.Delay(new TimeSpan(1,0,0));
                                                        break;
                                                    }
                                                    await Task.Delay(1000);
                                                    bmp = await devTools.Screenshot();
                                                    while (!await b.IncreaseFleet(bmp))
                                                    {
                                                        bmp = await devTools.Screenshot();
                                                        await Task.Delay(100);
                                                        token.ThrowIfCancellationRequested();
                                                    }
                                                    await Task.Delay(1000);
                                                    if(!await b.SelectFleet(bmp, botSettings.Fleets, SelectFleetType.Instance))
                                                    {
                                                        LogError("No fleet found!");
                                                        break;
                                                    }
                                                    await Task.Delay(1000);
                                                    bmp = await devTools.Screenshot();
                                                    var p = bmp.FindImage("Images\\instanceStart.png", 0.7);
                                                    if(p != null)
                                                    {
                                                        await host.LeftClick(p.Value, 100);
                                                    }
                                                    loop = false;
                                                    error = 0;
                                                    stageCount++;
                                                    LogInfo("Waiting for stage end...");
                                                    inStage = true;
                                                    break;
                                                case InstanceEnterState.InStage:
                                                    error = 0;
                                                    loop = false;
                                                    LogInfo("Already in stage");
                                                    inStage = true;
                                                    break;
                                            }
                                        }
                                        catch
                                        {

                                        }
                                        if(error > 20)
                                        {
                                            LogError("Something seriously wrong! Refreshing the game!");
                                            spaceStationLocated = false;
                                            mainScreenLocated = false;
                                            browser.Reload();
                                            error = 0;
                                            await Task.Delay(1000);
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    LogInfo("Locating Space station");
                                    error++;
                                    continue;
                                }
                            }
                            else if (inStage)
                            {
                                if (await b.BattleEnds(bmp))
                                {
                                    LogInfo("Battle Ends");
                                    inStage = false;
                                    spaceStationLocated = false;
                                    //check mailbox, bugged now and dumb
                                    var mail = bmp.FindImage("Images\\mail.png", 0.6);
                                    if (mail.HasValue)
                                    {
                                        LogInfo("Found Mail");
                                        //collect mail
                                        if(!await m.CollectMails(bmp))
                                        {
                                            //something wrong
                                            browser.Reload();
                                        }
                                    }
                                    ClearLog();
                                }
                                else
                                {
                                    await Task.Delay(9000);
                                }
                            }
                            if (mainScreenLocated)
                            {
                                await Task.Delay(1000);
                                if(await m.EZRewards(bmp))
                                {
                                    LogInfo("Collected EZRewards");
                                    //detect close
                                    bmp = await devTools.Screenshot();
                                    await b.CloseButtons(bmp);
                                    var detect = bmp.FindImageGrayscaled("Images\\MPOK.png", 0.8);
                                    if(detect != null)
                                    {
                                        await host.LeftClick(detect.Value, 120);
                                    }
                                    await Task.Delay(1000);
                                }
                            }
                            if(error > 20)
                            {
                                if (m.DetectDisconnect(bmp))
                                {
                                    LogError("Please refresh the browser! Server disconnected! Maybe is in server maintainance! ");
                                    LogInfo("Bot Stopped");
                                    return;
                                }
                                else
                                {
                                    //start over again
                                    mainScreenLocated = false;
                                    spaceStationLocated = false;
                                    inStage = false;
                                }
                            }
                            if(lastbmp != null)
                            {
                                if(lastbmp == bmp)
                                {
                                    lag++;
                                }
                                else
                                {
                                    lag = 0;
                                }
                                if(lag > 20)
                                {
                                    LogError("Lag detected!");
                                    browser.Reload();
                                    inStage = false;
                                    mainScreenLocated = false;
                                    spaceStationLocated = false;
                                }
                            }
                            lastbmp = bmp;
                        }
                        catch (Exception ex)
                        {
                            if (ex is OperationCanceledException)
                            {
                                throw;
                            }
                            LogError(ex.Message + "\n" + ex?.InnerException?.Message);
                        }
                        await Task.Delay(1000);
                    }
                    while (true);
                }
                catch (OperationCanceledException)
                {
                    IsRunning = false;
                }
            });
        }
        private void LogInfo(string info)
        {
            if (logger == null)
            {
                return;
            }
            logger.Invoke((MethodInvoker)delegate
            {
                logger.SelectionStart = logger.TextLength;
                logger.SelectionLength = 0;
                logger.SelectionColor = Color.Green;
                logger.AppendText("\n" + "[" + DateTime.Now.ToString("HH:mm") +  "]: " + info);
                logger.Focus();
                logger.Select(logger.TextLength, 0);
                logger.ScrollToCaret();
            });
        }
        private void LogError(string info)
        {
            if(logger == null)
            {
                return;
            }
            logger.Invoke((MethodInvoker)delegate
            {
                logger.SelectionStart = logger.TextLength;
                logger.SelectionLength = 0;
                logger.SelectionColor = Color.Red;
                logger.AppendText("\n" + "[" + DateTime.Now.ToString("HH:mm") + "]: " + info);
                logger.Focus();
                logger.Select(logger.TextLength, 0);
                logger.ScrollToCaret();
            });
        }

        private void ClearLog()
        {
            logger.Invoke((MethodInvoker)delegate
            {
                logger.Text = "";
            });
        }
    }
}
