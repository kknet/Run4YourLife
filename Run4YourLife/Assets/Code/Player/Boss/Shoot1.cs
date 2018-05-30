﻿using UnityEngine;

using Run4YourLife.GameManagement.AudioManagement;
using Run4YourLife.Utils;

namespace Run4YourLife.Player
{
    [RequireComponent(typeof(Animator))]
    public class Shoot1 : Shoot
    {
        #region Inspector

        [SerializeField]
        private GameObject instance;

        [SerializeField]
        private float bulletSpeed;

        #endregion

        private float timeToShootFromAnim = 0.5f;

        public override void ShootByAnim()
        {
            animator.SetTrigger("Shoot");
            StartCoroutine(AnimationCallbacks.OnStateAtNormalizedTime(animator, "Shoot", timeToShootFromAnim, () => Shoot()));
        }

        void Shoot()
        {
            AudioManager.Instance.PlaySFX(m_shotClip);
            Vector3 cPos = crossHairControl.Position;
            cPos.z = 0;
            Vector3 director = (crossHairControl.Position - shootInitZone.position).normalized;
            Quaternion rot = Quaternion.FromToRotation(Vector3.right,director);
            GameObject tempInstance = Instantiate(instance, shootInitZone.position, instance.transform.rotation * rot);
            tempInstance.GetComponent<Rigidbody>().velocity = director * bulletSpeed * Time.deltaTime;
        }
    }
}
