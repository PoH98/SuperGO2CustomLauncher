using CefSharp;
using CefSharp.WinForms;
using Discord;
using Discord.WebSocket;
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
        private readonly DiscordSocketClient client;
        private readonly IUser user;
        public InstanceScript(BotSettings settings, PlanetSettings planetSettings, DiscordSocketClient client) : base(settings, planetSettings)
        {
            this.client = client;
            if (client != null && botSettings.DiscordUserID != 0)
            {
                user = client.GetUserAsync(botSettings.DiscordUserID).Result;
            }
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
                while (!browser.IsBrowserInitialized)
                {
                    await Task.Delay(1000);
                }
                CefSharp.DevTools.DevToolsClient devTools = browser.GetBrowser().GetDevToolsClient();
                IBrowserHost host = browser.GetBrowser().GetHost();
                host.SetFocus(false);
                try
                {
                    //Init sub scripts
                    MainScreen m = new MainScreen(browser);
                    SpaceStation s = new SpaceStation(browser);
                    Battle b = new Battle(browser);
                    Inventory i = new Inventory(browser);
                    LagDetection l = new LagDetection();
                    Wheel w = new Wheel(browser);
                    //Init status
                    bool mainScreenLocated = false;
                    bool spaceStationLocated = false;
                    bool collectedResources = false;
                    bool inStage = false;
                    bool suspendCollect = false;
                    bool inSpin = false;
                    bool spinable = true;
                    int currentRestrictCount = 0;
                    bool runningRestrict = false, runningTrial = false, runningConstellation = false;
                    int stageCount = 0;
                    int currentTrialLv = -1;
                    long currentInstanceCount = 0;
                    long currentConstellationCount = 0;
                    bool trialStucked = false;
                    SpinResult spinResult = SpinResult.Failed;
                    DateTime noResources = DateTime.MinValue;
                    DateTime lastCollectTime = DateTime.MinValue;
                    DateTime lastRestrictDate = DateTime.Now;
                    DateTime lastTrialDate = DateTime.Now;
                    DateTime lastRefresh = DateTime.Now;
                    DateTime lastConstDate = DateTime.Now;
                    DateTime now = DateTime.Now;
                    int error = 0;
                    int lag = 0;

                    do
                    {
                        try
                        {
                            //Script stoped
                            Cancellation.ThrowIfCancellationRequested();
                            if (DateTime.Now.ToUniversalTime().Date != now.ToUniversalTime().Date)
                            {
                                Logger.LogInfo("Refreshing game to avoid slow game");
                                Model.SGO2.GetFrameResponse url = await httpService.GetIFrameUrl(userID);
                                browser.Load("https://client.guerradenaves.lat/?userId=" + url.Data.UserId + "&sessionKey=" + url.Data.SessionKey);
                                mainScreenLocated = false;
                                spaceStationLocated = false;
                                inStage = false;
                                await Task.Delay(3000);
                                now = DateTime.Now;
                                continue;
                            }
                            if (IsReloading)
                            {
                                await Task.Delay(200);
                                continue;
                            }
                            if ((DateTime.Now - lastRefresh).TotalHours > 12)
                            {
                                Logger.LogInfo("Refreshing game to avoid slow game");
                                Model.SGO2.GetFrameResponse url = await httpService.GetIFrameUrl(userID);
                                browser.Load("https://client.guerradenaves.lat/?userId=" + url.Data.UserId + "&sessionKey=" + url.Data.SessionKey);
                                mainScreenLocated = false;
                                spaceStationLocated = false;
                                inStage = false;
                                await Task.Delay(3000);
                                lastRefresh = DateTime.Now;
                                continue;
                            }
                            //error too much
                            if (error > 5)
                            {
                                Bitmap lagging = await devTools.Screenshot();
                                if (error > 20)
                                {
                                    //start over again
                                    mainScreenLocated = false;
                                    spaceStationLocated = false;
                                    inStage = false;
                                }
                            }
                            //Screenshot browser
                            Bitmap bmp = await devTools.Screenshot();
                            await Task.Delay(100);
                            if (m.DetectDisconnect(bmp))
                            {
                                if (user != null)
                                {
                                    _ = await user.SendMessageAsync("Game disconnected, reconnecting...");
                                }
                                Logger.LogWarning("Disconnected, reconnect after 5 sec...");
                                await Task.Delay(new TimeSpan(0, 0, 5));
                                Model.SGO2.GetFrameResponse url = await httpService.GetIFrameUrl(userID);
                                browser.Load("https://client.guerradenaves.lat/?userId=" + url.Data.UserId + "&sessionKey=" + url.Data.SessionKey);
                                mainScreenLocated = false;
                                spaceStationLocated = false;
                                inStage = false;
                                await Task.Delay(3000);
                                lastRefresh = DateTime.Now;
                                continue;
                            }
                            Bitmap crop = await bmp.Crop(new Point(0, 0), new Size((int)Math.Round((double)bmp.Width / 2), (int)Math.Round((double)bmp.Height / 2)));
                            Point? underAttack = bmp.FindImage("Images\\underattack.png", 0.8);
                            if (underAttack == null)
                            {
                                underAttack = bmp.FindImage("Images\\underattack2.png", 0.8);
                            }
                            if (underAttack.HasValue)
                            {
                                if (user != null)
                                {
                                    _ = await user.SendMessageAsync("You are under attack!");
                                }

                                _ = await i.OpenInventory(bmp);
                                await Task.Delay(500);
                                bmp = await devTools.Screenshot();
                                if (!await i.OpenTruce(bmp))
                                {
                                    if (user != null)
                                    {
                                        _ = await user.SendMessageAsync("Truce failed to open! Danger!");
                                    }

                                    Logger.LogError("Unable to open truce.");
                                }
                                bmp = await devTools.Screenshot();
                                _ = await b.CloseButtons(bmp);
                                await Task.Delay(50);
                                //click away the warning
                                await host.LeftClick(underAttack.Value, 100);
                                await Task.Delay(100);
                                bmp = await devTools.Screenshot();
                                _ = await b.CloseButtons(bmp);
                            }
                            //check for friendrequest
                            if (bmp.FindImageGrayscaled("Images\\friendrequesttext.png", 0.7).HasValue)
                            {
                                Point? friendClose = bmp.FindImage("Images\\friendrequestclose.png", 0.8);
                                if (friendClose.HasValue)
                                {
                                    await host.LeftClick(friendClose.Value, 100);
                                    await Task.Delay(botSettings.Delays - 100);
                                    bmp = await devTools.Screenshot();
                                }
                            }
                            if (!mainScreenLocated)
                            {
                                //locate planet base view
                                if (await m.Locate(bmp))
                                {
                                    for (int x = 0; x < 2; x++)
                                    {
                                        bmp = await devTools.Screenshot();
                                        if (x <= 1)
                                        {
                                            //don't click home base
                                            _ = await m.Locate(bmp, false);
                                        }
                                        else
                                        {
                                            _ = await m.Locate(bmp);
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
                                    Cancellation.ThrowIfCancellationRequested();
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
                                    _ = await m.LocateWarehouse(bmp);
                                    await Task.Delay(botSettings.Delays);
                                    bmp = await devTools.Screenshot();
                                    _ = await m.Collect(bmp);
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
                                Point? spaceStationLocation = await s.Locate(bmp);
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
                                    bool loop = true;
                                    InstanceEnterState state = InstanceEnterState.Error;
                                    SelectFleetType instanceType = SelectFleetType.Instance;
                                    while (loop)
                                    {
                                        Instance:
                                        Cancellation.ThrowIfCancellationRequested();
                                        try
                                        {
                                            await Task.Delay(botSettings.Delays);
                                            bmp = await devTools.Screenshot();
                                            Cancellation.ThrowIfCancellationRequested();
                                            if (planetSettings.TrialFight)
                                            {
                                                //new day, reset
                                                if (DateTime.Now.ToUniversalTime().Day != lastRestrictDate.ToUniversalTime().Day)
                                                {
                                                    currentTrialLv = 1;
                                                    lastTrialDate = DateTime.Now;
                                                    trialStucked = false;
                                                    runningTrial = false;
                                                }
                                                if (currentTrialLv <= planetSettings.TrialMaxLv && currentTrialLv < 10 && !trialStucked)
                                                {
                                                    Logger.LogInfo("Entering Trial");
                                                    (InstanceEnterState, int) r = await s.EnterTrial(bmp);
                                                    state = r.Item1;
                                                    if (currentTrialLv == r.Item2 && state == InstanceEnterState.IncreaseFleet)
                                                    {
                                                        //seems like last time failed
                                                        trialStucked = true;
                                                        Logger.LogWarning("Seems like you can't win this Trial " + r.Item2 + ", skipping...");
                                                        bmp = await devTools.Screenshot();
                                                        _ = await b.CloseButtons(bmp);
                                                        await Task.Delay(botSettings.Delays);
                                                        inStage = false;
                                                        spaceStationLocated = false;
                                                        break;
                                                    }
                                                    if (r.Item2 > planetSettings.TrialMaxLv && state == InstanceEnterState.IncreaseFleet)
                                                    {
                                                        currentTrialLv = r.Item2;
                                                        Logger.LogWarning("Trial " + r.Item2 + " is not attackable, skipping...");
                                                        bmp = await devTools.Screenshot();
                                                        _ = await b.CloseButtons(bmp);
                                                        await Task.Delay(botSettings.Delays);
                                                        inStage = false;
                                                        spaceStationLocated = false;
                                                        break;
                                                    }
                                                    if (state == InstanceEnterState.IncreaseFleet)
                                                    {
                                                        currentTrialLv = r.Item2;
                                                        instanceType = SelectFleetType.Trial;
                                                        runningTrial = true;
                                                        Logger.LogInfo("Current Trial Level: " + currentTrialLv);
                                                    }
                                                    if (state == InstanceEnterState.InstanceCompleted)
                                                    {
                                                        Logger.LogInfo("Trial completed!");
                                                        _ = await b.CloseButtons(bmp);
                                                        await Task.Delay(botSettings.Delays);
                                                        inStage = false;
                                                        spaceStationLocated = false;
                                                        trialStucked = true;
                                                    }
                                                }
                                                else
                                                {
                                                    runningTrial = false;
                                                }
                                            }
                                            if (planetSettings.RestrictFight && !runningTrial)
                                            {
                                                //new day, reset
                                                if (DateTime.Now.ToUniversalTime().Day != lastRestrictDate.ToUniversalTime().Day)
                                                {
                                                    currentRestrictCount = 0;
                                                    lastRestrictDate = DateTime.Now;
                                                    runningRestrict = false;
                                                }
                                                //fight restrict first
                                                if (currentRestrictCount < 3)
                                                {
                                                    //enter restrict instead
                                                    Logger.LogInfo("Entering Restrict");
                                                    try
                                                    {
                                                        state = await s.EnterRestrict(bmp, planetSettings.RestrictLevel);
                                                        instanceType = SelectFleetType.Restrict;
                                                        //have chances
                                                        currentRestrictCount++;
                                                        runningRestrict = true;
                                                    }
                                                    catch (ArgumentException ex)
                                                    {
                                                        if (ex.Message == "Already out of chance")
                                                        {
                                                            currentRestrictCount = 3;
                                                            Logger.LogInfo("Restrict already out of chances today, skipping...");
                                                            bmp = await devTools.Screenshot();
                                                            _ = await b.CloseButtons(bmp);
                                                            await Task.Delay(botSettings.Delays);
                                                            runningRestrict = false;
                                                            inStage = false;
                                                            spaceStationLocated = false;
                                                            break;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    runningRestrict = false;
                                                }
                                            }
                                            if (planetSettings.ConstellationFight && !runningRestrict && !runningTrial)
                                            {
                                                if (DateTime.Now.ToUniversalTime().Day != lastConstDate.ToUniversalTime().Day)
                                                {
                                                    currentConstellationCount = 0;
                                                    lastConstDate = DateTime.Now;
                                                    runningConstellation = false;
                                                }
                                                if (currentConstellationCount < planetSettings.ConstellationCount)
                                                {
                                                    Logger.LogInfo("Entering Constellations");
                                                    try
                                                    {
                                                        if (currentConstellationCount < planetSettings.ConstellationCount)
                                                        {
                                                            state = await s.EnterConstellations(bmp, (Constellations)planetSettings.ConstellationStage, planetSettings.ConstellationLevel);
                                                            instanceType = SelectFleetType.Constellation;
                                                            currentConstellationCount++;
                                                            runningConstellation = true;
                                                        }
                                                        else
                                                        {
                                                            Logger.LogWarning("Constellation had reached limit today!");
                                                        }
                                                    }
                                                    catch (ArgumentException ex)
                                                    {
                                                        if (ex.Message == "")
                                                        {
                                                            Logger.LogInfo("Out of items to enter Constellations, skipping...");
                                                            bmp = await devTools.Screenshot();
                                                            _ = await b.CloseButtons(bmp);
                                                            await Task.Delay(botSettings.Delays);
                                                            runningConstellation = false;
                                                            inStage = false;
                                                            spaceStationLocated = false;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    runningConstellation = false;
                                                }
                                            }
                                            if (!runningRestrict && !runningTrial && !runningConstellation)
                                            {
                                                Logger.LogInfo("Entering Instance");
                                                state = await s.EnterInstance(bmp, planetSettings.Instance);
                                                currentInstanceCount++;
                                                Logger.LogInfo("Current is " + currentInstanceCount + " run!");
                                            }

                                            switch (state)
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
                                                    if (!await b.RefillHE3(bmp, resources, planetSettings.HaltOn))
                                                    {
                                                        //no HE3
                                                        if (user != null)
                                                        {
                                                            _ = await user.SendMessageAsync("Out of HE3, going to halt attack!");
                                                        }

                                                        Logger.LogWarning("Out of HE3! Halt attack now!");
                                                        inStage = false;
                                                        mainScreenLocated = false;
                                                        spaceStationLocated = false;
                                                        try
                                                        {
                                                            Model.SGO2.GetFrameResponse url = await httpService.GetIFrameUrl(userID);
                                                            browser.Load("https://client.guerradenaves.lat/?userId=" + url.Data.UserId + "&sessionKey=" + url.Data.SessionKey);
                                                            lastRefresh = DateTime.Now;
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
                                                            Model.SGO2.GetFrameResponse url = await httpService.GetIFrameUrl(userID);
                                                            browser.Load("https://client.guerradenaves.lat/?userId=" + url.Data.UserId + "&sessionKey=" + url.Data.SessionKey);
                                                            lastRefresh = DateTime.Now;
                                                            break;
                                                        }
                                                        await Task.Delay(100);
                                                    }
                                                    if (error > 5)
                                                    {
                                                        break;
                                                    }
                                                    await Task.Delay(botSettings.Delays - 100);
                                                    bmp = await devTools.Screenshot();
                                                    int instanceLv = 1;
                                                    switch (instanceType)
                                                    {
                                                        case SelectFleetType.Instance:
                                                            instanceLv = planetSettings.Instance;
                                                            break;
                                                        case SelectFleetType.Restrict:
                                                            instanceLv = planetSettings.RestrictLevel;
                                                            break;
                                                        case SelectFleetType.Constellation:
                                                            instanceLv = planetSettings.ConstellationLevel;
                                                            break;
                                                    }
                                                    while (!await b.SelectFleet(bmp, planetSettings.Fleets, instanceType, instanceLv, (Constellations)planetSettings.ConstellationStage))
                                                    {
                                                        if (runningTrial && currentTrialLv == 10)
                                                        {
                                                            //completed trial
                                                            Logger.LogInfo("Trial seems completed");
                                                            goto Instance;

                                                        }
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
                                                            Model.SGO2.GetFrameResponse url = await httpService.GetIFrameUrl(userID);
                                                            browser.Load("https://client.guerradenaves.lat/?userId=" + url.Data.UserId + "&sessionKey=" + url.Data.SessionKey);
                                                            lastRefresh = DateTime.Now;
                                                            break;
                                                        }
                                                        await Task.Delay(100);
                                                        Cancellation.ThrowIfCancellationRequested();
                                                    }
                                                    if (error > 5)
                                                    {
                                                        break;
                                                    }
                                                    await Task.Delay(botSettings.Delays - 100);
                                                    bmp = await devTools.Screenshot();
                                                    Point? p = bmp.FindImage("Images\\instanceStart.png", 0.7);
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
                                            Model.SGO2.GetFrameResponse url = await httpService.GetIFrameUrl(userID);
                                            browser.Load("https://client.guerradenaves.lat/?userId=" + url.Data.UserId + "&sessionKey=" + url.Data.SessionKey);
                                            error = 0;
                                            await Task.Delay(botSettings.Delays);
                                            lastRefresh = DateTime.Now;
                                            continue;
                                        }
                                    }
                                }
                                else
                                {
                                    Logger.LogInfo("Locating Space station");
                                    error++;
                                    if (error > 3)
                                    {
                                        _ = await b.CloseButtons(bmp);
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
                                        Model.SGO2.GetFrameResponse url = await httpService.GetIFrameUrl(userID);
                                        browser.Load("https://client.guerradenaves.lat/?userId=" + url.Data.UserId + "&sessionKey=" + url.Data.SessionKey);
                                        await Task.Delay(botSettings.Delays);
                                        lastRefresh = DateTime.Now;
                                    }
                                    continue;
                                }
                            }
                            else if (inStage)
                            {
                                if (await b.BattleEnds(bmp))
                                {
                                    Logger.LogInfo("Battle Ends");
                                    if (inSpin)
                                    {
                                        for (int x = 0; x < 3; x++)
                                        {
                                            bmp = await devTools.Screenshot();
                                            _ = await b.CloseButtons(bmp);
                                            await Task.Delay(botSettings.Delays);
                                        }
                                        bmp = await devTools.Screenshot();
                                        _ = await w.EndSpin(bmp);
                                    }
                                    inStage = false;
                                    stageCount++;
                                    spaceStationLocated = false;
                                    Logger.ClearLog();
                                }
                                else
                                {
                                    crop = await bmp.Crop(new Point(0, 0), new Size(500, 500));
                                    Point? mail = crop.FindImage("Images\\mail.png", 0.6);
                                    if (mail.HasValue)
                                    {
                                        if (inSpin)
                                        {
                                            Logger.LogInfo("Found mail, exiting spin...");
                                            _ = await w.EndSpin(bmp);
                                            inSpin = false;
                                        }
                                        else
                                        {
                                            Logger.LogInfo("Found Mail");
                                        }
                                        //collect mail
                                        if (await m.CollectMails(bmp))
                                        {
                                            suspendCollect = false;
                                            bmp = await devTools.Screenshot();
                                        }
                                    }
                                    if (stageCount >= planetSettings.InstanceHitCount && planetSettings.InstanceHitCount > 0 && !suspendCollect && !runningRestrict && !runningTrial && !runningConstellation)
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
                                                    Model.SGO2.GetFrameResponse url = await httpService.GetIFrameUrl(userID);
                                                    browser.Load("https://client.guerradenaves.lat/?userId=" + url.Data.UserId + "&sessionKey=" + url.Data.SessionKey);
                                                    await Task.Delay(botSettings.Delays);
                                                    lastRefresh = DateTime.Now;
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
                                                    Model.SGO2.GetFrameResponse url = await httpService.GetIFrameUrl(userID);
                                                    browser.Load("https://client.guerradenaves.lat/?userId=" + url.Data.UserId + "&sessionKey=" + url.Data.SessionKey);
                                                    await Task.Delay(botSettings.Delays);
                                                    lastRefresh = DateTime.Now;
                                                    break;
                                                }
                                                Cancellation.ThrowIfCancellationRequested();
                                            }
                                        }
                                    }
                                }
                                if (planetSettings.SpinWheel)
                                {
                                    if (spinable)
                                    {
                                        if (!inSpin)
                                        {
                                            Logger.LogInfo("Lets spin wheel while waiting instance");
                                            _ = await w.GetIn(bmp);
                                            inSpin = true;
                                        }
                                        else
                                        {
                                            if (resources.Vouchers < planetSettings.MinVouchers)
                                            {
                                                Logger.LogWarning("Not enough vouchers for spinning, exiting");
                                                _ = await w.EndSpin(bmp);
                                                inSpin = false;
                                                spinable = false;
                                                spinResult = SpinResult.NotEnoughVouchers;
                                            }
                                            else
                                            {
                                                spinResult = await w.Spin(bmp, resources, planetSettings.SpinWithVouchers);
                                                if (spinResult == SpinResult.Failed)
                                                {
                                                    //stop spin, something wrong
                                                    Logger.LogWarning("Spin seems failed! Not going to spin!");
                                                    await Task.Delay(50);
                                                    bmp = await devTools.Screenshot();
                                                    _ = await w.EndSpin(bmp);
                                                    inSpin = false;
                                                    spinable = false;
                                                }
                                                else if (spinResult == SpinResult.Vouchers)
                                                {
                                                    if (planetSettings.SpinWithVouchers)
                                                    {
                                                        Logger.LogInfo("Predicted " + resources.Vouchers + " left!");
                                                    }
                                                    else
                                                    {
                                                        Logger.LogWarning("Voucher Spins! Auto canceling!");
                                                        await Task.Delay(50);
                                                        bmp = await devTools.Screenshot();
                                                        _ = await w.EndSpin(bmp);
                                                        inSpin = false;
                                                        spinable = false;
                                                    }
                                                }
                                                else if (spinResult == SpinResult.Success)
                                                {
                                                    Logger.LogInfo("Free Spin!");
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        //detect vouchers
                                        if (resources.Vouchers > planetSettings.MinVouchers && spinResult == SpinResult.NotEnoughVouchers)
                                        {
                                            Logger.LogInfo("Vouchers are now enough for spinning!");
                                            spinable = true;
                                        }
                                        else if (spinResult == SpinResult.Failed)
                                        {
                                            Logger.LogInfo("Lets retry spin after error!");
                                            spinable = true;
                                        }
                                        else if (spinResult == SpinResult.Vouchers && planetSettings.SpinWithVouchers)
                                        {
                                            Logger.LogInfo("Spin with vouchers enabled! Lets spin now!");
                                            spinable = true;
                                        }
                                    }
                                }
                                else if (inSpin)
                                {
                                    _ = await w.EndSpin(bmp);
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
                                            Model.SGO2.GetFrameResponse url = await httpService.GetIFrameUrl(userID);
                                            browser.Load("https://client.guerradenaves.lat/?userId=" + url.Data.UserId + "&sessionKey=" + url.Data.SessionKey);
                                            await Task.Delay(botSettings.Delays);
                                            Cancellation.ThrowIfCancellationRequested();
                                            lastRefresh = DateTime.Now;
                                            break;
                                        }
                                    }
                                    Point? detect = bmp.FindImageGrayscaled("Images\\MPOK.png", 0.8);
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
                                    Model.SGO2.GetFrameResponse url = await httpService.GetIFrameUrl(userID);
                                    browser.Load("https://client.guerradenaves.lat/?userId=" + url.Data.UserId + "&sessionKey=" + url.Data.SessionKey);
                                    lag = 0;
                                    inStage = false;
                                    mainScreenLocated = false;
                                    spaceStationLocated = false;
                                    lastRefresh = DateTime.Now;
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
                        await Task.Delay(botSettings.Delays - 100);
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
