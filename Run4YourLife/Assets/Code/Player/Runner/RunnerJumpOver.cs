﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Run4YourLife.Player
{
    public class RunnerJumpOver : RunnerBounceDetection {

        private RunnerCharacterController m_runnerCharacterController;

        private void Awake()
        {
            m_runnerCharacterController = transform.parent.GetComponent<RunnerCharacterController>();
            UnityEngine.Debug.Assert(m_runnerCharacterController, "Objects needs a parent that has a player character controller");
            Physics.IgnoreCollision(GetComponent<Collider>(), m_runnerCharacterController.GetComponent<Collider>());
        }

        #region JumpOver
        protected override void JumpedOn()
        {
            m_runnerCharacterController.BounceOnMe();
        }

        protected override Vector3 GetBounceForce()
        {
            RunnerAttributeController stats = GetComponentInParent<RunnerAttributeController>();
            float height = stats.GetAttribute(RunnerAttribute.BounceHeight);
            return Vector3.up * height;
        }
        #endregion
    }
}
