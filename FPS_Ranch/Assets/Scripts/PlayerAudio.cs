using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    private PlayerCharacterController _characterController;
    private AudioSource _audioSource;
    public float footstepSFXFrequency = 1f;
    public float sFXFrequencySprint = 1f;
    public AudioClip footStepSFX;
    public AudioClip jumpSFX;
    public AudioClip landSFX;
    public AudioClip fallDamageSFX;

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    public void StepSFX()
    {
        _audioSource.PlayOneShot(footStepSFX);
    }

    public void FallDmg()
    {
        _audioSource.PlayOneShot(fallDamageSFX);
    }

    public void Land()
    {
        _audioSource.PlayOneShot(landSFX);
    }
    public void JumpSFX()
    {
        _audioSource.PlayOneShot(jumpSFX);
    }

}
