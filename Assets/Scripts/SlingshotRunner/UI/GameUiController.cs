using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlingshotRunner
{
    [DisallowMultipleComponent]
    public sealed class GameUiController : MonoBehaviour
    {
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private GameObject aimingPanel;
        [SerializeField] private GameObject runningPanel;
        [SerializeField] private GameObject runEndedPanel;
        [SerializeField] private TMP_Text currencyText;
        [SerializeField] private TMP_Text powerText;
        [SerializeField] private TMP_Text runningText;
        [SerializeField] private TMP_Text runEndedText;
        [SerializeField] private TMP_Text tapToPlayText;
        [SerializeField] private TMP_Text backButtonText;
        [SerializeField] private TMP_Text continueButtonText;
        [SerializeField] private Button backButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private RectTransform joystickBase;
        [SerializeField] private RectTransform joystickKnob;
        [SerializeField] private List<UpgradeButtonView> upgradeButtons = new List<UpgradeButtonView>();

        public event Action BackClicked;
        public event Action ContinueClicked;
        public event Action<UpgradeType> UpgradeClicked;

        private readonly ITextsProvider fallbackTextsProvider = new TextsProvider();

        private ITextsProvider Texts => ServiceLocator.TryGet(out ITextsProvider provider) ? provider : fallbackTextsProvider;

        private void Awake()
        {
            ApplyStaticTexts();
        }

        private void OnEnable()
        {
            if (backButton != null)
            {
                backButton.onClick.AddListener(HandleBackClicked);
            }

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(HandleContinueClicked);
            }

            for (int i = 0; i < upgradeButtons.Count; i++)
            {
                if (upgradeButtons[i] != null)
                {
                    upgradeButtons[i].Clicked += HandleUpgradeClicked;
                }
            }
        }

        private void OnDisable()
        {
            if (backButton != null)
            {
                backButton.onClick.RemoveListener(HandleBackClicked);
            }

            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(HandleContinueClicked);
            }

            for (int i = 0; i < upgradeButtons.Count; i++)
            {
                if (upgradeButtons[i] != null)
                {
                    upgradeButtons[i].Clicked -= HandleUpgradeClicked;
                }
            }
        }

        private void ApplyStaticTexts()
        {
            SetText(tapToPlayText, Texts.Get(TextKey.TapToPlay));
            SetText(backButtonText, Texts.Get(TextKey.Back));
            SetText(continueButtonText, Texts.Get(TextKey.Continue));
        }

        public void SetState(GameState state)
        {
            SetPanel(menuPanel, state == GameState.Menu);
            SetPanel(aimingPanel, state == GameState.Aiming);
            SetPanel(runningPanel, state == GameState.Running);
            SetPanel(runEndedPanel, state == GameState.RunEnded);
        }

        public void RenderCurrency(int currency)
        {
            if (currencyText != null)
            {
                currencyText.text = Texts.Format(TextKey.CurrencyFormat, currency);
            }
        }

        public void RenderPower(float power)
        {
            if (powerText != null)
            {
                int percent = Mathf.RoundToInt(Mathf.Clamp01(power) * 100f);
                powerText.text = Texts.Format(TextKey.PowerFormat, percent);
            }
        }

        public void RenderRunning(float distanceMeters, int earnedCoins)
        {
            if (runningText != null)
            {
                runningText.text = Texts.Format(TextKey.RunningHudFormat, Mathf.RoundToInt(distanceMeters), earnedCoins);
            }
        }

        public void RenderRunEnded(int earnedCoins, float distanceMeters)
        {
            if (runEndedText != null)
            {
                runEndedText.text = Texts.Format(TextKey.RunEndedFormat, earnedCoins, Mathf.RoundToInt(distanceMeters));
            }
        }

        public void RenderJoystick(Vector2 normalizedDrag)
        {
            if (joystickKnob == null)
            {
                return;
            }

            joystickKnob.anchoredPosition = Vector2.ClampMagnitude(normalizedDrag, 1f) * GetJoystickKnobRange();
        }

        public void ResetJoystick()
        {
            RenderJoystick(Vector2.zero);
        }

        public void RenderUpgrades()
        {
            if (!ServiceLocator.TryGet(out EconomyService economy))
            {
                return;
            }

            for (int i = 0; i < upgradeButtons.Count; i++)
            {
                UpgradeButtonView view = upgradeButtons[i];
                if (view == null)
                {
                    continue;
                }

                UpgradeType upgradeType = view.UpgradeType;
                int price = economy.GetUpgradePrice(upgradeType);
                view.Render(
                    GetUpgradeTitle(upgradeType),
                    GetUpgradeLevel(economy.GetUpgradeLevel(upgradeType)),
                    GetUpgradePrice(price),
                    economy.CanAfford(upgradeType));
            }
        }

        private void HandleBackClicked()
        {
            BackClicked?.Invoke();
        }

        private void HandleContinueClicked()
        {
            ContinueClicked?.Invoke();
        }

        private void HandleUpgradeClicked(UpgradeType upgradeType)
        {
            UpgradeClicked?.Invoke(upgradeType);
        }

        private string GetUpgradeTitle(UpgradeType upgradeType)
        {
            switch (upgradeType)
            {
                case UpgradeType.ShotPower:
                    return Texts.Get(TextKey.UpgradeShotPower);
                case UpgradeType.SlideAbility:
                    return Texts.Get(TextKey.UpgradeSlideAbility);
                case UpgradeType.IncomeMultiplier:
                    return Texts.Get(TextKey.UpgradeIncomeMultiplier);
                default:
                    return string.Empty;
            }
        }

        private string GetUpgradeLevel(int level)
        {
            return Texts.Format(TextKey.UpgradeLevelFormat, level);
        }

        private string GetUpgradePrice(int price)
        {
            return price >= 0
                ? Texts.Format(TextKey.UpgradePriceFormat, price)
                : Texts.Get(TextKey.UpgradeMaxPrice);
        }

        private static void SetPanel(GameObject panel, bool isVisible)
        {
            if (panel != null)
            {
                panel.SetActive(isVisible);
            }
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        private float GetJoystickKnobRange()
        {
            if (joystickBase == null)
            {
                return 0f;
            }

            float baseRadius = Mathf.Min(joystickBase.rect.width, joystickBase.rect.height) * 0.5f;
            float knobRadius = Mathf.Max(joystickKnob.rect.width, joystickKnob.rect.height) * 0.5f;
            return Mathf.Max(0f, baseRadius - knobRadius);
        }
    }
}
