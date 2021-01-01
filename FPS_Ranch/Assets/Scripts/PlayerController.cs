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
    public PlayerCamera _camera;
    private PlayerAudio _audio;
    private PlayerCollisions _collisions;
    private CharacterController _controller;
    

    [Header("Movement")] 
    public float maxSpeedOnGround = 10f;
    public float movementSharpnessOnGround = 10f;
    [Range(0, 1)] 
    public float maxSpeedCrouchedRatio = 0.5f;
    public float maxSpeedInAir = 10f;
    public float accelerationSpeedInAir = 25f;
    public float sprintSpeedModifier = 2f;

    public InputHandler _inputHandler;
    private float rotationSpeed = 1f;
    private float rotationMultiplier = 1f;
    private float m_CameraVerticalAngle;
    public float capsuleHeightCrouching = 1.8f;
    private float _targetCharacterHeight;
    private float capsuleHeightStanding;

    public UnityAction<bool> onStanceChanged;
    private bool isCrouching;

    private void HandleCharacterMovement()
    {
        transform.Rotate(new Vector3(0f,(_inputHandler.GetLookInputsHorizontal() * rotationSpeed 
            * rotationMultiplier) , 0f), Space.Self);
        m_CameraVerticalAngle += _inputHandler.GetLookInputsHorizontal() * rotationSpeed * rotationMultiplier;
        m_CameraVerticalAngle = Mathf.Clamp(m_CameraVerticalAngle, -89f, 89f);
        _camera.transform.localEulerAngles = new Vector3(m_CameraVerticalAngle, 0, 0);

        bool isSprinting = _inputHandler.GetSprintInputHeld();
        {
            if (isSprinting)
            {
                isSprinting = SetCrouchingState(false, false);
            }

            float speedModifier = isSprinting ? sprintSpeedModifier : 1f;

            Vector3 worldSpaceMoveInput = transform.TransformVector(_inputHandler.GetMoveInput());
            if (_collisions.isGrounded)
            {
                
            }
        }
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
