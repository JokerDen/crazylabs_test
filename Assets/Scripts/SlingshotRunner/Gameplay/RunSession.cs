using UnityEngine;

namespace SlingshotRunner
{
    public sealed class RunSession
    {
        public int CollectedCoinValue { get; private set; }
        public int EarnedCoins { get; private set; }
        public float DistanceMeters { get; private set; }

        public void Reset()
        {
            CollectedCoinValue = 0;
            EarnedCoins = 0;
            DistanceMeters = 0f;
        }

        public void SetDistance(float distanceMeters)
        {
            DistanceMeters = Mathf.Max(0f, distanceMeters);
            RefreshEarnings();
        }

        public void CollectCoin(CollectibleCoin coin)
        {
            if (coin == null)
            {
                return;
            }

            CollectedCoinValue += Mathf.Max(1, coin.BaseValue);
            RefreshEarnings();
        }

        public void RefreshEarnings()
        {
            EarnedCoins = ServiceLocator.Get<EconomyService>().CalculateRunEarnings(CollectedCoinValue, DistanceMeters);
        }
    }
}
