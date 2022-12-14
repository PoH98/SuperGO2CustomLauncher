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
        private CancellationTokenSource CancellationToken = new CancellationTokenSource();
        private BotSettings settings;

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
        public AbstractScript(BotSettings settings, PlanetSettings planetSettings)
        {
            this.botSettings = settings;
            this.planetSettings = planetSettings;
        }

        protected AbstractScript(BotSettings settings)
        {
            this.settings = settings;
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

        protected CancellationToken Cancellation
        {
            get
            {
                return this.CancellationToken.Token;
            }
        }
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
