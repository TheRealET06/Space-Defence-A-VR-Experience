using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Brian
{
    
    public enum GameState { Ready, Playing, Won, Lost }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public WaveAudioDirector wd;

        public GameState State { get; private set; } = GameState.Ready;
        public float ElapsedTime { get; private set; }
        public bool IsPlaying => State == GameState.Playing;
        public bool IsOver => State == GameState.Won || State == GameState.Lost;

        public event Action<GameState> StateChanged;

        void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (IsPlaying) ElapsedTime += Time.deltaTime;
        }

        public void StartGame()
        {
            if (State != GameState.Ready) return;
            State = GameState.Playing;
            StateChanged?.Invoke(State);
        }

        public void Win()
        {
            wd.PlayGameWon();
            End(GameState.Won);
        }

        public void Lose()
        {
            wd.PlayGameLost();
            End(GameState.Lost);
        }

        void End(GameState result)
        {
            if (!IsPlaying) return;
            State = result;
            Time.timeScale = 0f;
            StateChanged?.Invoke(State);
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
