using CefSharp;
using CefSharp.WinForms;
using GO2FlashLauncher.Model;
using GO2FlashLauncher.Script.GameLogic;
using GO2FlashLauncher.Service;
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
        private readonly BotSettings botSettings;
        private BaseResources resources = new BaseResources();
        private DateTime BotStartTime;
        public BaseResources Resources
        {
            get
            {
                return resources;
            }
        }
        public TimeSpan BotRuntime
        {
            get
            {
                return DateTime.Now - BotStartTime;
            }
        }
        public MainScript(BotSettings settings)
        {
            this.botSettings = settings;
        }
        /// <summary>
        /// Check if script is running
        /// </summary>
        public bool Running
        {
            get
            {
                return IsRunning;
            }
        }
        /// <summary>
        /// Main loop of script
        /// </summary>
        /// <param name="token"></param>
        /// <param name="browser"></param>
        /// <returns></returns>
        public Task Run(CancellationToken token, ChromiumWebBrowser browser)
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
                    //Init sub scripts
                    MainScreen m = new MainScreen(browser);
                    SpaceStation s = new SpaceStation(browser);
                    Battle b = new Battle(browser);
                    Inventory i = new Inventory(browser);
                    LagDetection l = new LagDetection();

                    //Init status
                    bool mainScreenLocated = false;
                    bool spaceStationLocated = false;
                    bool collectedResources = false;
                    bool inStage = false;
                    int stageCount = 0;
                    DateTime noResources = DateTime.MinValue;
                    DateTime lastCollectTime = DateTime.MinValue;
                    int error = 0;
                    int lag = 0;
                    Bitmap lastbmp = null;
                    //Clear logs in rtb
                    Logger.ClearLog();
                    do
                    {
                        try
                        {
                            //Script stoped
                            token.ThrowIfCancellationRequested();
                            //Screenshot browser
                            var bmp = await devTools.Screenshot();
                            //WinAPI capture, tested not working
                            //var bmp = await browser.Screenshot();
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
                                    resources = await m.DetectResource(bmp);
                                    Logger.LogInfo("Detected Metal: " + resources.Metal);
                                    Logger.LogInfo("Detected HE3: " + resources.HE3);
                                    Logger.LogInfo("Detected Gold: " + resources.Gold);
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
                                        await Task.Delay(1000);
                                        continue;
                                    }
                                }
                            }
                            else if (collectedResources && (DateTime.Now - lastCollectTime).TotalHours >= 1)
                            {
                                //reset collected resources and force collect again
                                collectedResources = false;
                                mainScreenLocated = false;
                                spaceStationLocated = false;
                                inStage = false;
                            }
                            else if (!collectedResources)
                            {
                                //collect resources
                                Logger.LogInfo("Collecting Resources");
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
                            else if (!spaceStationLocated && (noResources - DateTime.Now).TotalSeconds <= 0)
                            {
                                //locate space stations
                                await Task.Delay(1000);
                                await s.Enter(bmp);
                                Logger.LogInfo("Going to space station");
                                await Task.Delay(1000);
                                bmp = await devTools.Screenshot();
                                var spaceStationLocation = await s.Locate(bmp);
                                if (spaceStationLocation.HasValue)
                                {
                                    Logger.LogInfo("Space station located");
                                    spaceStationLocated = true;
                                    error = 0;
                                    await Task.Delay(1000);
                                    resources = await m.DetectResource(bmp);
                                    Logger.LogInfo("Detected Metal: " + resources.Metal);
                                    Logger.LogInfo("Detected HE3: " + resources.HE3);
                                    Logger.LogInfo("Detected Gold: " + resources.Gold);
                                    await host.LeftClick(spaceStationLocation.Value, 100);
                                    await Task.Delay(200);
                                    Logger.LogInfo("Entering Instance");
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
                                                    spaceStationLocated = false;
                                                    loop = false;
                                                    Logger.LogError("Error on entering instance!");
                                                    break;
                                                case InstanceEnterState.IncreaseFleet:
                                                    Logger.LogInfo("Selecting fleets");
                                                    while (!await b.IncreaseFleet(bmp))
                                                    {
                                                        bmp = await devTools.Screenshot();
                                                        await Task.Delay(100);
                                                        token.ThrowIfCancellationRequested();
                                                    }
                                                    await Task.Delay(1000);
                                                    bmp = await devTools.Screenshot();
                                                    Logger.LogInfo("Refilling fleets");
                                                    if (!await b.RefillHE3(bmp, resources, botSettings.HaltOn))
                                                    {
                                                        //no HE3
                                                        Logger.LogWarning("Out of HE3! Halt attack now!");
                                                        browser.Reload();
                                                        inStage = false;
                                                        mainScreenLocated = false;
                                                        spaceStationLocated = false;
                                                        Logger.LogInfo("Waiting for resources...");
                                                        noResources = DateTime.Now.AddHours(1);
                                                        break;
                                                    }
                                                    await Task.Delay(1000);
                                                    bmp = await devTools.Screenshot();
                                                    while (!await b.IncreaseFleet(bmp))
                                                    {
                                                        bmp = await devTools.Screenshot();
                                                        await Task.Delay(100);
                                                        token.ThrowIfCancellationRequested();
                                                        error++;
                                                        if (error > 10)
                                                        {
                                                            loop = false;
                                                            //something wrong
                                                            spaceStationLocated = false;
                                                            mainScreenLocated = false;
                                                            inStage = false;
                                                            browser.Reload();
                                                            return;
                                                        }
                                                    }
                                                    await Task.Delay(1000);
                                                    bmp = await devTools.Screenshot();
                                                    await Task.Delay(1000);
                                                    while (!await b.SelectFleet(bmp, botSettings.Fleets, SelectFleetType.Instance))
                                                    {
                                                        Logger.LogError("No fleet found!");
                                                        bmp = await devTools.Screenshot();
                                                        await Task.Delay(100);
                                                        error++;
                                                        if (error > 10)
                                                        {
                                                            loop = false;
                                                            //something wrong
                                                            spaceStationLocated = false;
                                                            mainScreenLocated = false;
                                                            inStage = false;
                                                            browser.Reload();
                                                            return;
                                                        }
                                                        break;
                                                    }
                                                    await Task.Delay(1000);
                                                    bmp = await devTools.Screenshot();
                                                    var p = bmp.FindImage("Images\\instanceStart.png", 0.7);
                                                    if (p != null)
                                                    {
                                                        await host.LeftClick(p.Value, 100);
                                                    }
                                                    loop = false;
                                                    error = 0;
                                                    stageCount++;
                                                    Logger.LogInfo("Waiting for stage end...");
                                                    inStage = true;
                                                    break;
                                                case InstanceEnterState.InStage:
                                                    error = 0;
                                                    loop = false;
                                                    Logger.LogInfo("Already in stage");
                                                    inStage = true;
                                                    break;
                                            }
                                        }
                                        catch
                                        {

                                        }
                                        if (error > 20)
                                        {
                                            Logger.LogError("Something seriously wrong! Refreshing the game!");
                                            spaceStationLocated = false;
                                            mainScreenLocated = false;
                                            inStage = false;
                                            browser.Reload();
                                            error = 0;
                                            await Task.Delay(1000);
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    Logger.LogInfo("Locating Space station");
                                    error++;
                                    continue;
                                }
                            }
                            else if (inStage)
                            {
                                var mail = bmp.FindImage("Images\\mail.png", 0.6);
                                if (mail.HasValue)
                                {
                                    Logger.LogInfo("Found Mail");
                                    //collect mail
                                    if (await m.CollectMails(bmp))
                                    {
                                        bmp = await devTools.Screenshot();
                                    }
                                }
                                if (stageCount >= botSettings.InstanceHitCount && botSettings.InstanceHitCount > 0)
                                {
                                    //open box
                                    if(await i.OpenInventory(bmp))
                                    {
                                        await Task.Delay(2000);
                                        bmp = await devTools.Screenshot();
                                        //open boxes
                                        if (await i.OpenTreasury(bmp, stageCount))
                                        {
                                            stageCount = 0;
                                        }
                                        await Task.Delay(1000);
                                        bmp = await devTools.Screenshot();
                                        await b.CloseButtons(bmp);
                                    }
                                    else
                                    {
                                        await Task.Delay(1000);
                                        bmp = await devTools.Screenshot();
                                        await b.CloseButtons(bmp);
                                    }
                                }
                                if (await b.BattleEnds(bmp))
                                {
                                    Logger.LogInfo("Battle Ends");
                                    inStage = false;
                                    spaceStationLocated = false;
                                    Logger.ClearLog();
                                }
                                else
                                {
                                    await Task.Delay(2000);
                                }
                            }
                            //collecting EZRewards
                            if (mainScreenLocated)
                            {
                                await Task.Delay(1000);
                                if (await m.EZRewards(bmp))
                                {
                                    Logger.LogInfo("Collected EZRewards");
                                    await Task.Delay(1000);
                                    //detect close
                                    bmp = await devTools.Screenshot();
                                    bmp = await bmp.Crop(new Point(0,0), new Size(bmp.Width, bmp.Height - 300));
                                    await b.CloseButtons(bmp);
                                    var detect = bmp.FindImageGrayscaled("Images\\MPOK.png", 0.8);
                                    if (detect != null)
                                    {
                                        await host.LeftClick(detect.Value, 120);
                                    }
                                    await Task.Delay(1000);
                                }
                            }
                            //error too much
                            if (error > 20)
                            {
                                if (m.DetectDisconnect(bmp))
                                {
                                    Logger.LogError("Please refresh the browser! Server disconnected! Maybe is in server maintainance! ");
                                    Logger.LogInfo("Bot Stopped");
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
                            //lag detection
                            if (lastbmp != null)
                            {
                                if ((noResources - DateTime.Now).TotalSeconds >= 0)
                                {
                                    Logger.LogDebug("Halt attack, disabled lag detection");
                                }
                                else
                                {
                                    if (bmp.Size == lastbmp.Size)
                                    {
                                        if (l.IsLagging(bmp, lastbmp, inStage))
                                        {
                                            lag++;
                                        }
                                        else
                                        {
                                            lag = 0;
                                        }
                                    }
                                }


                                if (lag > 120)
                                {
                                    Logger.LogError("Lag detected! Lag confirmed! Restarting...");
                                    browser.Reload();
                                    lag = 0;
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
                            Logger.LogError(ex.ToString());
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
    }
}
