using CefSharp.WinForms;
using GO2FlashLauncher.Model;
using GO2FlashLauncher.Service;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GO2FlashLauncher.Script
{
    internal abstract class AbstractScript
    {
        protected bool IsRunning;
        protected readonly BotSettings botSettings;
        protected BaseResources resources = new BaseResources();
        protected DateTime BotStartTime;
        public bool IsReloading { get; set; }
        private CancellationTokenSource CancellationToken = new CancellationTokenSource();
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
        public AbstractScript(BotSettings settings)
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
