using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public CharacterController player;
  
    private float playerSpeed = 5f;
    private Vector3 playerVelocity;
    private float jumpHeight = 1.5f;
    private float gravityValue = -9.8f;
    private bool isGrounded;

    public InputActionReference moveAction;
    public InputActionReference jumpAction;


    void Update()
    {
        isGrounded = player.isGrounded;

        if (isGrounded)
        {
            if(playerVelocity.y < -2f)
                playerVelocity.y = -2f;
        }
        Vector2 input = moveAction.action.ReadValue<Vector2>();
        Vector3 move = new Vector3(input.x , 0 , input.y);
        move = Vector3.ClampMagnitude(move, 1f);

        if (isGrounded && jumpAction.action.WasPressedThisFrame())
        {
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravityValue);
        }

        playerVelocity.y += gravityValue * Time.deltaTime;

        Vector3 finalMove = move * playerSpeed + Vector3.up * playerVelocity.y;
        player.Move(finalMove * Time.deltaTime);
    }

}
