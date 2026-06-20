using System;
using System.Collections.Generic;
using System.Globalization;

namespace SlingshotRunner
{
    public enum TextKey
    {
        TapToPlay,
        Back,
        Continue,
        CurrencyFormat,
        UpgradeShotPower,
        UpgradeSlideAbility,
        UpgradeIncomeMultiplier,
        UpgradeLevelFormat,
        UpgradePriceFormat,
        UpgradeMaxPrice,
        PowerFormat,
        RunningHudFormat,
        SpeedometerFormat,
        RunEndedFormat
    }

    public interface ITextsProvider
    {
        string Get(TextKey key);
        string Format(TextKey key, params object[] arguments);
    }

    public sealed class TextsProvider : ITextsProvider
    {
        private static readonly IReadOnlyDictionary<TextKey, string> Texts =
            new Dictionary<TextKey, string>
            {
                { TextKey.TapToPlay, "TAP TO PLAY" },
                { TextKey.Back, "BACK" },
                { TextKey.Continue, "CONTINUE" },
                { TextKey.CurrencyFormat, "<sprite=0> {0}" },
                { TextKey.UpgradeShotPower, "POWER" },
                { TextKey.UpgradeSlideAbility, "SLIDE" },
                { TextKey.UpgradeIncomeMultiplier, "INCOME" },
                { TextKey.UpgradeLevelFormat, "LV {0}" },
                { TextKey.UpgradePriceFormat, "<sprite=0> {0}" },
                { TextKey.UpgradeMaxPrice, "MAX" },
                { TextKey.PowerFormat, "POWER {0}%" },
                { TextKey.RunningHudFormat, "{0} m   <sprite=0> {1}" },
                { TextKey.SpeedometerFormat, "SPEED {0:0.0} m/s" },
                { TextKey.RunEndedFormat, "EARNED <sprite=0> {0}\nDISTANCE {1} m" }
            };

        public string Get(TextKey key)
        {
            return Texts.TryGetValue(key, out string text) ? text : key.ToString();
        }

        public string Format(TextKey key, params object[] arguments)
        {
            return string.Format(CultureInfo.InvariantCulture, Get(key), arguments ?? Array.Empty<object>());
        }
    }
}
