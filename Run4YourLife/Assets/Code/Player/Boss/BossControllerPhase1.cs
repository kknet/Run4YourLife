﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Run4YourLife.GameManagement.AudioManagement;
using Run4YourLife.Utils;
using Run4YourLife.GameManagement;

namespace Run4YourLife.Player
{
    public class BossControllerPhase1 : BossController
    {

        [SerializeField]
        private GameObject m_bulletPrefab;

        [SerializeField]
        private float m_bulletSpeed;

        [SerializeField]
        private float m_shootBulletNormalizedTime;

        [SerializeField]
        private float m_meleeNormalizedTime;

        [SerializeField]
        private TrembleConfig m_meleeTrembleConfig;

        protected override void ExecuteShoot()
        {
            base.ExecuteShoot();
            m_animator.SetTrigger(BossAnimation.Triggers.Shoot);

            IsHeadLocked = true;

            AudioManager.Instance.PlaySFX(m_shotWarningClip);

            Vector3 direction = (m_crossHairControl.Position - m_shotSpawn.position).normalized;
            Quaternion rotation = Quaternion.FromToRotation(Vector3.right, direction);

            StartCoroutine(AnimationCallbacks.OnStateAtNormalizedTime(m_animator, BossAnimation.StateNames.Shoot, m_shootBulletNormalizedTime, () => ExecuteShootCallback(direction, rotation)));
        }

        private void ExecuteShootCallback(Vector3 direction, Quaternion rotation)
        {
            GameObject bulletInstance = DynamicObjectsManager.Instance.GameObjectPool.GetAndPosition(m_bulletPrefab, m_shotSpawn.position, rotation, true);
            bulletInstance.GetComponent<Rigidbody>().velocity = direction * m_bulletSpeed;

            AudioManager.Instance.PlaySFX(m_shotFireClip);

            IsHeadLocked = false;
        }

        protected override void ExecuteMelee()
        {
            base.ExecuteMelee();
            m_animator.SetTrigger(BossAnimation.Triggers.Mele);
            AudioManager.Instance.PlaySFX(m_meleeClip);
            StartCoroutine(AnimationCallbacks.OnStateAtNormalizedTime(m_animator, BossAnimation.StateNames.Mele, m_meleeNormalizedTime, () => TrembleManager.Instance.Tremble(m_meleeTrembleConfig)));
        }
    }
}
