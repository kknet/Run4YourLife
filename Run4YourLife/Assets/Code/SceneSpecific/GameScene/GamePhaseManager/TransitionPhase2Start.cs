﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using Cinemachine;

namespace Run4YourLife.GameManagement
{
    public class TransitionPhase2Start : TransitionBase
    {
        public override GamePhase GamePhase { get { return GamePhase.TransitionPhase2Start; } }

        [SerializeField]
        private PlayableDirector m_RunnersCutscene;

        [SerializeField]
        private PlayableDirector m_BossCutscene;

        [SerializeField]
        private CinemachineVirtualCamera m_virtualCamera;

        [SerializeField]
        private Transform portalTransform;

        [SerializeField]
        private Vector3 offsetFromPortal = new Vector3 (0,-0.5f,0);

        private PlayerSpawner m_playerSpawner;

        private void Awake()
        {
            m_playerSpawner = GetComponent<PlayerSpawner>();
            Debug.Assert(m_playerSpawner != null);
        }

        public override void StartPhase()
        {
            GameplayPlayerManager.Instance.ReviveAllRunners();
            StartCoroutine(StartPhaseCoroutine());
        }

        private IEnumerator StartPhaseCoroutine()
        {
            CameraManager.Instance.TransitionToCamera(m_virtualCamera);

            List<GameObject> runners = GameplayPlayerManager.Instance.RunnersAlive;
            foreach (GameObject runner in runners)
            {
                runner.transform.position = portalTransform.position + offsetFromPortal;
                runner.SetActive(false);
                DeactivateScripts(runner);
            }
            GameObject boss = m_playerSpawner.ActivateBoss();
            DeactivateScripts(boss);

            //Runners intro
            BindTimelineTracks(m_RunnersCutscene, runners, boss);
            m_RunnersCutscene.Play();
            yield return new WaitUntil(() => m_RunnersCutscene.state != PlayState.Playing); // wait until cutscene has completed
            Unbind(m_RunnersCutscene);
            foreach (GameObject runner in runners)
            {
                ActivateScripts(runner);
            }
            //Boss intro
            BindTimelineTracks(m_BossCutscene,runners,boss);
            m_BossCutscene.Play();
            yield return new WaitUntil(() => m_BossCutscene.state != PlayState.Playing); // wait until cutscene has completed
            Unbind(m_BossCutscene);
            ActivateScripts(boss);
            GameManager.Instance.ChangeGamePhase(GamePhase.TransitionPhase2End);
        }

        public override void EndPhase()
        {

        }

        public override void DebugEndPhase()
        {
            Debug.LogError("This method should never be called");
        }

        public override void DebugStartPhase()
        {
            GameplayPlayerManager.Instance.ReviveAllRunners();
            StartCoroutine(StartPhaseCoroutine());
        }
    }
}
