﻿using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;

namespace SpinDots
{
    public enum GameState
    {
        Prepare,
        Playing,
        Paused,
        PreGameOver,
        GameOver
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public static event System.Action<GameState, GameState> GameStateChanged;

        private static bool isRestart;

        public GameState GameState
        {
            get
            {
                return _gameState;
            }
            private set
            {
                if (value != _gameState)
                {
                    GameState oldState = _gameState;
                    _gameState = value;

                    if (GameStateChanged != null)
                        GameStateChanged(_gameState, oldState);
                }
            }
        }

        public static int GameCount
        {
            get { return _gameCount; }
            private set { _gameCount = value; }
        }

        private static int _gameCount = 0;

        [Header("Set the target frame rate for this game")]
        [Tooltip("Use 60 for games requiring smooth quick motion, set -1 to use platform default frame rate")]
        public int targetFrameRate = 30;

        [Header("Current game state")]
        [SerializeField]
        private GameState _gameState = GameState.Prepare;

        // List of public variable for gameplay tweaking
        [Header("Gameplay Config")]

        [SerializeField]
        protected Vector3 originalPlayerPosition = new Vector3(-1.63f, 2, 0);

        [Range(0f, 1f)]
        public float coinFrequency = 0.1f;

        // List of public variables referencing other objects
        [Header("Object References")]
        public PlayerController playerController;

        public GameObject dieEffect;

        public GameObject[] balls=new GameObject[2];
        public GameObject AdPanel;
        public bool isInvincible;
        public bool isCancelAd;

        public void SetAdPanel(bool a)
        {
            AdPanel.SetActive(a);
            Time.timeScale=a?0:1;
        }

        public void PreDie()
        {
            SetAdPanel(true);
            StartCoroutine("PlayerReallyDie");
        }

        public void Revive()
        {
            AdManager.ShowVideoAd("192if3b93qo6991ed0",
            (bol) => {
                if (bol)
                {

                    SetAdPanel(false);
                    StopCoroutine("PlayerReallyDie");
                    StartCoroutine("Invincible");

                    AdManager.clickid = "";
                    AdManager.getClickid();
                    AdManager.apiSend("game_addiction", AdManager.clickid);
                    AdManager.apiSend("lt_roi", AdManager.clickid);


                }
                else
                {
                    StarkSDKSpace.AndroidUIManager.ShowToast("观看完整视频才能获取奖励哦！");
                }
            },
            (it, str) => {
                Debug.LogError("Error->" + str);
                //AndroidUIManager.ShowToast("广告加载异常，请重新看广告！");
            });
        }

        public IEnumerator Invincible()
        {
            balls[0].GetComponent<Collider>().isTrigger = true;
            balls[1].GetComponent<Collider>().isTrigger = true;
            yield return new WaitForSeconds(3f);
            balls[0].GetComponent<Collider>().isTrigger = false;
            balls[1].GetComponent<Collider>().isTrigger = false;
        }

        public void CancelAd()
        {
            isCancelAd = true;
            SetAdPanel(false);
        }

        public IEnumerator PlayerReallyDie()
        {
            yield return new WaitForSeconds(0.2f);
            BallController.ReallyDie();
        }

        void OnEnable()
        {
            PlayerController.PlayerDied += PlayerController_PlayerDied;
            CharacterScroller.SelectCurCharacter += CreateNewCharacter;
        }

        void OnDisable()
        {
            isCancelAd = false;
            PlayerController.PlayerDied -= PlayerController_PlayerDied;
            CharacterScroller.SelectCurCharacter -= CreateNewCharacter;
        }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                DestroyImmediate(Instance.gameObject);
                Instance = this;
            }
            CreateNewCharacter(CharacterManager.Instance.CurrentCharacterIndex);
        }

        IEnumerator CR_DelayCreateNewCharacter(int curChar)
        {
            yield return new WaitForEndOfFrame();
            GameObject player = Instantiate(CharacterManager.Instance.characters[curChar]);
            player.transform.position = originalPlayerPosition;
            playerController = player.GetComponent<PlayerController>();
            balls = GameObject.FindGameObjectsWithTag("Player");

        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // Use this for initialization
        void Start()
        {
            // Initial setup
            Application.targetFrameRate = targetFrameRate;
            ScoreManager.Instance.Reset();
            PrepareGame();
        }

        // Update is called once per frame
        void CreateNewCharacter(int curChar)
        {
            if(playerController != null)
            {
                DestroyImmediate(playerController.gameObject);
                playerController = null;
            }
            StartCoroutine(CR_DelayCreateNewCharacter(curChar));
        }

        // Listens to the event when player dies and call GameOver
        void PlayerController_PlayerDied()
        {
            GameObject effect = Instantiate(dieEffect, playerController.gameObject.transform.position, Quaternion.identity);
            ParticleSystem par = effect.GetComponent<ParticleSystem>();
            var main = par.main;
            main.startColor = playerController.mainColor;
            GameOver();
        }

        // Make initial setup and preparations before the game can be played
        public void PrepareGame()
        {
            GameState = GameState.Prepare;

            // Automatically start the game if this is a restart.
            if (isRestart)
            {
                isRestart = false;
                StartGame();
            }
        }

        // A new game official starts
        public void StartGame()
        {
            StartCoroutine(DelayStartGame());
        }

        IEnumerator DelayStartGame()
        {
            yield return new WaitForEndOfFrame();
            GameState = GameState.Playing;
            if (SoundManager.Instance.background != null)
            {
                SoundManager.Instance.PlayMusic(SoundManager.Instance.background);
            }
        }
        // Called when the player died
        public void GameOver()
        {
            if (SoundManager.Instance.background != null)
            {
                SoundManager.Instance.StopMusic();
            }

            SoundManager.Instance.PlaySound(SoundManager.Instance.gameOver);
            GameState = GameState.GameOver;
            GameCount++;

            // Add other game over actions here if necessary
        }

        // Start a new game
        public void RestartGame(float delay = 0)
        {
            isRestart = true;
            StartCoroutine(CRRestartGame(delay));
        }

        IEnumerator CRRestartGame(float delay = 0)
        {
            yield return new WaitForSeconds(delay);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void HidePlayer()
        {
            if (playerController != null)
                playerController.gameObject.SetActive(false);
        }

        public void ShowPlayer()
        {
            if (playerController != null)
                playerController.gameObject.SetActive(true);
        }
    }
}
