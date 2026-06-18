using System.Collections.Generic;
using UnityEngine;

namespace SlingshotRunner
{
    [DisallowMultipleComponent]
    public sealed class GameController : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private GameConfig config;

        [Header("Scene")]
        [SerializeField] private PlayerController player;
        [SerializeField] private Transform playerSpawn;
        [SerializeField] private PointerInputArea inputArea;
        [SerializeField] private GameUiController uiController;
        [SerializeField] private CameraModeController cameraModeController;
        [SerializeField] private List<CollectibleCoin> coins = new List<CollectibleCoin>();
        [SerializeField] private List<Obstacle> obstacles = new List<Obstacle>();

        private GameStateMachine stateMachine;
        private EconomyService economy;
        private RunSession runSession;
        private ITextsProvider textsProvider;

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError($"{nameof(GameController)} requires a {nameof(GameConfig)} reference.", this);
                enabled = false;
                return;
            }

            stateMachine = new GameStateMachine();
            textsProvider = new TextsProvider();
            economy = new EconomyService(config.StartingCurrency, config.UpgradePricing, config.MetersPerDistanceCoin);
            runSession = new RunSession();

            ServiceLocator.Register(stateMachine);
            ServiceLocator.Register(textsProvider);
            ServiceLocator.Register(economy);
            ServiceLocator.Register(runSession);

            if (uiController != null)
            {
                ServiceLocator.Register(uiController);
            }

            if (cameraModeController != null)
            {
                ServiceLocator.Register(cameraModeController);
            }
        }

        private void OnEnable()
        {
            if (inputArea != null)
            {
                inputArea.PointerPressed += HandlePointerPressed;
                inputArea.PointerDragged += HandlePointerDragged;
                inputArea.PointerReleased += HandlePointerReleased;
            }

            if (uiController != null)
            {
                uiController.BackClicked += EnterMenuState;
                uiController.ContinueClicked += ContinueFromRunEnded;
                uiController.UpgradeClicked += TryPurchaseUpgrade;
            }

            if (stateMachine != null)
            {
                stateMachine.StateChanged += HandleStateChanged;
            }
        }

        private void OnDisable()
        {
            if (inputArea != null)
            {
                inputArea.PointerPressed -= HandlePointerPressed;
                inputArea.PointerDragged -= HandlePointerDragged;
                inputArea.PointerReleased -= HandlePointerReleased;
            }

            if (uiController != null)
            {
                uiController.BackClicked -= EnterMenuState;
                uiController.ContinueClicked -= ContinueFromRunEnded;
                uiController.UpgradeClicked -= TryPurchaseUpgrade;
            }

            if (stateMachine != null)
            {
                stateMachine.StateChanged -= HandleStateChanged;
            }
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister(stateMachine);
            ServiceLocator.Unregister(textsProvider);
            ServiceLocator.Unregister(economy);
            ServiceLocator.Unregister(runSession);

            if (uiController != null)
            {
                ServiceLocator.Unregister(uiController);
            }

            if (cameraModeController != null)
            {
                ServiceLocator.Unregister(cameraModeController);
            }
        }

        private void Start()
        {
            economy.Load();
            ResetRunObjects();
            ResetPlayerToSpawn();
            EnterMenuState();
        }

        private void Update()
        {
            if (!stateMachine.Is(GameState.Running) || player == null)
            {
                return;
            }

            runSession.SetDistance(player.Distance);
            uiController?.RenderRunning(runSession.DistanceMeters, runSession.EarnedCoins);

            if (player.HasStopped())
            {
                EnterRunEndedState();
            }
        }

        private void HandleStateChanged(GameState previousState, GameState nextState)
        {
            uiController?.SetState(nextState);

            if (nextState != GameState.Running)
            {
                uiController?.ResetJoystick();
            }

            if (nextState == GameState.Running || nextState == GameState.RunEnded)
            {
                cameraModeController?.ShowRunning();
            }
            else
            {
                cameraModeController?.ShowMenu();
            }
        }

        private void HandlePointerPressed(Vector2 screenPosition)
        {
            if (stateMachine.Is(GameState.Aiming))
            {
                uiController?.RenderPower(0f);
                ResetPlayerToSpawn();
            }
        }

        private void HandlePointerDragged(Vector2 dragDelta, Vector2 screenPosition)
        {
            if (stateMachine.Is(GameState.Aiming))
            {
                Vector2 pull = CalculatePull(dragDelta);
                float power = CalculatePower(dragDelta);
                player.PreviewSlingshot(pull);
                uiController?.RenderPower(power);
                return;
            }

            if (stateMachine.Is(GameState.Running))
            {
                Vector2 joystickInput = CalculateJoystickInput(dragDelta);
                player.SetSteerInput(joystickInput.x);
                uiController?.RenderJoystick(joystickInput);
            }
        }

        private void HandlePointerReleased(Vector2 dragDelta, Vector2 screenPosition, float heldSeconds)
        {
            bool isTap = dragDelta.magnitude <= config.TapMoveTolerancePixels;

            if (stateMachine.Is(GameState.Menu))
            {
                if (isTap)
                {
                    EnterAimingState();
                }

                return;
            }

            if (stateMachine.Is(GameState.Aiming))
            {
                float power = CalculatePower(dragDelta);
                Vector2 pull = CalculatePull(dragDelta);
                if (power > config.LaunchThreshold)
                {
                    EnterRunningState(power, pull);
                }
                else
                {
                    ResetPlayerToSpawn();
                    uiController?.RenderPower(0f);
                }

                return;
            }

            if (stateMachine.Is(GameState.Running))
            {
                player.SetSteerInput(0f);
                uiController?.ResetJoystick();
            }
        }

        private void EnterMenuState()
        {
            stateMachine.SetState(GameState.Menu, true);
            inputArea?.Cancel();
            ResetPlayerToSpawn();
            RenderEconomy();
        }

        private void EnterAimingState()
        {
            stateMachine.SetState(GameState.Aiming);
            ResetPlayerToSpawn();
            uiController?.RenderPower(0f);
        }

        private void EnterRunningState(float power, Vector2 pull)
        {
            stateMachine.SetState(GameState.Running);
            runSession.Reset();

            uiController?.ResetJoystick();
            player.StartRun(power, pull, economy.ShotPowerBonusLevel, economy.SlideAbilityBonusLevel);
            uiController?.RenderRunning(runSession.DistanceMeters, runSession.EarnedCoins);
        }

        private void EnterRunEndedState()
        {
            stateMachine.SetState(GameState.RunEnded);
            inputArea?.Cancel();
            player.StopPhysics();
            runSession.RefreshEarnings();
            uiController?.RenderRunEnded(runSession.EarnedCoins, runSession.DistanceMeters);
        }

        private void ContinueFromRunEnded()
        {
            if (!stateMachine.Is(GameState.RunEnded))
            {
                return;
            }

            economy.AddCurrency(runSession.EarnedCoins);
            ResetRunObjects();
            runSession.Reset();
            EnterMenuState();
        }

        private void TryPurchaseUpgrade(UpgradeType upgradeType)
        {
            if (!stateMachine.Is(GameState.Menu))
            {
                return;
            }

            economy.TryPurchase(upgradeType);
            RenderEconomy();
        }

        private Vector2 CalculatePull(Vector2 dragDelta)
        {
            float scale = config.DragPixelsForFullPower;
            float power = CalculatePower(dragDelta);
            if (power <= 0f)
            {
                return Vector2.zero;
            }

            float side = Mathf.Clamp(dragDelta.x / scale, -power, power);
            return Vector2.ClampMagnitude(new Vector2(side, power), 1f);
        }

        private float CalculatePower(Vector2 dragDelta)
        {
            return Mathf.Clamp01(-dragDelta.y / config.DragPixelsForFullPower);
        }

        private Vector2 CalculateJoystickInput(Vector2 dragDelta)
        {
            float dragRange = config.DragPixelsForFullPower * 0.45f;
            if (dragRange <= 0f)
            {
                return Vector2.zero;
            }

            return Vector2.ClampMagnitude(dragDelta / dragRange, 1f);
        }

        private void RenderEconomy()
        {
            uiController?.RenderCurrency(economy.Currency);
            uiController?.RenderUpgrades();
        }

        private void ResetRunObjects()
        {
            for (int i = 0; i < coins.Count; i++)
            {
                if (coins[i] != null)
                {
                    coins[i].ResetCoin();
                }
            }

            for (int i = 0; i < obstacles.Count; i++)
            {
                if (obstacles[i] != null)
                {
                    obstacles[i].ResetObstacle();
                }
            }
        }

        private void ResetPlayerToSpawn()
        {
            if (player != null && playerSpawn != null)
            {
                player.ResetToSpawn(playerSpawn.position, playerSpawn.rotation);
            }
        }
    }
}
