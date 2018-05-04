﻿using System.Collections.Generic;

using UnityEngine;
using Cinemachine;

using Run4YourLife.Player;

namespace Run4YourLife.GameManagement
{
    public class EasyMoveHorizontalPhaseManager : GamePhaseManager
    {
        #region Editor variables

        [SerializeField]
        private CinemachineVirtualCamera m_virtualCamera;

        [SerializeField]
        private BossPath m_checkPointManager;

        #endregion

        #region Member variables

        private PlayerSpawner m_playerSpawner;

        #endregion

        #region Initialization

        private void Awake()
        {
            m_playerSpawner = GetComponent<PlayerSpawner>();
            UnityEngine.Debug.Assert(m_playerSpawner != null);

            RegisterPhase(GamePhase.EasyMoveHorizontal);
        }

        #endregion

        #region Regular Execution

        public override void StartPhase()
        {
            StartPhaseCommon();
        }

        void StartPhaseCommon()
        {
            GameObject boss = GameplayPlayerManager.Instance.Boss;
            UnityEngine.Debug.Assert(boss != null);

            m_virtualCamera.Follow = boss.transform;
            m_virtualCamera.LookAt = boss.transform;
            m_virtualCamera.gameObject.SetActive(true);

            List<GameObject> runners = GameplayPlayerManager.Instance.Runners;

            foreach (GameObject g in runners)
            {
                RunnerCharacterController tempController = g.GetComponent<RunnerCharacterController>();
                tempController.SetLimitScreenRight(true);
                tempController.SetLimitScreenLeft(true);
                tempController.SetCheckOutScreen(true);
            }

            m_checkPointManager.gameObject.SetActive(true);
        }

        public override void EndPhase()
        {
            EndPhaseCommon();
        }

        void EndPhaseCommon()
        {
            m_virtualCamera.Follow = null;
            m_virtualCamera.LookAt = null;
            m_virtualCamera.gameObject.SetActive(false);

            List<GameObject> runners = GameplayPlayerManager.Instance.Runners;

            foreach (GameObject g in runners)
            {
                RunnerCharacterController tempController = g.GetComponent<RunnerCharacterController>();
                tempController.SetLimitScreenRight(false);
                tempController.SetLimitScreenLeft(false);
                tempController.SetCheckOutScreen(false);
            }

            m_checkPointManager.gameObject.SetActive(false);
        }

        #endregion

        #region Debug Execution

        public override void DebugStartPhase()
        {
            m_playerSpawner.ActivatePlayers();
            StartPhaseCommon();
        }

        public override void DebugEndPhase()
        {
            GameplayPlayerManager.Instance.DebugClearAllPlayers();
            EndPhaseCommon();
        }

        #endregion
    }
}