﻿using CefSharp;
using CefSharp.WinForms;
using GO2FlashLauncher.Model;
using GO2FlashLauncher.Script.GameLogic;
using GO2FlashLauncher.Service;
using System;
using System.Drawing;
using System.Threading.Tasks;

namespace GO2FlashLauncher.Script
{
    internal class InstanceScript : AbstractScript
    {
        public InstanceScript(BotSettings settings) : base(settings)
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
                    bool suspendCollect = false;
                    int stageCount = 0;
                    DateTime noResources = DateTime.MinValue;
                    DateTime lastCollectTime = DateTime.MinValue;
                    int error = 0;
                    int lag = 0;
                    Bitmap lastbmp = null;
                    do
                    {
                        try
                        {
                            //Script stoped
                            Cancellation.ThrowIfCancellationRequested();
                            //error too much
                            if (error > 20)
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
                                    spaceStationLocated = false;
                                    inStage = false;
                                }
                            }
                            //Screenshot browser
                            var bmp = await devTools.Screenshot();
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
                                        await Task.Delay(botSettings.Delays);
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
                                    await Task.Delay(botSettings.Delays / 2);
                                    await m.LocateWarehouse(bmp);
                                    await Task.Delay(botSettings.Delays);
                                    bmp = await devTools.Screenshot();
                                    await m.Collect(bmp);
                                    collectedResources = true;
                                    lastCollectTime = DateTime.Now;
                                }
                            }
                            else if (!spaceStationLocated && (noResources - DateTime.Now).TotalSeconds <= 0)
                            {
                                //locate space stations
                                await Task.Delay(botSettings.Delays / 4 * 3);
                                await s.Enter(bmp);
                                Logger.LogInfo("Going to space station");
                                await Task.Delay(botSettings.Delays / 4 * 3);
                                bmp = await devTools.Screenshot();
                                var spaceStationLocation = await s.Locate(bmp);
                                if (spaceStationLocation.HasValue)
                                {
                                    Logger.LogInfo("Space station located");
                                    resources = await m.DetectResource(httpService, userID);
                                    Logger.LogInfo("Detected Metal: " + resources.Metal);
                                    Logger.LogInfo("Detected HE3: " + resources.HE3);
                                    Logger.LogInfo("Detected Gold: " + resources.Gold);
                                    spaceStationLocated = true;
                                    error = 0;
                                    await Task.Delay(botSettings.Delays / 4 * 3);
                                    await host.LeftClick(spaceStationLocation.Value, 100);
                                    await Task.Delay(200);
                                    Logger.LogInfo("Entering Instance");
                                    bool loop = true;
                                    while (loop)
                                    {
                                        Cancellation.ThrowIfCancellationRequested();
                                        try
                                        {
                                            await Task.Delay(botSettings.Delays);
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
                                                        Cancellation.ThrowIfCancellationRequested();
                                                    }
                                                    await Task.Delay(botSettings.Delays);
                                                    bmp = await devTools.Screenshot();
                                                    Logger.LogInfo("Refilling fleets");
                                                    if (!await b.RefillHE3(bmp, resources, botSettings.HaltOn))
                                                    {
                                                        //no HE3
                                                        Logger.LogWarning("Out of HE3! Halt attack now!");
                                                        inStage = false;
                                                        mainScreenLocated = false;
                                                        spaceStationLocated = false;
                                                        try
                                                        {
                                                            var url = await httpService.GetIFrameUrl(userID);
                                                            browser.Load("https://beta-client.supergo2.com/?userId=" + url.Data.UserId + "&sessionKey=" + url.Data.SessionKey);
                                                        }
                                                        catch
                                                        {
                                                            //unable to refresh
                                                            Logger.LogError("Unable to refresh window! something wrong!");
                                                        }
                                                        Logger.LogInfo("Waiting for resources...");
                                                        noResources = DateTime.Now.AddHours(1);
                                                        break;
                                                    }
                                                    await Task.Delay(botSettings.Delays);
                                                    bmp = await devTools.Screenshot();
                                                    while (!await b.IncreaseFleet(bmp))
                                                    {
                                                        bmp = await devTools.Screenshot();
                                                        Cancellation.ThrowIfCancellationRequested();
                                                        error++;
                                                        if (error > 5)
                                                        {
                                                            loop = false;
                                                            //something wrong
                                                            spaceStationLocated = false;
                                                            mainScreenLocated = false;
                                                            inStage = false;
                                                            var url = await httpService.GetIFrameUrl(userID);
                                                            browser.Load("https://beta-client.supergo2.com/?userId=" + url.Data.UserId + "&sessionKey=" + url.Data.SessionKey);
                                                            return;
                                                        }
                                                        await Task.Delay(100);
                                                    }
                                                    await Task.Delay(botSettings.Delays - 100);
                                                    bmp = await devTools.Screenshot();
                                                    while (!await b.SelectFleet(bmp, botSettings.Fleets, SelectFleetType.Instance))
                                                    {
                                                        Logger.LogError("No fleet found!");
                                                        bmp = await devTools.Screenshot();
                                                        error++;
                                                        if (error > 3)
                                                        {
                                                            loop = false;
                                                            //something wrong
                                                            spaceStationLocated = false;
                                                            mainScreenLocated = false;
                                                            inStage = false;
                                                            var url = await httpService.GetIFrameUrl(userID);
                                                            browser.Load("https://beta-client.supergo2.com/?userId=" + url.Data.UserId + "&sessionKey=" + url.Data.SessionKey);
                                                            return;
                                                        }
                                                        await Task.Delay(100);
                                                        Cancellation.ThrowIfCancellationRequested();
                                                    }
                                                    await Task.Delay(botSettings.Delays - 100);
                                                    bmp = await devTools.Screenshot();
                                                    var p = bmp.FindImage("Images\\instanceStart.png", 0.7);
                                                    if (p != null)
                                                    {
                                                        await host.LeftClick(p.Value, 100);
                                                    }
                                                    loop = false;
                                                    error = 0;
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
                                            var url = await httpService.GetIFrameUrl(userID);
                                            browser.Load("https://beta-client.supergo2.com/?userId=" + url.Data.UserId + "&sessionKey=" + url.Data.SessionKey);
                                            error = 0;
                                            await Task.Delay(botSettings.Delays);
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    Logger.LogInfo("Locating Space station");
                                    error++;
                                    if (error > 3)
                                    {
                                        await b.CloseButtons(bmp);
                                        await Task.Delay(300);
                                    }
                                    if (error > 10)
                                    {
                                        //something really wrong lol
                                        Logger.LogError("Something seriously wrong! Refreshing the game!");
                                        spaceStationLocated = false;
                                        mainScreenLocated = false;
                                        inStage = false;
                                        error = 0;
                                        var url = await httpService.GetIFrameUrl(userID);
                                        browser.Load("https://beta-client.supergo2.com/?userId=" + url.Data.UserId + "&sessionKey=" + url.Data.SessionKey);
                                        await Task.Delay(botSettings.Delays);
                                        break;
                                    }
                                    continue;
                                }
                            }
                            else if (inStage)
                            {
                                if (await b.BattleEnds(bmp))
                                {
                                    Logger.LogInfo("Battle Ends");
                                    inStage = false;
                                    stageCount++;
                                    spaceStationLocated = false;
                                    Logger.ClearLog();
                                }
                                else
                                {
                                    var crop = await bmp.Crop(new Point(0, 0), new Size(500, 500));
                                    var mail = crop.FindImage("Images\\mail.png", 0.6);
                                    if (mail.HasValue)
                                    {
                                        Logger.LogInfo("Found Mail");
                                        //collect mail
                                        if (await m.CollectMails(bmp))
                                        {
                                            suspendCollect = false;
                                            bmp = await devTools.Screenshot();
                                        }
                                    }
                                    if (stageCount >= botSettings.InstanceHitCount && botSettings.InstanceHitCount > 0 && !suspendCollect)
                                    {
                                        //open box
                                        if (await i.OpenInventory(bmp))
                                        {
                                            await Task.Delay(botSettings.Delays);
                                            bmp = await devTools.Screenshot();
                                            //open boxes
                                            if (await i.OpenTreasury(bmp, stageCount))
                                            {
                                                stageCount = 0;
                                            }
                                            else
                                            {
                                                suspendCollect = true;
                                            }
                                            await Task.Delay(botSettings.Delays);
                                            bmp = await devTools.Screenshot();
                                            while (!await b.CloseButtons(bmp))
                                            {
                                                error++;
                                                await Task.Delay(100);
                                                bmp = await devTools.Screenshot();
                                                Cancellation.ThrowIfCancellationRequested();
                                                if (error > 10)
                                                {
                                                    //???
                                                    Logger.LogError("Something seriously wrong! Refreshing the game!");
                                                    spaceStationLocated = false;
                                                    mainScreenLocated = false;
                                                    inStage = false;
                                                    error = 0;
                                                    var url = await httpService.GetIFrameUrl(userID);
                                                    browser.Load("https://beta-client.supergo2.com/?userId=" + url.Data.UserId + "&sessionKey=" + url.Data.SessionKey);
                                                    await Task.Delay(botSettings.Delays);
                                                    break;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            suspendCollect = true;
                                            await Task.Delay(botSettings.Delays);
                                            bmp = await devTools.Screenshot();
                                            while (!await b.CloseButtons(bmp))
                                            {
                                                error++;
                                                await Task.Delay(100);
                                                bmp = await devTools.Screenshot();
                                                if (error > 10)
                                                {
                                                    //???
                                                    Logger.LogError("Something seriously wrong! Refreshing the game!");
                                                    spaceStationLocated = false;
                                                    mainScreenLocated = false;
                                                    inStage = false;
                                                    error = 0;
                                                    var url = await httpService.GetIFrameUrl(userID);
                                                    browser.Load("https://beta-client.supergo2.com/?userId=" + url.Data.UserId + "&sessionKey=" + url.Data.SessionKey);
                                                    await Task.Delay(botSettings.Delays);
                                                    break;
                                                }
                                                Cancellation.ThrowIfCancellationRequested();
                                            }
                                        }
                                    }
                                }
                            }
                            //collecting EZRewards
                            if (mainScreenLocated)
                            {
                                if (await m.EZRewards(bmp))
                                {
                                    Logger.LogInfo("Collected EZRewards");
                                    await Task.Delay(800);
                                    //detect close
                                    bmp = await devTools.Screenshot();
                                    bmp = await bmp.Crop(new Point(0, 0), new Size(bmp.Width, bmp.Height - 300));
                                    while (!await b.CloseButtons(bmp))
                                    {
                                        error++;
                                        await Task.Delay(100);
                                        bmp = await devTools.Screenshot();
                                        if (error > 10)
                                        {
                                            //???
                                            Logger.LogError("Something seriously wrong! Refreshing the game!");
                                            spaceStationLocated = false;
                                            mainScreenLocated = false;
                                            inStage = false;
                                            error = 0;
                                            var url = await httpService.GetIFrameUrl(userID);
                                            browser.Load("https://beta-client.supergo2.com/?userId=" + url.Data.UserId + "&sessionKey=" + url.Data.SessionKey);
                                            await Task.Delay(botSettings.Delays);
                                            Cancellation.ThrowIfCancellationRequested();
                                            break;
                                        }
                                    }
                                    var detect = bmp.FindImageGrayscaled("Images\\MPOK.png", 0.8);
                                    if (detect != null)
                                    {
                                        await host.LeftClick(detect.Value, 120);
                                    }
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
                                    var url = await httpService.GetIFrameUrl(userID);
                                    browser.Load("https://beta-client.supergo2.com/?userId=" + url.Data.UserId + "&sessionKey=" + url.Data.SessionKey);
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
                        await Task.Delay(botSettings.Delays);
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