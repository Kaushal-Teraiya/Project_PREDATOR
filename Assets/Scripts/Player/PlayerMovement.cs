using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public CharacterController player;
    private SoundEmitter emitter;

    [SerializeField] private float playerWalkSpeed = 5f;
    [SerializeField] private float playerRunSpeed = 10f;
    [SerializeField] private float jumpHeight = 1.5f;
    private float playerSpeed;
    private float soundEmissionRadius;
    private Vector3 playerVelocity; private float gravityValue = -9.8f;
    private bool isGrounded;

    public InputActionReference moveAction;
    public InputActionReference jumpAction;
    public InputActionReference runAction;

    void Start()
    {
        emitter = GetComponent<SoundEmitter>();
    }

    void Update()
    {
        isGrounded = player.isGrounded;

        if (isGrounded)
        {
            if (playerVelocity.y < -2f)
                playerVelocity.y = -2f;
        }
        Vector2 input = moveAction.action.ReadValue<Vector2>();
        Vector3 move = transform.right * input.x + transform.forward * input.y;
        move = Vector3.ClampMagnitude(move, 1f);

        if (isGrounded && jumpAction.action.WasPressedThisFrame())
        {
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravityValue);
        }

        playerVelocity.y += gravityValue * Time.deltaTime;

        if (runAction.action.IsPressed())
        {
            playerSpeed = playerRunSpeed;
            soundEmissionRadius = emitter.runRadius;
        }
        else
        {
            playerSpeed = playerWalkSpeed;
            soundEmissionRadius = 0f;
        }
        Vector3 finalMove = move * playerSpeed + Vector3.up * playerVelocity.y;
        player.Move(finalMove * Time.deltaTime);

        bool isMoving = move.magnitude > 0.1f;

        if (isMoving)
        {
            emitter.EmitSound(soundEmissionRadius);
        }
    }

    public void DisableMovement()
    {
        moveAction.action.Disable();
        jumpAction.action.Disable();
        runAction.action.Disable();
    }

}
