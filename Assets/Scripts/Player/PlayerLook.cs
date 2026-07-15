using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : MonoBehaviour
{
    public Transform cameraPivot;
    public float mouseSensitivity = 100f;
    private bool canLook = true;
    private float xRotation = 0f;
    private float recoilPitchOffset, recoilYawOffset, recoilRecoverySpeed;


    void Start()
    {
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;
    }

    void Update()
    {
        if (!canLook)
        {
            return;
        }

        if (Keyboard.current.escapeKey.isPressed)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // recoilPitchOffset = Mathf.MoveTowards(recoilPitchOffset, 0f, recoilRecoverySpeed * Time.deltaTime);
        // recoilYawOffset = Mathf.MoveTowards(recoilYawOffset, 0f, recoilRecoverySpeed * Time.deltaTime);
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        float mouseX = mouseDelta.x * mouseSensitivity * Time.deltaTime;
        float mouseY = mouseDelta.y * mouseSensitivity * Time.deltaTime;

        // Rotate player (Y axis)
        transform.Rotate(Vector3.up * mouseX);

        // Rotate camera (X axis)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        var finalPitch = xRotation - recoilPitchOffset;
        cameraPivot.localRotation = Quaternion.Euler(finalPitch, recoilYawOffset, 0f);
    }

    public void DisableLook()
    {
        canLook = false;
    }

    public void ApplyRecoil(float vertical, float horizontal)
    {
        float verticalKick = Random.Range(vertical * 0.9f, vertical * 1.1f);
        float horizontalKick = Random.Range(-horizontal, horizontal);

        float maxPitch = 80f;
        // float minPitch = -80f;

        float currentFinalPitch = xRotation - recoilPitchOffset;

        float allowedKick = currentFinalPitch + verticalKick > maxPitch
            ? maxPitch - currentFinalPitch
            : verticalKick;

        recoilPitchOffset += allowedKick;

        recoilYawOffset = Mathf.Clamp(
            recoilYawOffset + horizontalKick,
            -2f,
            2f
        );
    }
    public void SetRecoverySpeed(float recoilRecoverySpeed)
    {
        this.recoilRecoverySpeed = recoilRecoverySpeed;
    }

    public void SetCanLook(bool value)
    {
        canLook = value;

        Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !value;
    }
}
