using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputHandler : MonoBehaviour
{
    private GameManager _gameManager;
    public bool invertYaxis;
    private float lookSensitivity = 2f;

    public float GetLookInputsHorizontal()
    {
        return GetMouseOrStickLookAxis(GameConstants.k_MouseAxisNameHorizontal,
            GameConstants.k_AxisNameJoystickLookHorizontal);
    }

    private float GetMouseOrStickLookAxis(string mousInputName, string stickInputName)
    {
        if (CanProcessInput())
        {
            bool isGamePad = Input.GetAxis(stickInputName) != 0f;
            float i = isGamePad ? Input.GetAxis(stickInputName) : Input.GetAxisRaw(mousInputName);

            if (invertYaxis)
                i *= -1f;
            i *= lookSensitivity;
            if (isGamePad)
            {
                i *= Time.deltaTime;
            }
            else
            {
                i *= 0.01f;
            }

            return i;
        }

        return 0f;
    }

    private bool CanProcessInput()
    {
        return Cursor.lockState == CursorLockMode.Locked && !_gameManager.gameIsEnding;
    }

    public bool GetSprintInputHeld()
    {
        if (CanProcessInput())
        {
            return Input.GetButton(GameConstants.k_ButtonNameSprint);
        }

        return false;
    }

    public Vector3 GetMoveInput()
    {
        if (CanProcessInput())
        {
            Vector3 move = new Vector3(Input.GetAxisRaw(GameConstants.k_MouseAxisNameHorizontal), 0f,
                Input.GetAxisRaw(GameConstants.k_AxisNameVertical));
            move = Vector3.ClampMagnitude(move, 1);

            return move;
        }
        return Vector3.zero;
    }
}
