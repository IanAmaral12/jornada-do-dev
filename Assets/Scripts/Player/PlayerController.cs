using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] float moveSpeed = 6f;
    [SerializeField] float jumpSpeed = 12f;
    [SerializeField] Transform groundCheck;
    [SerializeField] float groundCheckRadius = 0.1f;
    [SerializeField] LayerMask groundLayer;

    Rigidbody2D rb2d;
    PlayerControls inputActions;
    Vector2 moveInput;
    SpriteRenderer spriteRenderer;
    Animator animator;
    bool isGrounded;
    bool jumpRequested;

    public bool InputLocked { get; set; }

    void Awake()
    {
        rb2d = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        inputActions = new PlayerControls();
    }

    void OnEnable()
    {
        inputActions.Movement.Enable();
        inputActions.Movement.Move.performed += OnMove;
        inputActions.Movement.Move.canceled += OnMoveCanceled;
        inputActions.Movement.Jump.performed += OnJump;
    }

    void OnDisable()
    {
        inputActions.Movement.Move.performed -= OnMove;
        inputActions.Movement.Move.canceled -= OnMoveCanceled;
        inputActions.Movement.Jump.performed -= OnJump;
        inputActions.Movement.Disable();
    }

    void OnDestroy() => inputActions.Dispose();

    void OnMove(InputAction.CallbackContext context) => moveInput = context.ReadValue<Vector2>();

    void OnMoveCanceled(InputAction.CallbackContext context) => moveInput = Vector2.zero;

    void OnJump(InputAction.CallbackContext context)
    {
        if (isGrounded && !InputLocked)
            jumpRequested = true;
    }

    void Update()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        float velocidadeX = InputLocked ? 0f : Mathf.Abs(moveInput.x);
        animator.SetFloat("Speed", velocidadeX);
        animator.SetFloat("VerticalVelocity", rb2d.linearVelocity.y);
        animator.SetBool("isGrounded", isGrounded);
    }

    void FixedUpdate()
    {
        float x = InputLocked ? 0f : moveInput.x;
        rb2d.linearVelocity = new Vector2(x * moveSpeed, rb2d.linearVelocity.y);

        if (x > 0.01f) spriteRenderer.flipX = false;
        else if (x < -0.01f) spriteRenderer.flipX = true;

        if (jumpRequested)
        {
            rb2d.AddForce(Vector2.up * jumpSpeed, ForceMode2D.Impulse);
            jumpRequested = false;
        }

        // queda mais rápida que a subida deixa o pulo menos "flutuante"
        rb2d.gravityScale = rb2d.linearVelocity.y < 0f ? 6f : 2f;
    }

    public void FaceTowards(float x) => spriteRenderer.flipX = x < transform.position.x;
}
