using System;

namespace SlingshotRunner
{
    public sealed class GameStateMachine
    {
        public event Action<GameState, GameState> StateChanged;

        public GameState State { get; private set; } = GameState.Menu;

        public bool Is(GameState state)
        {
            return State == state;
        }

        public void SetState(GameState nextState, bool force = false)
        {
            if (!force && State == nextState)
            {
                return;
            }

            GameState previousState = State;
            State = nextState;
            StateChanged?.Invoke(previousState, nextState);
        }
    }
}
