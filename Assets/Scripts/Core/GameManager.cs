using System;
using UnityEngine;

namespace Cellstorm.Core
{
    /// <summary>
    /// Lives in the Game scene. Owns the run state and the time-scale based pause.
    /// Playing and Menu run at normal time; every other state freezes the game (timeScale 0).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] GameState startState = GameState.Playing;

        public GameState State { get; private set; }
        public bool IsPlaying => State == GameState.Playing;

        /// <summary>Raised after a state change: (previous, current).</summary>
        public event Action<GameState, GameState> StateChanged;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void OnEnable()
        {
            InputService.Pause.performed += OnPausePerformed;
        }

        void OnDisable()
        {
            if (InputService.Pause != null)
                InputService.Pause.performed -= OnPausePerformed;
        }

        void Start()
        {
            SetState(startState, force: true);
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                Time.timeScale = 1f; // never leave the next scene frozen
            }
        }

        void OnPausePerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {
            TogglePause();
        }

        public void TogglePause()
        {
            if (State == GameState.Playing) SetState(GameState.Paused);
            else if (State == GameState.Paused) SetState(GameState.Playing);
        }

        public void SetState(GameState newState, bool force = false)
        {
            if (!force && newState == State) return;

            var previous = State;
            State = newState;
            Time.timeScale = TimeScaleFor(newState);
            StateChanged?.Invoke(previous, newState);
        }

        static float TimeScaleFor(GameState state)
        {
            switch (state)
            {
                case GameState.Menu:
                case GameState.Playing:
                    return 1f;
                default:
                    return 0f;
            }
        }
    }
}
