using System;
using UnityEngine;

namespace Core
{
    public class GameStateMachine
    {
        public GameState Current { get; private set; } = GameState.Init;
        
        public event Action<GameState> OnStateChanged;

        public void ChangeState(GameState newState)
        {
            if (Current == newState) return;
            Current = newState;
            OnStateChanged?.Invoke(Current);
            
            Debug.Log("Game state changed to: " + Current);
        }
    }
}