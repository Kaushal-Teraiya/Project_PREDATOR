using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour, IPlayerMovement
{
    public CharacterController player;
    private SoundEmitter emitter;

    [SerializeField] private float playerWalkSpeed = 5f;
    [SerializeField] private float playerRunSpeed = 10f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float footStepsTime_walk = 0.5f;
    [SerializeField] private float footStepsTime_Run = 0.3f;
    private float stepTimer;
    [SerializeField] private SoundSource SoundSource_Run;
    [SerializeField] private SoundSource SoundSource_Walk;
    public float playerSpeed { get; private set; }
    private SoundSource activeProfile;
    private Vector3 playerVelocity;
    private float gravityValue = -9.8f;
    private bool isGrounded;
    private float stepInterval;

    public InputActionReference moveAction;
    public InputActionReference jumpAction;
    public InputActionReference runAction;
    private Vector3 lastPlayerMovementDirection;

    public bool isPerformingAction { get; private set; }
    [SerializeField]
    private float significantMovementDistance = 2f;

    private Vector3 lastPlayerPosition;
    private Vector3 accumulatedMovement;
    public bool IsPerformingAction => isPerformingAction;
    public Vector3 LastMovementDirection => lastPlayerMovementDirection;
    //private Vector3 lastPlayerMovementDirection;

    void Start()
    {
        emitter = GetComponent<SoundEmitter>();
        lastPlayerPosition = transform.position;
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
        bool isMoving = move.magnitude > 0.1f;

        if (runAction.action.IsPressed())
        {
            playerSpeed = playerRunSpeed;
            activeProfile = SoundSource_Run;
            stepInterval = footStepsTime_Run;
        }
        else
        {
            playerSpeed = playerWalkSpeed;
            activeProfile = SoundSource_Walk;
            stepInterval = footStepsTime_walk;
        }
        Vector3 finalMove = move * playerSpeed + Vector3.up * playerVelocity.y;
        player.Move(finalMove * Time.deltaTime);

        if (isMoving && isGrounded)
        {
            stepTimer += Time.deltaTime;
            if (stepTimer >= stepInterval)
            {
                emitter.EmitSound(activeProfile);
                stepTimer = 0f;
            }
        }

        if (!isMoving)
        {
            stepTimer = 0f;
        }

        SetPerformingAction(isMoving || jumpAction.action.WasPressedThisFrame());
        UpdateReliableMovementDirection();
    }

    private void SetPerformingAction(bool status)
    {
        isPerformingAction = status;
    }

    public void DisableMovement()
    {
        moveAction.action.Disable();
        jumpAction.action.Disable();
        runAction.action.Disable();
    }

    public Vector3 GetPlayerVelocity()
    {
        // Debug.Log($"Velocity: {player.velocity}");
        return player.velocity;
    }

    public Vector3 GetLastPlayerMovementDirection()
    {
        return lastPlayerMovementDirection;
    }

    private void UpdateReliableMovementDirection()
    {
        Vector3 frameMovement = transform.position - lastPlayerPosition;

        accumulatedMovement += frameMovement;

        if (accumulatedMovement.magnitude >= significantMovementDistance)
        {
            lastPlayerMovementDirection = accumulatedMovement.normalized;

            accumulatedMovement = Vector3.zero;
        }

        lastPlayerPosition = transform.position;
    }

}
