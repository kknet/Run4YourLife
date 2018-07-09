﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Run4YourLife.SceneManagement;
using Run4YourLife.InputManagement;

namespace Run4YourLife.SceneSpecific.SplashScreen
{
    public class SplashScreenManager : MonoBehaviour {

        [SerializeField]
        private SceneTransitionRequest m_mainMenuLoad;

        private InputDevice m_defaultInputDevice;

        private void Start()
        {
            m_defaultInputDevice = InputManagement.InputDeviceManager.Instance.DefaultInputDevice;
        }

        private void Update()
        {
            if(Input.anyKeyDown)
            {
                m_mainMenuLoad.Execute();
            }
        }
    }
}