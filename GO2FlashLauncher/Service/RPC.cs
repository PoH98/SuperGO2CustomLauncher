using DiscordRPC;

namespace GO2FlashLauncher.Service
{
    internal class RPC
    {
        private readonly string ClientId = "701835165407903744";
        private readonly DiscordRpcClient rpc;
        private bool Inited = false;
        private Timestamps startTime;
        public RPC()
        {
            rpc = new DiscordRpcClient(ClientId);
            startTime = Timestamps.Now;
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
                    Timestamps = startTime,
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
