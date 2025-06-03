using System;
using UnityEngine;

public class GameStateMachine
{
    public GameState Current { get; private set; } = GameState.Init;
    public GameState PreviousState { get; private set; } = GameState.Init;
        
        public event Action<GameState> OnStateChanged;

        public void ChangeState(GameState newState)
        {
            if (Current == newState) return;
            PreviousState = Current;
            Current = newState;
            OnStateChanged?.Invoke(Current);
            
        Debug.Log($"Game state changed from {PreviousState} to: {Current}");
    }
}
