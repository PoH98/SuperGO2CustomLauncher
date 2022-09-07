using DiscordRPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GO2FlashLauncher.Service
{
    internal class RPC
    {
        private readonly string ClientId = "701835165407903744";
        private readonly DiscordRpcClient rpc;
        private bool Inited = false;
        private DateTime startTime;
        public RPC()
        {
            rpc = new DiscordRpcClient(ClientId);
            startTime = DateTime.Now;
        }

        public void SetPresence()
        {
            if (!Inited)
            {
                Inited = rpc.Initialize();
            }
            if (Inited)
            {
                rpc.SetPresence(new RichPresence()
                {
                    Details = "Playing Stage",
                    State = "A New Galaxy Awakens",
                    Assets = new Assets()
                    {
                        LargeImageKey = "main-logo"
                    },
                    Timestamps = Timestamps.Now,
                    Buttons = new Button[]
                    {
                        new Button()
                        {
                            Label = "Website",
                            Url = "https://supergo2.com"
                        }
                    }
                });
            }
        }
    }
}
