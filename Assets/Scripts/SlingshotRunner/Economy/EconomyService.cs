using System;
using UnityEngine;

namespace SlingshotRunner
{
    public sealed class EconomyService
    {
        private const string CurrencyKey = "SlingshotRunner.Currency";
        private const string ShotPowerKey = "SlingshotRunner.ShotPower";
        private const string SlideAbilityKey = "SlingshotRunner.SlideAbility";
        private const string IncomeMultiplierKey = "SlingshotRunner.IncomeMultiplier";

        private readonly int startingCurrency;
        private readonly UpgradePricing[] pricing;
        private readonly float metersPerDistanceCoin;
        private readonly float distanceCoinMultiplier;

        private int currency;
        private int shotPowerLevel;
        private int slideAbilityLevel;
        private int incomeMultiplierLevel;

        public int Currency => currency;
        public int ShotPowerBonusLevel => GetBonusLevel(shotPowerLevel);
        public int SlideAbilityBonusLevel => GetBonusLevel(slideAbilityLevel);

        public EconomyService(int startingCurrency, UpgradePricing[] pricing, float metersPerDistanceCoin, float distanceCoinMultiplier)
        {
            this.startingCurrency = Mathf.Max(0, startingCurrency);
            this.pricing = pricing != null ? pricing : Array.Empty<UpgradePricing>();
            this.metersPerDistanceCoin = Mathf.Max(1f, metersPerDistanceCoin);
            this.distanceCoinMultiplier = Mathf.Max(0f, distanceCoinMultiplier);
        }

        public void Load()
        {
            currency = PlayerPrefs.GetInt(CurrencyKey, startingCurrency);
            shotPowerLevel = Mathf.Max(1, PlayerPrefs.GetInt(ShotPowerKey, 1));
            slideAbilityLevel = Mathf.Max(1, PlayerPrefs.GetInt(SlideAbilityKey, 1));
            incomeMultiplierLevel = Mathf.Max(1, PlayerPrefs.GetInt(IncomeMultiplierKey, 1));
        }

        public void AddCurrency(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            currency += amount;
            Save();
        }

        public bool TryPurchase(UpgradeType upgradeType)
        {
            int level = GetUpgradeLevel(upgradeType);
            int price = GetUpgradePrice(upgradeType, level);
            if (price < 0 || currency < price)
            {
                return false;
            }

            currency -= price;
            SetUpgradeLevel(upgradeType, level + 1);
            Save();
            return true;
        }

        public int GetUpgradeLevel(UpgradeType upgradeType)
        {
            switch (upgradeType)
            {
                case UpgradeType.ShotPower:
                    return shotPowerLevel;
                case UpgradeType.SlideAbility:
                    return slideAbilityLevel;
                case UpgradeType.IncomeMultiplier:
                    return incomeMultiplierLevel;
                default:
                    return 1;
            }
        }

        public int GetUpgradePrice(UpgradeType upgradeType)
        {
            return GetUpgradePrice(upgradeType, GetUpgradeLevel(upgradeType));
        }

        public bool CanAfford(UpgradeType upgradeType)
        {
            int price = GetUpgradePrice(upgradeType);
            return price >= 0 && currency >= price;
        }

        public int CalculateRunEarnings(int collectedCoinValue, float distanceMeters)
        {
            int distanceCoins = Mathf.FloorToInt(Mathf.Max(0f, distanceMeters) / metersPerDistanceCoin * distanceCoinMultiplier);
            int baseCoins = Mathf.Max(0, collectedCoinValue) + distanceCoins;
            float multiplier = 1f + GetBonusLevel(incomeMultiplierLevel) * 0.25f;
            return Mathf.RoundToInt(baseCoins * multiplier);
        }

        private void Save()
        {
            PlayerPrefs.SetInt(CurrencyKey, currency);
            PlayerPrefs.SetInt(ShotPowerKey, shotPowerLevel);
            PlayerPrefs.SetInt(SlideAbilityKey, slideAbilityLevel);
            PlayerPrefs.SetInt(IncomeMultiplierKey, incomeMultiplierLevel);
            PlayerPrefs.Save();
        }

        private int GetUpgradePrice(UpgradeType upgradeType, int level)
        {
            UpgradePricing upgradePricing = FindPricing(upgradeType);
            if (upgradePricing.maxLevel > 0 && level >= upgradePricing.maxLevel)
            {
                return -1;
            }

            int priceStep = GetBonusLevel(level);
            return Mathf.RoundToInt(upgradePricing.basePrice * Mathf.Pow(Mathf.Max(1f, upgradePricing.priceMultiplier), priceStep));
        }

        private void SetUpgradeLevel(UpgradeType upgradeType, int level)
        {
            switch (upgradeType)
            {
                case UpgradeType.ShotPower:
                    shotPowerLevel = Mathf.Max(1, level);
                    break;
                case UpgradeType.SlideAbility:
                    slideAbilityLevel = Mathf.Max(1, level);
                    break;
                case UpgradeType.IncomeMultiplier:
                    incomeMultiplierLevel = Mathf.Max(1, level);
                    break;
            }
        }

        private UpgradePricing FindPricing(UpgradeType upgradeType)
        {
            for (int i = 0; i < pricing.Length; i++)
            {
                if (pricing[i].type == upgradeType)
                {
                    return pricing[i];
                }
            }

            return new UpgradePricing { type = upgradeType, basePrice = 25, priceMultiplier = 1.6f, maxLevel = 10 };
        }

        private static int GetBonusLevel(int level)
        {
            return Mathf.Max(0, level - 1);
        }
    }
}
