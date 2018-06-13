﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using Run4YourLife.UI;

using Run4YourLife.Player;
using Run4YourLife.Utils;

using Cinemachine;

namespace Run4YourLife.GameManagement
{
    [RequireComponent(typeof(BossFightGemManager))]
    public class BossFightPhaseManager : GamePhaseManager
    {
        public override GamePhase GamePhase { get { return GamePhase.BossFight; } }

        #region Editor variables

        [SerializeField]
        private CinemachineVirtualCamera m_virtualCamera;

        [SerializeField]
        private Transform m_bossFightStartingCameraPositionDebug;

        [SerializeField]
        private Tiling m_backgroundTiling;

        [SerializeField]
        private Transform[] m_runnerSpawns;

        #endregion

        #region Member variables

        private PlayerSpawner m_playerSpawner;

        private GameObject m_ui;

        #endregion

        #region Initialization

        private void Awake()
        {
            m_ui = GameObject.FindGameObjectWithTag(Tags.UI);

            m_playerSpawner = GetComponent<PlayerSpawner>();
            Debug.Assert(m_playerSpawner != null);
        }

        #endregion

        #region Regular Execution

        public override void StartPhase()
        {
            m_playerSpawner.ActivateBoss();
            StartPhaseCommon();
        }

        void StartPhaseCommon()
        {
            GameObject boss = GameplayPlayerManager.Instance.Boss;
            Debug.Assert(boss != null);

            m_virtualCamera.Follow = boss.transform;
            m_virtualCamera.LookAt = boss.transform;
            CameraManager.Instance.TransitionToCamera(m_virtualCamera);

            m_backgroundTiling.SetActive(false);

            ExecuteEvents.Execute<IUICrossHairEvents>(m_ui, null, (a,b) => a.ShowCrossHair());

            StartCoroutine(YieldHelper.SkipFrame(()=>MoveRunners()));           
        }

        private void MoveRunners()
        {
            for (int i = 0; i < GameplayPlayerManager.Instance.Runners.Count; i++)
            {
                GameObject runner = GameplayPlayerManager.Instance.Runners[i];
                runner.transform.position = m_runnerSpawns[i].position;
                RunnerCharacterController runnerCharacterController = runner.GetComponent<RunnerCharacterController>();
                runnerCharacterController.CheckOutScreen = true;
            }
        }

        public void StartNextPhase()
        {
            GameManager.Instance.EndExecutingPhaseAndStartPhase(GamePhase.BossFightRockTransition);
        }

        public override void EndPhase()
        {
            EndPhaseCommon();
        }

        private void EndPhaseCommon()
        {
            foreach (GameObject runner in GameplayPlayerManager.Instance.Runners)
            {
                RunnerCharacterController runnerCharacterController = runner.GetComponent<RunnerCharacterController>(); 
                runnerCharacterController.CheckOutScreen = false; 
            }
            ExecuteEvents.Execute<IUICrossHairEvents>(m_ui, null, (a,b) => a.HideCrossHair());
        }

        #endregion

        #region Debug Execution

        public override void DebugStartPhase()
        {
            m_playerSpawner.ActivatePlayers();
            m_virtualCamera.transform.position = m_bossFightStartingCameraPositionDebug.position;

            StartPhaseCommon();
        }

        public override void DebugEndPhase()
        {
            StopAllCoroutines();

            GameplayPlayerManager.Instance.DebugClearAllPlayers();

            EndPhaseCommon();
        }

        #endregion
    }
}
