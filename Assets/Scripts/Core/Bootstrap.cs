using UnityEngine;
using UnityEngine.SceneManagement;
using Services.Pooling;

namespace Core
{
    public class Bootstrap : MonoBehaviour
    {
        private static Bootstrap _instance;
        public static GameStateMachine StateMachine { get; private set; }
        
        public static PoolService Pools { get; private set; }

        private void Awake()
        {
            if (_instance != null)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            StateMachine = new GameStateMachine();
            StateMachine.OnStateChanged += HandleState;

            StateMachine.ChangeState(GameState.Menu);
            
            Pools = new PoolService();
        }

        private void HandleState(GameState state)
        {
            switch (state)
            {
                case GameState.Menu:
                    SceneManager.LoadScene("Game");
                    break;
                //TODO
            }
        }
    }
}