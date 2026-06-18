using System;

namespace SlingshotRunner
{
    [Serializable]
    public struct UpgradePricing
    {
        public UpgradeType type;
        public int basePrice;
        public float priceMultiplier;
        public int maxLevel;
    }
}
