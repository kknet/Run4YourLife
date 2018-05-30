﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Run4YourLife.InputManagement;
using System;
using UnityEngine.EventSystems;
using Run4YourLife.GameManagement;
using Run4YourLife.GameManagement.AudioManagement;
using Run4YourLife.Utils;

namespace Run4YourLife.Player
{
    [RequireComponent(typeof(RunnerControlScheme))]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(RunnerAttributeController))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(InputController))]
    [RequireComponent(typeof(BumpController))]
    public class RunnerCharacterController : MonoBehaviour
    {
        #region InspectorVariables

        [SerializeField]
        private AudioClip m_dashClip;

        [SerializeField]
        private AudioClip m_jumpClip;

        [SerializeField]
        private AudioClip m_bounceClip;

        [SerializeField]
        private AudioClip m_fartClip;

        [SerializeField]
        private AudioClip m_deathClip;

        [SerializeField]
        private float m_coyoteGroundedTime;

        [SerializeField]
        private float m_impulseHorizontalDrag;

        [SerializeField]
        private float m_baseGravity;

        [SerializeField]
        private float m_hoverGravity;

        [SerializeField]
        private float m_hoverDuration;

        [SerializeField]
        private float m_releaseJumpButtonVelocityReductor;

        [SerializeField]
        private float m_secondJumpHeight;
        
        [SerializeField]
        private float m_endOfJumpGravity;

        [SerializeField]
        private float m_dashCooldown;

        [SerializeField]
        private float m_dashDistance;

        [SerializeField]
        private float m_dashTime;

        [SerializeField]
        private float m_timeToIdle;

        [SerializeField]
        private FXReceiver leftbounceReceiver;

        [SerializeField]
        private FXReceiver rightbounceReceiver;

        [SerializeField]
        private FXReceiver fartReceiver;

        [SerializeField]
        private FXReceiver leftdustReceiver;

        [SerializeField]
        private FXReceiver rightdustReceiver;

        #endregion

        #region References

        private CharacterController m_characterController;
        private RunnerAttributeController m_runnerAttributeController;
        private RunnerControlScheme m_runnerControlScheme;
        private Animator m_animator;
        private InputController m_inputController;
        private BumpController m_bumpController;

        private Transform m_graphics;
        private Transform m_dashTrail;

        private Camera m_mainCamera;

        #endregion

        #region Private Variables

        private WaitForSeconds m_waitForSecondsCoyoteGroundedTime;
        private WaitForSeconds m_waitForSecondsDashCooldown;

        private bool m_isGroundedOrCoyoteGrounded;
        private bool m_isJumping;
        private bool m_isBouncing;
        private bool m_isBeingImpulsed;
        private bool m_isDashing;
        private bool m_isReadyToDash = true;
        private bool m_ceilingCollision;
        private bool m_jumpedWhileFalling;
        private bool m_isFacingRight = true;

        private Vector3 m_velocity;
        private float m_gravity;
        private float m_horizontalDrag;

        private bool m_checkOutOfScreen;

        private float m_idleTimer;

        private readonly float m_baseHorizontalDrag = 2f; // Hard coded because DragToVelocity calculates with a magic number

        #endregion

        #region Attributes
        public Vector3 Velocity
        {
            get
            {
                return m_velocity;
            }
        }

        public Vector3 ExternalVelocity { get; set; }

        public bool IsDashing { get { return m_isDashing; } }

        public bool CheckOutScreen { get { return m_checkOutOfScreen; } set { m_checkOutOfScreen = value; } }
        #endregion

        void Awake()
        {
            m_runnerControlScheme = GetComponent<RunnerControlScheme>();
            m_characterController = GetComponent<CharacterController>();
            m_runnerAttributeController = GetComponent<RunnerAttributeController>();
            m_animator = GetComponent<Animator>();
            m_inputController = GetComponent<InputController>();
            m_bumpController = GetComponent<BumpController>();

            m_graphics = transform.Find("Graphics");
            Debug.Assert(m_graphics != null);

            m_dashTrail = transform.Find("Graphics/DashTrail");
            Debug.Assert(m_dashTrail != null);

            m_mainCamera = CameraManager.Instance.MainCamera;
            Debug.Assert(m_mainCamera != null);

            m_gravity = m_baseGravity;
            m_horizontalDrag = m_baseHorizontalDrag;

            m_waitForSecondsCoyoteGroundedTime = new WaitForSeconds(m_coyoteGroundedTime);
            m_waitForSecondsDashCooldown = new WaitForSeconds(m_dashCooldown);
        }

        private void OnEnable()
        {
            StartCoroutine(CheckCoyoteGroundedCoroutine());
            StartCoroutine(AnimationCallbacks.OnTransitionFromTo(m_animator, "idle", "correr", () => Dust(), true));
            m_runnerControlScheme.Active = true;

            ResetMembers();
        }

        private void ResetMembers()
        {
            m_gravity = m_baseGravity;
            m_horizontalDrag = m_baseHorizontalDrag;
            m_velocity = Vector3.zero;

            m_isGroundedOrCoyoteGrounded = false;
            m_isJumping = false;
            m_isBouncing = false;
            m_isBeingImpulsed = false;
            m_isDashing = false;
            m_isReadyToDash = true;
            m_ceilingCollision = false;
            m_dashTrail.gameObject.SetActive(false);
            m_animator.Rebind();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            m_runnerControlScheme.Active = false;
        }

        private IEnumerator CheckCoyoteGroundedCoroutine()
        {
            while(true)
            {
                if (m_characterController.isGrounded)
                {
                    m_isGroundedOrCoyoteGrounded = true;
                }
                else
                {
                    if(m_isGroundedOrCoyoteGrounded)
                    {
                        yield return m_waitForSecondsCoyoteGroundedTime;
                        m_isGroundedOrCoyoteGrounded = false;
                    }
                }
                yield return null;
            }
        }

        void Update()
        {
            if (!m_isBeingImpulsed && !m_isDashing)
            {
                GravityAndDrag();

                if (m_isGroundedOrCoyoteGrounded && m_inputController.Started(m_runnerControlScheme.Jump))
                {
                    Jump();
                }

                if (m_isReadyToDash && m_inputController.Started(m_runnerControlScheme.Dash))
                {
                    Dash();
                }

                Move();
            }
            m_animator.SetFloat(RunnerAnimation.ySpeed,m_velocity.y);
            m_animator.SetBool(RunnerAnimation.isGrounded, m_characterController.isGrounded);
        }

        private void Dash()
        {
            StartCoroutine(DashCoroutine());
        }

        private IEnumerator DashCoroutine()
        {
            AudioManager.Instance.PlaySFX(m_dashClip);

            m_isDashing = true;
            m_isReadyToDash = false;
            m_animator.SetTrigger(RunnerAnimation.dash);

            m_dashTrail.gameObject.SetActive(true);

            float facingRight = m_isFacingRight ? 1 : -1;
            m_velocity.x = facingRight * m_dashDistance/m_dashTime;
            m_velocity.y = 0.0f;
            float endTime = Time.time + m_dashTime;
            while (Time.time < endTime)
            {
                MoveCharacterContoller(m_velocity * Time.deltaTime);
                yield return null;
            }
            m_velocity.x = 0;   
            m_dashTrail.gameObject.SetActive(false);

            m_isDashing = false;
            yield return m_waitForSecondsDashCooldown;
            m_isReadyToDash = true;
        }

        private void GravityAndDrag()
        {
            Gravity();
            Drag();
        }

        private void Gravity()
        {
            m_velocity.y += m_gravity * Time.deltaTime;
        }

        private void Drag()
        {
            m_velocity.x = m_velocity.x * (1 - m_horizontalDrag * Time.deltaTime);
            if (Mathf.Abs(m_velocity.x) < 1.0f)
            {
                m_velocity.x = 0;
            }
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {       
            if(m_isJumping && RunnerHitItsHead(hit))
            {
                OnRunnerHitItsHead();
            }

            if (!m_isJumping && m_characterController.isGrounded)
            {
                m_velocity.y = 0.0f;
            }
        }

        private bool RunnerHitItsHead(ControllerColliderHit hit)
        {
            Vector3 hitVector = hit.point - transform.position;
            return hitVector.y > 0.3f && hitVector.x == 0;
        }

        void OnRunnerHitItsHead()
        {
            m_ceilingCollision = true;
            m_velocity.y = 0;
        }

        private void Move()
        {
            float horizontal = m_inputController.Value(m_runnerControlScheme.Move);

            Vector3 modifiers = AddModifiersToSpeed();

            Vector3 inputMovement = transform.right * (horizontal * m_runnerAttributeController.GetAttribute(RunnerAttribute.Speed) * Time.deltaTime);
            Vector3 totalMovement = inputMovement + (m_velocity + modifiers) * Time.deltaTime + ExternalVelocity * Time.deltaTime;

            ExternalVelocity = Vector3.zero;

            MoveCharacterContoller(totalMovement);
        }

        private Vector3 AddModifiersToSpeed()
        {
            Vector3 toReturn = new Vector3(0, 0, 0);

            Wind windModifier = GetComponent<Wind>();
            if(windModifier)
            {
                toReturn.x += windModifier.GetWindForce();
            }

            return toReturn;
        }

        private void MoveCharacterContoller(Vector3 movement)
        {
            m_characterController.Move(movement);
            if (m_checkOutOfScreen)
            {
                TrimPlayerPositionHorizontalInsideCameraView();
            }

            m_animator.SetFloat(RunnerAnimation.xSpeed, Mathf.Abs(movement.x));
            LookAtMovingSide();
            UpdateIdleTimer(movement);
        }

        private void TrimPlayerPositionHorizontalInsideCameraView()
        {
            float xScreenLeft = m_mainCamera.ScreenToWorldPoint(new Vector3(0, 0, Math.Abs(m_mainCamera.transform.position.z - transform.position.z))).x;
            float xScreenRight = m_mainCamera.ScreenToWorldPoint(new Vector3(m_mainCamera.pixelWidth, 0, Math.Abs(m_mainCamera.transform.position.z - transform.position.z))).x;
            Vector3 trimmedPosition = transform.position;
            trimmedPosition.x = Mathf.Clamp(trimmedPosition.x, xScreenLeft, xScreenRight);
            transform.position = trimmedPosition;
        }

        private void LookAtMovingSide()
        {
            bool shouldFaceTheOtherWay = (m_isFacingRight && m_runnerControlScheme.Move.Value() < 0) || (!m_isFacingRight && m_runnerControlScheme.Move.Value() > 0);
            if (shouldFaceTheOtherWay)
            {
                m_graphics.Rotate(Vector3.up, 180);
                m_isFacingRight = !m_isFacingRight;
            }
        }

        private void UpdateIdleTimer(Vector3 totalMovement)
        {
            bool isMoving = totalMovement != Vector3.zero;
            m_idleTimer = isMoving ? m_idleTimer + Time.deltaTime : 0.0f;
            m_animator.SetFloat(RunnerAnimation.idleTime, m_idleTimer);
        }

        #region Jump

        private void Jump()
        {
            m_isGroundedOrCoyoteGrounded = false;
            StartCoroutine(JumpCoroutine());
        }

        private float HeightToVelocity(float height)
        {
            return Mathf.Sqrt(height * -2.0f * m_baseGravity);
        }

        private float VelocityToHeight(float velocity)
        {
            return Mathf.Sqrt(velocity / (-2.0f * m_baseGravity));
        }

        private float DragToVelocity(float dragDistance)
        {
            return 2.1129f * dragDistance;
        }

        private IEnumerator JumpCoroutine()
        {
            m_isJumping = true;

            AudioManager.Instance.PlaySFX(m_jumpClip);
            m_animator.SetTrigger(RunnerAnimation.jump);

            //set vertical velocity to the velocity needed to reach maxJumpHeight
            m_velocity.y = HeightToVelocity(m_runnerAttributeController.GetAttribute(RunnerAttribute.JumpHeight));
            yield return StartCoroutine(WaitUntilApexOfJumpOrReleaseButtonOrCeiling(false));

            if (!m_ceilingCollision && m_runnerControlScheme.Jump.Persists())
            {
                yield return StartCoroutine(JumpHover());
            }
            else
            {
                m_ceilingCollision = false;
            }

            m_isJumping = false;

            yield return StartCoroutine(FallFaster(false));

            if(!m_isBouncing && m_jumpedWhileFalling)
            {
                StartCoroutine(SecondJumpCoroutine());
            }
        }

        private IEnumerator SecondJumpCoroutine()
        {
            AudioManager.Instance.PlaySFX(m_fartClip);
            fartReceiver.PlayFx();
            
            float jumpHeight = m_secondJumpHeight;
            if(m_velocity.y > 0)
            {
                jumpHeight += VelocityToHeight(m_velocity.y);
            }
            m_velocity.y = HeightToVelocity(jumpHeight);

            yield return StartCoroutine(WaitUntilApexOfJumpOrReleaseButtonOrCeiling(true));
            
            if (!m_ceilingCollision)
            {
                yield return StartCoroutine(JumpHover());
            }
            else
            {
                m_ceilingCollision = false;
            }

            yield return StartCoroutine(FallFaster(true));
        }

        private IEnumerator JumpHover()
        {
            while(m_velocity.y > 0f)
            {
                yield return null;
                if(m_runnerControlScheme.Jump.Started()) // we want to execute the second jump
                {
                    yield break; // exit prematurely
                }
                m_velocity.y = Mathf.Lerp(m_velocity.y, 0.0f, m_releaseJumpButtonVelocityReductor * Time.deltaTime);
            }

            m_gravity = m_hoverGravity;

            float endTime = Time.time + m_hoverDuration;
            yield return new WaitUntil(() => Time.time >= endTime || m_runnerControlScheme.Jump.Ended() || m_characterController.isGrounded || m_isBouncing);

            m_gravity = m_baseGravity;
        }

        private IEnumerator FallFaster(bool ignoreJump)
        {
            m_gravity += m_endOfJumpGravity;
            yield return new WaitUntil(() => m_characterController.isGrounded || m_isBouncing || (!ignoreJump && m_runnerControlScheme.Jump.Started()));
            m_jumpedWhileFalling = !ignoreJump && m_runnerControlScheme.Jump.Started();
            m_gravity -= m_endOfJumpGravity;
        }

        private IEnumerator WaitUntilApexOfJumpOrReleaseButtonOrCeiling(bool ignoreJump)
        {
            float previousPositionY = transform.position.y;
            yield return null;

            while (m_runnerControlScheme.Jump.Persists() && 
                    previousPositionY < transform.position.y && 
                    !m_ceilingCollision)
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

        public void BouncedOn()
        {
            m_bumpController.Bump();
        }

        public void Bounce(Vector3 bounceForce)
        {
            StartCoroutine(BounceCoroutine(bounceForce));
        }

        IEnumerator BounceCoroutine(Vector3 bounceForce)
        {
            if (m_isFacingRight)
            {
                rightbounceReceiver.PlayFx();
            }
            else
            {
                leftbounceReceiver.PlayFx();
            }
            m_isBouncing = true;
            AudioManager.Instance.PlaySFX(m_bounceClip);
            m_velocity.x = DragToVelocity(bounceForce.x);
            m_velocity.y = HeightToVelocity(bounceForce.y);

            yield return StartCoroutine(WaitUntilApexOfBounceOrJump());

            m_isBouncing = false; // Bounce ends here However more behaviour continues after this


            if(m_runnerControlScheme.Jump.Started())
            {
                StartCoroutine(SecondJumpCoroutine());
            }
            else
            {
                yield return StartCoroutine(FallFaster(false));

                if(!m_isBouncing && m_jumpedWhileFalling) // We can bounce while falling
                {
                    StartCoroutine(SecondJumpCoroutine());
                }
            }
        }

        private IEnumerator WaitUntilApexOfBounceOrJump()
        {
            float previousPositionY = transform.position.y;
            yield return null;

            while (previousPositionY < transform.position.y && !m_runnerControlScheme.Jump.Started())
            {
                previousPositionY = transform.position.y;
                yield return null;
            }
        }

        #endregion

        #region Impulse

        public void Impulse(Vector3 force)
        {
            StartCoroutine(ImpulseCoroutine(force));
        }

        IEnumerator ImpulseCoroutine(Vector3 force)
        {
            m_isBeingImpulsed = true;
            m_horizontalDrag = m_impulseHorizontalDrag;

            m_animator.SetTrigger(RunnerAnimation.push);
            m_animator.SetFloat(RunnerAnimation.pushForce, force.x);

            AddVelocity(force);

            while(m_velocity.x != 0.0f)
            {
                yield return null;
                GravityAndDrag();
                MoveCharacterContoller(m_velocity * Time.deltaTime);
            }

            float endOfMovement = Time.time + 0.5f;
            while (Time.time < endOfMovement)
            {
                yield return null;
                GravityAndDrag();
                MoveCharacterContoller(m_velocity * Time.deltaTime);
            }

            m_horizontalDrag = m_baseHorizontalDrag;
            m_isBeingImpulsed = false;
        }

        #endregion

        #region DustParticlesManagement

        void Dust()
        {            
            if (m_isFacingRight)
            {
                rightdustReceiver.PlayFx();
            }
            else
            {
                leftdustReceiver.PlayFx();
            }
        }

        #endregion
    }
}
