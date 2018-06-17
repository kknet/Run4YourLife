﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Run4YourLife.Player;
using Run4YourLife.GameManagement;

namespace Run4YourLife.Debugging
{
    public class PlayerInvencible : DebugFeature
    {
        private bool m_isInvincibleActive;

        private string m_invincibleActiveText = "Runners Invincible";


        protected override string GetPanelName()
        {
            return "Runner revive mode";
        }


        protected override void OnCustomDrawGUI()
        {
            m_isInvincibleActive = GUILayout.Toggle(m_isInvincibleActive, m_invincibleActiveText);
                        
            foreach (GameObject runner in GameplayPlayerManager.Instance.Runners)
            {
                if(m_isInvincibleActive)
                {
                    Activate(runner);
                }
                else
                {
                    Deactivate(runner);
                }
            }
        }

        //Dictionary<GameObject, bool> toggleValues = new Dictionary<GameObject, bool>();

        /*protected override void OnCustomDrawGUI()
        {
            GUILayout.BeginHorizontal();

            List<GameObject> runners = GameplayPlayerManager.Instance.Runners;
            
            for (int i = 0; i < runners.Count; ++i)
            {
                GameObject runner = runners[i];

                PlayerHandle runnerHandle = runner.GetComponent<PlayerInstance>().PlayerHandle;

                bool value;
                toggleValues.TryGetValue(runner,out value);
                toggleValues[runner] = GUILayout.Toggle(value, "Runner " + runnerHandle.ID);
                if (toggleValues[runner])
                {
                    Activate(runner);
                }
                else
                {
                    Deactivate(runner);
                }
            }

            GUILayout.EndHorizontal();
        }*/

        private void Activate(GameObject runner)
        {
            runner.GetComponent<RunnerController>().SetReviveMode(true);
            if (!runner.activeInHierarchy)
            {
                GameplayPlayerManager.Instance.ReviveRunner(runner.GetComponent<PlayerInstance>().PlayerHandle, GetRandomSpawnPosition());
            }
        }

        private void Deactivate(GameObject runner)
        {
            runner.GetComponent<RunnerController>().SetReviveMode(false);
        }

        private Vector3 GetRandomSpawnPosition()
        {
            Vector3 position = CameraManager.Instance.MainCamera.transform.position;

            position.x += Random.Range(-3, 6);
            position.y += Random.Range(-2, 5);
            position.z = 0;

            return position;
        }
    }
}
