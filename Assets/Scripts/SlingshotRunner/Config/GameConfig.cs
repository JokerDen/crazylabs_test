using UnityEngine;

namespace SlingshotRunner
{
    [CreateAssetMenu(fileName = "SlingshotRunnerGameConfig", menuName = "Slingshot Runner/Game Config")]
    public sealed class GameConfig : ScriptableObject
    {
        [Header("Economy")]
        [SerializeField] private int startingCurrency = 150;
        [SerializeField] private float metersPerDistanceCoin = 1f;
        [SerializeField] private float distanceCoinMultiplier = 5f;
        [SerializeField] private UpgradePricing[] upgradePricing =
        {
            new UpgradePricing { type = UpgradeType.ShotPower, basePrice = 100, priceMultiplier = 1.65f, maxLevel = 12 },
            new UpgradePricing { type = UpgradeType.SlideAbility, basePrice = 100, priceMultiplier = 1.6f, maxLevel = 12 },
            new UpgradePricing { type = UpgradeType.IncomeMultiplier, basePrice = 100, priceMultiplier = 1.7f, maxLevel = 10 }
        };

        [Header("Input")]
        [SerializeField] private float dragPixelsForFullPower = 330f;
        [SerializeField] private float launchThreshold = 0.05f;
        [SerializeField] private float tapMoveTolerancePixels = 28f;

        public int StartingCurrency => Mathf.Max(0, startingCurrency);
        public float DragPixelsForFullPower => Mathf.Max(1f, dragPixelsForFullPower);
        public float LaunchThreshold => Mathf.Clamp01(launchThreshold);
        public float TapMoveTolerancePixels => Mathf.Max(0f, tapMoveTolerancePixels);
        public float MetersPerDistanceCoin => Mathf.Max(1f, metersPerDistanceCoin);
        public float DistanceCoinMultiplier => Mathf.Max(0f, distanceCoinMultiplier);
        public UpgradePricing[] UpgradePricing => upgradePricing;
    }
}
