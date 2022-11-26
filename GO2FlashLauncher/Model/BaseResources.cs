namespace GO2FlashLauncher.Model
{
    internal class BaseResources
    {
        public long Metal { get; set; }
        public long HE3 { get; set; }
        public long Gold { get; set; }
        public int MP { get; set; }
        public int Vouchers { get; set; }

        public override string ToString()
        {
            return $"Metal: {Metal}\nHE3: {HE3}\nGold: {Gold}\nMP: {MP}\nVouchers: {Vouchers}";
        }
    }
}
