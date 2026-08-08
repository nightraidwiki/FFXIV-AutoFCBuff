namespace AutoFCBuff.Core;

public static class AfcConstants
{
    public const string PrimaryCommand = "/afc";
    public const string AliasCommand = "/autofcbuff";

    public const int MaxFcStockLimit = 15;
    public const int SaveThrottleMs = 1000;

    public static class ThrottleKeys
    {
        public const string Save = "AfcSaveConfig";
        public const string VendorInteract = "AfcVendorInteract";
        public const string BuyItem = "AfcBuyItem";
    }
}
