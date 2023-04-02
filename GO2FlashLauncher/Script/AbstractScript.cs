using CefSharp.WinForms;
using GO2FlashLauncher.Model;
using GO2FlashLauncher.Service;
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace GO2FlashLauncher.Script
{
    internal abstract class AbstractScript
    {
        protected bool IsRunning;
        protected readonly BotSettings botSettings;
        protected readonly PlanetSettings planetSettings;
        protected BaseResources resources = new BaseResources();
        protected DateTime BotStartTime;
        public bool IsReloading { get; set; }
        public Bitmap lastbmp { get; set; }
        private readonly CancellationTokenSource CancellationToken = new CancellationTokenSource();

        public BaseResources Resources => resources;
        public TimeSpan BotRuntime => DateTime.Now - BotStartTime;
        public AbstractScript(BotSettings settings, PlanetSettings planetSettings)
        {
            botSettings = settings;
            this.planetSettings = planetSettings;
        }

        protected AbstractScript(BotSettings settings)
        {
            botSettings = settings;
        }

        /// <summary>
        /// Check if script is running
        /// </summary>
        public bool Running => IsRunning;

        protected CancellationToken Cancellation => CancellationToken.Token;
        /// <summary>
        /// Main loop of script
        /// </summary>
        /// <param name="token"></param>
        /// <param name="browser"></param>
        /// <returns></returns>
        public abstract Task Run(ChromiumWebBrowser browser, int userID, GO2HttpService httpService);
        public void Stop()
        {
            IsRunning = false;
            CancellationToken.Cancel();
        }
    }
}
