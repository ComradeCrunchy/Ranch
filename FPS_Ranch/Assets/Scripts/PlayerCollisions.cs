using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(CharacterController))]
public class PlayerCollisions : MonoBehaviour
{
    public bool isGrounded;
    public LayerMask ground;
    public Transform groundCheck;
    public float checkRadius = 0.05f;
    public Vector3 m_GroundNormal;
    private CharacterController m_Controller;
    private float k_GroundCheckDistanceInAir;
    private float m_lastTimeJumped = 0f;

    private const float k_JumpGroundingPreventionTime = 0.2f;

    private void Start()
    {
        m_Controller = GetComponent<CharacterController>();
    }

    public void Grounding()
    {
        float groundcheckRadius = isGrounded ? (m_Controller.skinWidth + checkRadius) : k_GroundCheckDistanceInAir;
        m_GroundNormal = Vector3.up;
        if (Physics.CapsuleCast(GetCapsuleBottomHemisphere(), GetCapsuleTopHemisphere(m_Controller.height),
            m_Controller.radius, Vector3.down, out RaycastHit hit, groundcheckRadius, ground,
            QueryTriggerInteraction.Ignore))
        {
            if (Time.time >= m_lastTimeJumped + k_JumpGroundingPreventionTime)
            {
                if (isGrounded)
                {
                    m_GroundNormal = hit.normal;

                    if (Vector2.Dot(hit.normal, transform.up) > 0f &&
                        IsNormalUnderSlopeLimit(m_GroundNormal))
                    {
                        isGrounded = true;
                        if (hit.distance > m_Controller.skinWidth)
                        {
                            m_Controller.Move(Vector3.down * hit.distance);
                        }
                    }
                }
            }
        }
    }

    public Vector3 GetCapsuleTopHemisphere(float atHeight)
    {
        return transform.position + (transform.up * (atHeight - m_Controller.radius));
    }

    bool IsNormalUnderSlopeLimit(Vector3 normal)
    {
        return Vector3.Angle(transform.up, normal) <= m_Controller.slopeLimit;
    }
    public Vector3 GetCapsuleBottomHemisphere()
    {
        return transform.position + (transform.up * m_Controller.radius);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
    }
}
