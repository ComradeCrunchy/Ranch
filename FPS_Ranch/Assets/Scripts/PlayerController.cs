using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
[RequireComponent(typeof(InputHandler), typeof(PlayerAudio), 
    typeof(PlayerCamera))]
[RequireComponent(typeof(PlayerAudio))]
public class PlayerController : MonoBehaviour
{
    [Header("References")] 
    private PlayerCamera _camera;
    private PlayerAudio _audio;
    private PlayerCollisions _collisions;
    private CharacterController _controller;
    private InputHandler _inputHandler;
    private Actor _actor;
    

    [Header("Movement")] 
    public float maxSpeedOnGround = 10f;
    public float movementSharpnessOnGround = 10f;
    [Range(0, 1)] 
    public float maxSpeedCrouchedRatio = 0.5f;
    public float maxSpeedInAir = 10f;
    public float accelerationSpeedInAir = 25f;
    public float sprintSpeedModifier = 2f;
    public float jumpForce = 10f;

    
    private float rotationSpeed = 1f;
    private float rotationMultiplier = 1f;
    private float m_CameraVerticalAngle;
    public float capsuleHeightCrouching = 1.8f;
    public float _targetCharacterHeight = 2f;
    public float capsuleHeightStanding;
    public float crouchingSharpness;
    [Header("Fall damage variables")]
    public float minSpeedForFallDamage = 10f;
    public float maxSpeedForFallDamage = 30f;
    public float fallDamageAtMinSpeed = 10f;
    public float fallDamagAtMaxSpeed = 50f;
    public bool recievesFallDamage;

    public UnityAction<bool> onStanceChanged;
    private bool isCrouching;
    private Vector3 characterVelocity;
    private float _lastTimeJumped;
    private bool hasJumpedThisFrame;
    private float _footstepDistancCounter;
    public float gravityDownForce;
    private Vector3 _latestImpactSpeed;
    private float camerHeightRatio;
    
    


    private void Start()
    {
        _inputHandler = GetComponent<InputHandler>();
        _controller = GetComponent<CharacterController>();
        _collisions = GetComponent<PlayerCollisions>();
        _audio = GetComponent<PlayerAudio>();
        _camera = GetComponent<PlayerCamera>();
        _actor = GetComponent<Actor>();
    }

    private void Update()
    {
        bool wasGrounded = _collisions.isGrounded;
        _collisions.Grounding();
        if (_collisions.isGrounded && !wasGrounded)
        {
            float fallSpeed = -Mathf.Min(characterVelocity.y, _latestImpactSpeed.y);
            float fallSpeedRatio =
                (fallSpeed - minSpeedForFallDamage) / (maxSpeedForFallDamage - minSpeedForFallDamage);
            if (recievesFallDamage && fallSpeedRatio > 0f)
            {
                float dmgfromfall = Mathf.Lerp(fallDamageAtMinSpeed, fallDamagAtMaxSpeed, fallSpeedRatio);
                _audio.FallDmg();
            }
            else
            {
                _audio.Land();
            }
        }
        if (_inputHandler.GetCrouchedInputDown())
        {
            SetCrouchingState(!isCrouching, false);
        }
        UpdateCharacterHeight(false);
        HandleCharacterMovement();
    }

    private void UpdateCharacterHeight(bool force)
    {
        if (force)
        {
            _controller.height = _targetCharacterHeight;
            _controller.center = Vector3.up * (_controller.height * 0.5f);
            _camera.transform.localPosition = Vector3.up * (_targetCharacterHeight * camerHeightRatio);
            _actor.aimPoint.transform.localPosition = _controller.center;
        }
        else if (_controller.height != _targetCharacterHeight)
        {
            _controller.height = Mathf.Lerp(_controller.height, _targetCharacterHeight,
                crouchingSharpness * Time.deltaTime);
            _controller.center = Vector3.up * (_controller.height * 0.5f);
            _camera.transform.localPosition = Vector3.Lerp(_camera.transform.localPosition, 
                Vector3.up * (_targetCharacterHeight * camerHeightRatio), crouchingSharpness * Time.deltaTime);
            _actor.aimPoint.transform.localPosition = _controller.center;
        }
    }

    private void HandleCharacterMovement()
    {
        transform.Rotate(new Vector3(0f,(_inputHandler.GetLookInputsHorizontal() * rotationSpeed 
            * rotationMultiplier) , 0f), Space.Self);
        m_CameraVerticalAngle += _inputHandler.GetLookInputsHorizontal() * rotationSpeed * rotationMultiplier;
        m_CameraVerticalAngle = Mathf.Clamp(m_CameraVerticalAngle, -89f, 89f);
        _camera.transform.localEulerAngles = new Vector3(m_CameraVerticalAngle, 0, 0);

        bool isSprinting = _inputHandler.GetSprintInputHeld();
        
            if (isSprinting)
            {
                isSprinting = SetCrouchingState(false, false);
            }

            float speedModifier = isSprinting ? sprintSpeedModifier : 1f;

            Vector3 worldSpaceMoveInput = transform.TransformVector(_inputHandler.GetMoveInput());
            if (_collisions.isGrounded)
            {
                Vector3 targetVelocity = worldSpaceMoveInput * (maxSpeedOnGround * speedModifier);
                if (isCrouching)
                    targetVelocity *= maxSpeedCrouchedRatio;
                targetVelocity = GetDirectionReorientedOnSlope(targetVelocity.normalized, _collisions.m_GroundNormal)
                                 * targetVelocity.magnitude;
                characterVelocity = Vector3.Lerp(characterVelocity, targetVelocity,
                    movementSharpnessOnGround * Time.deltaTime);
                if (_collisions.isGrounded && _inputHandler.GetJumpInputDown())
                {
                    if (SetCrouchingState(false, false))
                    {
                        characterVelocity = new Vector3(characterVelocity.x, 0f, characterVelocity.z);

                        characterVelocity += Vector3.up * jumpForce;

                        _audio.JumpSFX();

                        _lastTimeJumped = Time.time;
                        hasJumpedThisFrame = true;

                        _collisions.isGrounded = false;
                        _collisions.m_GroundNormal = Vector3.up;
                    }
                }
                float chosenFootStepSFXFrequency = (isSprinting ? _audio.sFXFrequencySprint : _audio.footstepSFXFrequency);
                if (_footstepDistancCounter >= 1f / chosenFootStepSFXFrequency)
                {
                    _footstepDistancCounter = 0f;
                    _audio.StepSFX();
                }

                _footstepDistancCounter += characterVelocity.magnitude * Time.deltaTime;
            }
            else
            {
                characterVelocity += worldSpaceMoveInput * (accelerationSpeedInAir * Time.deltaTime);

                float verticalVelocity = characterVelocity.y;
                Vector3 horizontalVelocity = Vector3.ProjectOnPlane(characterVelocity, Vector3.up);
                horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, maxSpeedInAir * speedModifier);
                characterVelocity = horizontalVelocity + (Vector3.up * verticalVelocity);

                characterVelocity += Vector3.down * (gravityDownForce * Time.deltaTime);
            }
        
        Vector3 capsuleBottomBeforeMove = _collisions.GetCapsuleBottomHemisphere();
        Vector3 capsuleTopBeforeMove = _collisions.GetCapsuleTopHemisphere(_controller.height);
        _controller.Move(characterVelocity * Time.deltaTime);
        _latestImpactSpeed = Vector3.zero;
        if (Physics.CapsuleCast(capsuleBottomBeforeMove, capsuleTopBeforeMove, _controller.radius,
            characterVelocity.normalized, out RaycastHit hit,
            characterVelocity.magnitude * Time.deltaTime, _collisions.ground, QueryTriggerInteraction.Ignore))
        {
            _latestImpactSpeed = characterVelocity;
            characterVelocity = Vector3.ProjectOnPlane(characterVelocity, hit.normal);
        }
    }

    public Vector3 GetDirectionReorientedOnSlope(Vector3 direction, Vector3 slopeNormal)
    {
        Vector3 directionRight = Vector3.Cross(direction, transform.up);
        return Vector3.Cross(slopeNormal, directionRight).normalized;
    }

    private bool SetCrouchingState(bool crouched, bool ignoreObstructions)
    {
        if (crouched)
        {
            _targetCharacterHeight = capsuleHeightCrouching;
        }
        else
        {
            if (!ignoreObstructions)
            {
                Collider[] standingOverlaps = Physics.OverlapCapsule(_collisions.GetCapsuleBottomHemisphere(), 
                    _collisions.GetCapsuleTopHemisphere(capsuleHeightStanding), _collisions.checkRadius,
                    _collisions.ground, QueryTriggerInteraction.Ignore);
                foreach (Collider c in standingOverlaps)
                {
                    if (c != _controller)
                    {
                        return false;
                    }
                }
            }

            _targetCharacterHeight = capsuleHeightStanding;
        }

        if (onStanceChanged != null)
        {
            onStanceChanged.Invoke(crouched);
        }

        isCrouching = crouched;
        return true;
    }
}
