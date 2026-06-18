using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

namespace SlingshotRunner
{
    [DisallowMultipleComponent]
    public sealed class UpgradeButtonView : MonoBehaviour
    {
        [SerializeField] private UpgradeType upgradeType;
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text priceText;

        public event Action<UpgradeType> Clicked;

        public UpgradeType UpgradeType => upgradeType;

        private void OnEnable()
        {
            if (button != null)
            {
                button.onClick.AddListener(HandleClicked);
            }
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClicked);
            }
        }

        public void Render(string title, string level, string price, bool canAfford)
        {
            if (titleText != null)
            {
                titleText.text = title;
            }

            if (levelText != null)
            {
                levelText.text = level;
            }

            if (priceText != null)
            {
                priceText.text = price;
            }

            if (button != null)
            {
                button.interactable = canAfford;
            }
        }

        private void HandleClicked()
        {
            Clicked?.Invoke(upgradeType);
        }
    }
}
