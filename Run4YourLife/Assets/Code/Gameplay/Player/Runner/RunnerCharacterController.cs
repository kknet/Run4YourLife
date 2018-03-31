﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Run4YourLife.Input;
using System;
using UnityEngine.EventSystems;
using Run4YourLife.GameManagement;

namespace Run4YourLife.Player
{
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(RunnerControlScheme))]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Stats))]
    [RequireComponent(typeof(Animator))]
    public class RunnerCharacterController : MonoBehaviour
    {
        #region InspectorVariables

        [SerializeField]
        private GameObject graphics;

        [SerializeField]
        private float m_baseGravity;

        [SerializeField]
        private float m_endOfJumpGravity;

        [SerializeField]
        private float m_gravityPushMuliplier;

        [SerializeField]
        private float m_timeToIdle;

        [SerializeField]
        private float m_pushReduction;

        [SerializeField]
        private float m_minPushMagnitude;

        #endregion

        #region Public Variable

        public AudioClip jumpClip;
        public AudioClip bounceClip;

        #endregion

        #region References

        private CharacterController m_characterController;
        private Stats m_stats;
        private RunnerControlScheme m_playerControlScheme;
        private Animator m_animator;
        private Animation m_currentAnimation;
        private AudioSource m_audioSource;

        #endregion

        #region Private Variables

        private bool m_isJumping;
        private bool m_isBouncing;
        private bool m_isBeingImpulsed;
        private bool m_ceilingCollision = false;

        private bool m_isFacingRight = true;

        private Vector3 m_velocity;
        private float m_gravity;

        private float m_burnedHorizontal = 1.0f;

        private float m_idleTimer = 0.0f;

        #endregion

        #region Attributes
        public Vector3 Velocity
        {
            get
            {
                return m_velocity;
            }
        }
        #endregion

        void Awake()
        {
            m_audioSource = GetComponent<AudioSource>();
            m_playerControlScheme = GetComponent<RunnerControlScheme>();
            m_characterController = GetComponent<CharacterController>();
            m_stats = GetComponent<Stats>();
            m_animator = GetComponent<Animator>();
            m_gravity = m_baseGravity;
        }

        void Update()
        {
            if (!m_isBeingImpulsed)
            {
                Gravity();

                if (m_characterController.isGrounded && m_playerControlScheme.jump.Started())
                {
                    Jump();
                }

                Move();
            }
            m_animator.SetBool("ground", m_characterController.isGrounded);
        }

        private void Gravity()
        {
            m_velocity.y += m_gravity * Time.deltaTime;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if(m_isJumping && !m_characterController.isGrounded)
            {
                m_ceilingCollision = true;
                m_velocity.y = 0;
            }

            if (!m_isJumping && m_characterController.isGrounded)
            {
                m_velocity.y = 0.0f;
            }
        }

        private void Move()
        {
            // float horizontal = CheckStatModificators(m_playerControlScheme.move.Value());

            float horizontal = m_playerControlScheme.move.Value();

            IRunnerInput[] iRunnerInputList = GetComponents<IRunnerInput>();

            Array.Sort(iRunnerInputList, (x, y) => x.GetPriority().CompareTo(y.GetPriority()));

            foreach(IRunnerInput iRunnerInput in iRunnerInputList)
            {
                iRunnerInput.ModifyHorizontalInput(ref horizontal);
            }

            Vector3 inputMovement = transform.forward * horizontal * m_stats.Get(StatType.SPEED) * Time.deltaTime;
            Vector3 totalMovement = inputMovement + m_velocity * Time.deltaTime;

            MoveCharacterContoller(totalMovement);
        }

        private void MoveCharacterContoller(Vector3 movement)
        {
            m_characterController.Move(movement);
            float xScreenRight = Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth, 0, Math.Abs(Camera.main.transform.position.z - transform.position.z))).x;
            if (transform.position.x > xScreenRight)
            {
                Vector3 tempPos = transform.position;
                tempPos.x = xScreenRight;
                transform.position = tempPos;
            }

            m_animator.SetFloat("xSpeed", Mathf.Abs(movement.x));
            LookAtMovingSide();
            UpdateIdleTimer(movement);
        }

        private void LookAtMovingSide()
        {
            bool shouldFaceTheOtherWay = (m_isFacingRight && m_playerControlScheme.move.Value() < 0) || (!m_isFacingRight && m_playerControlScheme.move.Value() > 0);
            if (shouldFaceTheOtherWay)
            {
                graphics.transform.Rotate(Vector3.up, 180);
                m_isFacingRight = !m_isFacingRight;
            }
        }

        private void UpdateIdleTimer(Vector3 totalMovement)
        {
            bool isMoving = totalMovement != Vector3.zero;
            m_idleTimer = isMoving ? m_idleTimer + Time.deltaTime : 0.0f;
            m_animator.SetFloat("timeToIdle", m_idleTimer);
        }

        #region Jump

        private void Jump()
        {
            StartCoroutine(JumpCoroutine());
        }

        private float HeightToVelocity(float height)
        {
            return Mathf.Sqrt(height * -2f * m_baseGravity);
        }

        private IEnumerator JumpCoroutine()
        {
            m_isJumping = true;

            m_audioSource.PlayOneShot(jumpClip);
            m_animator.SetTrigger("jump");

            //set vertical velocity to the velocity needed to reach maxJumpHeight
            m_velocity.y = HeightToVelocity(m_stats.Get(StatType.JUMP_HEIGHT));
            yield return StartCoroutine(WaitUntilApexOfJumpOrReleaseButton());

            m_ceilingCollision = false;
            m_isJumping = false;

            yield return StartCoroutine(FallFaster());
        }

        private IEnumerator FallFaster()
        {
            m_gravity += m_endOfJumpGravity;
            yield return new WaitUntil(() => m_characterController.isGrounded || m_isBouncing || m_isJumping);
            m_gravity -= m_endOfJumpGravity;
        }

        private IEnumerator WaitUntilApexOfJumpOrReleaseButton()
        {
            float previousPositionY = transform.position.y;
            yield return null;

            while (m_playerControlScheme.jump.Persists() && previousPositionY < transform.position.y && !m_ceilingCollision)
            {                
                previousPositionY = transform.position.y;
                yield return null;
            }
        }

        #endregion

        public void AddVelocity(Vector3 velocity)
        {
            m_velocity += velocity;
        }

        #region Bounce

        public void BounceOnMe()
        {
            m_animator.SetTrigger("bump");
        }

        public void Bounce(float bounceForce)
        {
            StartCoroutine(BounceCoroutine(bounceForce));
        }

        IEnumerator BounceCoroutine(float bounceForce)
        {
            m_isBouncing = true;
            m_audioSource.PlayOneShot(bounceClip);
            m_velocity.y = HeightToVelocity(bounceForce);

            yield return StartCoroutine(WaitUntilApexOfBounce());
            m_isBouncing = false;

            yield return StartCoroutine(FallFaster());
        }

        private IEnumerator WaitUntilApexOfBounce()
        {
            float previousPositionY = transform.position.y;
            yield return null;

            while (previousPositionY < transform.position.y)
            {
                previousPositionY = transform.position.y;
                yield return null;
            }
        }

        #endregion

        #region Impulse

        public void Impulse(Vector3 direction,float force)
        {
            StartCoroutine(ImpulseCoroutine(direction,force));
        }

        IEnumerator ImpulseCoroutine(Vector3 direction,float force)
        {
            m_animator.SetTrigger("push");
            m_animator.SetFloat("pushForce", direction.x);
            m_isBeingImpulsed = true;
            bool isRight = direction.x > 0;
            Vector3 director = Quaternion.Euler(0,0,45) * Vector3.right;
            if (!isRight)
            {
                director.x = -director.x;
            }
            director *= force;
            while(Mathf.Abs(director.x) > m_minPushMagnitude)
            {
                MoveCharacterContoller(director * Time.deltaTime);
                yield return null;
                director.x = Mathf.Lerp(director.x,0,Time.deltaTime/m_pushReduction);
                director.y += m_gravity * Time.deltaTime*m_gravityPushMuliplier;
            }
            
            m_isBeingImpulsed = false;
        }

        #endregion
    }
}
