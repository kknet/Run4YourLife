﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Run4YourLife.Player;
using Run4YourLife.InputManagement;
using Run4YourLife.Debugging;

namespace Run4YourLife.GameManagement {
    public interface IGameplayPlayerEvents : IEventSystemHandler
    {
        void OnRunnerDeath(GameObject player);
        void OnReviveRunnerRequest(Vector3 position);
    }

    [System.Serializable]
    public class OnPlayerReviveEvent : UnityEvent<GameObject> { }

    [RequireComponent(typeof(GameManager))]
    [RequireComponent(typeof(RunnerPrefabManager))]
    public class GameplayPlayerManager : SingletonMonoBehaviour<GameplayPlayerManager>, IGameplayPlayerEvents {

        #region Editor

        [SerializeField]
        [Tooltip("All the phase bosses that can be used in the game")]
        private List<GameObject> m_sceneBosses;

        [SerializeField]
        [Tooltip("Event that will be fired when a playerIsRevived")]
        private OnPlayerReviveEvent m_onPlayerRevived;

        [SerializeField]
        private GameObject[] m_runnerSlot;

        [SerializeField]
        private GameObject m_runnerDeathParticles;

        #endregion

        #region Properties

        public GameObject Boss { get { return m_boss; } }
        public List<GameObject> Runners { get { return m_runners; } }
        public List<GameObject> RunnersAlive { get { return m_runnersAlive; } }
        public Queue<PlayerHandle> DeadRunners { get { return m_deadRunners; } }
        public Dictionary<PlayerHandle, GameObject> RunnerGameObject { get { return m_runnerGameObject; } }

        #endregion

        #region Private Members

        private PlayerManager m_playerManager;

        private GameObject m_boss;
        private List<GameObject> m_runners = new List<GameObject>();
        private List<GameObject> m_runnersAlive = new List<GameObject>();
        private Queue<PlayerHandle> m_deadRunners = new Queue<PlayerHandle>();
        private Dictionary<PlayerHandle, GameObject> m_runnerGameObject = new Dictionary<PlayerHandle, GameObject>();
        private Dictionary<PlayerHandle, GameObject> m_ghostGameObject = new Dictionary<PlayerHandle, GameObject>();

        private PlayerHandle m_bossPlayerHandle;
        private List<PlayerHandle> m_runnerPlayerHandles = new List<PlayerHandle>();

        private RunnerPrefabManager m_runnerPrefabManager;

        private int runnerIndex = 0;

        #endregion

        #region Initialize

        private void Awake()
        {
            m_runnerPrefabManager = GetComponent<RunnerPrefabManager>();

            GameManager gameManager = GetComponent<GameManager>();
            gameManager.onGamePhaseChanged.AddListener(OnGamePhaseChanged);
        }

        private void Start()
        {
            InitializePlayers();
        }

        private void InitializePlayers()
        {
            if(PlayerManager.Instance.PlayerHandles.Count == 0)
            {
                CreateDebugPlayers();
            }

            foreach (PlayerHandle playerHandle in PlayerManager.Instance.PlayerHandles)
            {
                if(playerHandle.IsBoss)
                {
                    InitializeBoss(playerHandle);
                } else
                {
                    InitializeRunner(playerHandle);
                }
            }
        }

        private void CreateDebugPlayers()
        {
            PlayerHandle[] playerHandles = new PlayerHandle[]
            {
                new PlayerHandle() {
                    CharacterType = CharacterType.Purple,
                    InputDevice = InputDeviceManager.Instance.InputDevices[0],
                    IsBoss = false
                },
                new PlayerHandle() {
                    CharacterType = CharacterType.Green,
                    InputDevice = InputDeviceManager.Instance.InputDevices[1],
                    IsBoss = true
                },
                new PlayerHandle() {
                    CharacterType = CharacterType.Orange,
                    InputDevice = InputDeviceManager.Instance.InputDevices[2],
                    IsBoss = false
                },
                new PlayerHandle() {
                    CharacterType = CharacterType.Green,
                    InputDevice = InputDeviceManager.Instance.InputDevices[3],
                    IsBoss = false
                }
            };

            foreach (PlayerHandle playerHandle in playerHandles)
            {
                PlayerManager.Instance.AddPlayer(playerHandle);
            }
        }

        private void InitializeRunner(PlayerHandle playerHandle)
        {
            m_runnerPlayerHandles.Add(playerHandle);

            GameObject runner = Instantiate(GetRunnerForPlayer(playerHandle), m_runnerSlot[runnerIndex].transform, false);
            GameObject ghostRunner = Instantiate(GetGhostForPlayer(playerHandle), m_runnerSlot[runnerIndex].transform, false);

            runnerIndex++;
            runner.SetActive(false);
            ghostRunner.SetActive(false);
            OnplayerHandleChanged(runner, playerHandle);
            OnplayerHandleChanged(ghostRunner, playerHandle);

            if (!m_runnersAlive.Contains(runner))
            {
                m_runners.Add(runner);
            }

            m_runnerGameObject[playerHandle] = runner;
            m_ghostGameObject[playerHandle] = ghostRunner;
        }

        private GameObject GetRunnerForPlayer(PlayerHandle playerHandle)
        {
            return m_runnerPrefabManager.GetRunner(playerHandle.CharacterType);
        }

        private GameObject GetGhostForPlayer(PlayerHandle playerHandle)
        {
            return m_runnerPrefabManager.GetGhost(playerHandle.CharacterType);
        }

        private void InitializeBoss(PlayerHandle playerHandle)
        {
            foreach (GameObject boss in m_sceneBosses)
            {
                OnplayerHandleChanged(boss, playerHandle);
            }
        }

        public void OnplayerHandleChanged(GameObject player, PlayerHandle playerHandle)
        {
            foreach(IPlayerHandleEvent playerHandleEvent in player.GetComponents<IPlayerHandleEvent>())
            {
                playerHandleEvent.OnPlayerHandleChanged(playerHandle);
            }
        }

        #endregion

        public void OnRunnerDeath(GameObject runner)
        {
            PlayerHandle playerHandle = runner.GetComponent<PlayerInstance>().PlayerHandle;
            m_deadRunners.Enqueue(playerHandle);
            m_runnersAlive.Remove(runner);

            DeactivateRunner(playerHandle);
            ActivateRunnerGhost(playerHandle,runner.transform.position);

            if (runner.GetComponent<RunnerController>().GetReviveMode())
            {
                ReviveRunner(playerHandle, GetRandomSpawnPosition());
            }
            else if (m_runnersAlive.Count == 0)
            {
                ExecuteEvents.Execute<IGameplayEvents>(gameObject, null, (x,y) =>x.OnGameEnded(GameEndResult.BossWin));
            }
        }

        private void ActivateRunnerGhost(PlayerHandle playerhandle, Vector3 position)
        {
            GameObject ghost = m_ghostGameObject[playerhandle];

            ghost.transform.position = position;
            ghost.SetActive(true);
        }

        public void OnReviveRunnerRequest(Vector3 position)
        {
            if(m_deadRunners.Count > 0)
            { 
                PlayerHandle playerHandle = m_deadRunners.Dequeue();
                ReviveRunner(playerHandle,position);
            }
        }

        public void ReviveRunner(PlayerHandle playerHandle,Vector3 position)
        {
            DeactivateGhost(playerHandle);
            GameObject runner = ActivateRunner(playerHandle, position);
            runner.GetComponent<RunnerController>().RecentlyRevived();
            m_onPlayerRevived.Invoke(runner);
        }

        private void DeactivateGhost(PlayerHandle playerHandle)
        {
            m_ghostGameObject[playerHandle].SetActive(false);
        }

        private void DeactivateRunner(PlayerHandle playerHandle)
        {
            m_runnerGameObject[playerHandle].SetActive(false);
        }

        public GameObject ActivateRunner(PlayerHandle playerHandle, Vector3 position)
        {
            GameObject runner = m_runnerGameObject[playerHandle];

            runner.transform.position = position;
            runner.SetActive(true);
            m_runnersAlive.Add(runner);
            return runner;
        }

        public GameObject ActivateBoss(GamePhase gamePhase, Vector3 position)
        {
            if(m_boss != null)
            {
                m_boss.SetActive(false);
            }

            m_boss = GetBossForPhase(gamePhase);
            m_boss.transform.position = position;
            m_boss.SetActive(true);
            return m_boss;
        }

        private GameObject GetBossForPhase(GamePhase gamePhase)
        {
            switch(gamePhase)
            {
                case GamePhase.EasyMoveHorizontal:
                case GamePhase.TransitionToEasyMoveHorizontal:
                    return m_sceneBosses[0];
                case GamePhase.TransitionToBossFight:
                case GamePhase.BossFight:
                    return m_sceneBosses[1];
                case GamePhase.TransitionToHardMoveHorizontal:
                case GamePhase.HardMoveHorizontal:
                    return m_sceneBosses[2];
                default:
                    Debug.LogError("You are trying to get a boss for a phase that has no boss");
                    return null;
            }
        }

        private void Update()
        {
            if(DebugSystemManagerHelper.DebuggingToolsEnabled())
            {
                if(Input.GetKeyDown(KeyCode.R))
                {
                    ReviveAllRunners();
                }
            }
        }

        public void OnGamePhaseChanged(GamePhase gamePhase)
        {
            if(gamePhase.Equals(GamePhase.TransitionToBossFight) || gamePhase.Equals(GamePhase.TransitionToHardMoveHorizontal))
            {
                ReviveAllRunners(); 
            }
        }

        public void ReviveAllRunners()
        {
            while(m_deadRunners.Count > 0)
            {
                Vector3 position = GetRandomSpawnPosition();
                OnReviveRunnerRequest(position);
            }
        }

        private Vector3 GetRandomSpawnPosition()
        {
            Vector3 position = CameraManager.Instance.MainCamera.transform.position;

            position.x += Random.Range(-3, 6);
            position.y += Random.Range(-2, 5);
            position.z = 0;

            return position;
        }

        public void DebugClearAllPlayers()
        {
            m_boss.SetActive(false);
            m_boss = null;
            for(int i = 0; i< m_runnersAlive.Count;i++)
            {
                m_runnersAlive[i].SetActive(false);

            }

            m_runnersAlive.Clear();
            m_deadRunners.Clear();
        }

        public void DebugClearAllRunners()
        {
            foreach(GameObject runner in m_runnersAlive)
            {
                runner.SetActive(false);
            }

            m_deadRunners.Clear();
            m_runnersAlive.Clear();
        }

        public void DebugActivateAllRunners()
        {
            foreach(GameObject runner in m_runners)
            {
                m_runnersAlive.Add(runner);
                runner.transform.position = GetRandomSpawnPosition();
                runner.SetActive(true);
            }
        }
    }
}
