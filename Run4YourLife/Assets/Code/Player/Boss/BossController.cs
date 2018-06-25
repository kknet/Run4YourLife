﻿using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;


using Run4YourLife.Utils;
using Run4YourLife.InputManagement;
using Run4YourLife.UI;
using Run4YourLife.GameManagement.AudioManagement;

namespace Run4YourLife.Player
{

    [RequireComponent(typeof(BossControlScheme))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(CrossHairControl))]
    public class BossController : MonoBehaviour {

        #region Inspector
        [SerializeField]
        private float m_shootCooldown;

        

        [SerializeField]
        private float m_normalizedTimeToSpawnTrap = 0.2f;

        [SerializeField]
        private SkillBase m_lightningSkill;
        
        [SerializeField]
        private SkillBase m_earthSpikeSkill;
        
        [SerializeField]
        private SkillBase m_windSkill;
        
        [SerializeField]
        private SkillBase m_bombSkill;

        [SerializeField]
        [Range(-90, 90)]
        [Tooltip("Offset from wich the head will look, used to shoot with the mouth instead of the beak")]
        private float m_headLookAtOffset;

        [SerializeField]
        private Transform m_headBone;

        [SerializeField]
        protected Transform m_shotSpawn;

        [SerializeField]
        [Tooltip("Audio clip that playes when the boss uses a skill")]
        private AudioClip m_castClip;

        [SerializeField]
        protected AudioClip m_shotClip;

        [SerializeField]
        private GameObjectPool m_gameObjectPool;



        #endregion

        private float m_earthSpikeReadyTime;
        private float m_windReadyTime;
        private float m_bombReadyTime;
        private float m_lightningReadyTime;
        private float m_shootReadyTime;
        protected Quaternion m_initialHeadRotation;



        private BossControlScheme m_controlScheme;
        private Animator m_animator;
        private GameObject m_ui;
        private CrossHairControl m_crossHairControl;

        private void Awake()
        {
            m_controlScheme = GetComponent<BossControlScheme>();
            m_animator = GetComponent<Animator>();
            m_crossHairControl = GetComponent<CrossHairControl>();
            m_ui = GameObject.FindGameObjectWithTag(Tags.UI);
            Debug.Assert(m_ui != null);

            m_initialHeadRotation = m_headBone.rotation; // We have to store the starting position to in order to rotate it properly
        }

        void Update() {
            if(IsReadyToAttack())
            {
                if (m_controlScheme.Lightning.Started() && (m_lightningReadyTime <= Time.time) && m_lightningSkill.CanBePlacedAtPosition(m_crossHairControl.Position))
                {
                    m_lightningReadyTime = Time.time + m_lightningSkill.Cooldown;
                    ExecuteSkill(m_lightningSkill, ActionType.A);             
                }
                else if (m_controlScheme.EarthSpike.Started() && (m_earthSpikeReadyTime <= Time.time) && m_earthSpikeSkill.CanBePlacedAtPosition(m_crossHairControl.Position))
                {
                    m_earthSpikeReadyTime = Time.time + m_earthSpikeSkill.Cooldown;
                    ExecuteSkill(m_earthSpikeSkill, ActionType.X);
                }
                else if (m_controlScheme.Wind.Started() && (m_windReadyTime <= Time.time) && m_windSkill.CanBePlacedAtPosition(m_crossHairControl.Position))
                {
                    m_windReadyTime = Time.time + m_windSkill.Cooldown;
                    ExecuteSkill(m_windSkill, ActionType.Y);
                }
                else if (m_controlScheme.Bomb.Started() && (m_bombReadyTime <= Time.time) && m_bombSkill.CanBePlacedAtPosition(m_crossHairControl.Position))
                {
                    m_bombReadyTime = Time.time + m_bombSkill.Cooldown;
                    ExecuteSkill(m_bombSkill, ActionType.B);
                } 
                else if(m_controlScheme.Shoot.BoolValue() && m_shootReadyTime <= Time.time)
                {
                    m_shootCooldown = Time.time + m_shootCooldown;
                    ExecuteShoot();
                }
            }
        }

        private void LateUpdate()
        {
            RotateHead();
        }
        
        private bool IsReadyToAttack()
        {
            return AnimatorQuery.IsInStateCompletely(m_animator, BossAnimation.StateNames.Move);
        }

        private void ExecuteSkill(SkillBase skill, ActionType type)
        {
            GameObject instance = m_gameObjectPool.GetAndPosition(skill.gameObject, m_crossHairControl.Position, Quaternion.identity);          
            m_animator.SetTrigger(BossAnimation.Triggers.Cast);
            StartCoroutine(AnimationCallbacks.AfterStateAtNormalizedTime(m_animator, BossAnimation.StateNames.Move, m_normalizedTimeToSpawnTrap, () => PlaceSkillAtAnimationCallback(instance)));
            AudioManager.Instance.PlaySFX(m_castClip);
            ExecuteEvents.Execute<IUIEvents>(m_ui, null, (x, y) => x.OnActionUsed(type, skill.Cooldown));
        }

        private void PlaceSkillAtAnimationCallback(GameObject instance)
        {
            instance.SetActive(true);
            instance.GetComponent<SkillBase>().StartSkill();
        }


        protected virtual void ExecuteShoot()
        {
            ExecuteEvents.Execute<IUIEvents>(m_ui, null, (x, y) => x.OnActionUsed(ActionType.SHOOT, m_shootCooldown));
        }

        protected virtual void RotateHead()
        {
            m_headBone.LookAt(m_crossHairControl.Position);
            m_headBone.Rotate(0, -90, m_headLookAtOffset);
            m_headBone.rotation *= m_initialHeadRotation;
        }
    }
}
