using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    private PlayerController _controller;
    public float footstepSFXFrequency = 1f;
    public float sFXFrequencySprint = 1f;
    public AudioClip footStepSFX;
    public AudioClip jumpSFX;
    public AudioClip landSFX;
    public AudioClip fallDamageSFX;

}
