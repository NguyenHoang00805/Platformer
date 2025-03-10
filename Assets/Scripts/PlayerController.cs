using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
[RequireComponent(typeof(Rigidbody2D), typeof(TouchingDirections))]
public class PlayerController : MonoBehaviour
{
    Animator animator;
    Rigidbody2D rb;
    Vector2 moveInput;

    public float airWalkSpeed = 4f;
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    private bool _canDash = true;
    private bool _isDashing;
    private float _timeSinceGrounded;
    private bool _coyoteUsable;
    private bool _canVarJump = true;
    private float dashingCooldown = 1f;

    [SerializeField] private bool _isMoving = false;
    [SerializeField] private bool _isRunning = false;
    [SerializeField] private float coyoteTimer = 0.1f;
    [SerializeField] private float maxFallSpeed = -50f;
    [SerializeField] private float fallAcceleration = 6f;
    [SerializeField] private float jumpImpulse = 10f;
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private float dashingPower = 24f;
    [SerializeField] private float dashingTime = 0.2f;

    public bool _isFacingRight = true;

    public UIManager UIManager;
    public GameOverScreen gameOverScreen;
    TouchingDirections touchingDirections;
    Damageable damageable;
    public PlayerInput playerInput;
    public float currentMoveSpeed
    {
        get
        {
            if (CanMove)
            {
                if (IsMoving && !touchingDirections.IsOnWall)
                {
                    if (touchingDirections.IsGrounded)
                    {
                        if (IsRunning)
                        {
                            return runSpeed;
                        }
                        else
                        {
                            return walkSpeed;
                        }
                    }
                    else
                    {
                        //Air move
                        return airWalkSpeed;
                    }
                }
                else
                {
                    return 0;   //idle speed is 0
                }
            } else
            {
                //Movement locked
                return 0;
            }

        }
    }


    public bool IsMoving
    {
        get { return _isMoving; }
        set
        {
            _isMoving = value;
            animator.SetBool(AnimationStrings.isMoving, value);
        }
    }


    public bool IsRunning
    {
        get
        {
            return _isRunning;
        }
        set
        {
            _isRunning = value;
            animator.SetBool(AnimationStrings.isRunning, value);
        }
    }
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        touchingDirections = GetComponent<TouchingDirections>();
        damageable = GetComponent<Damageable>();
        trailRenderer = GetComponent<TrailRenderer>();
        playerInput = GetComponent<PlayerInput>();
        playerInput.enabled = true;
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    private void FixedUpdate()
    {
        if (_isDashing)
        {
            return;
        }
        if (!damageable.LockVelocity)
        {
            rb.velocity = new Vector2(moveInput.x * currentMoveSpeed, rb.velocity.y);
        }
        animator.SetFloat(AnimationStrings.yVelocity, rb.velocity.y);
        HandleGravity();
        if (touchingDirections.IsGrounded && IsAlive)
        {
            _timeSinceGrounded = Time.time;
            _coyoteUsable = true;
            _canVarJump = true;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void onMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        if (IsAlive)
        {
            IsMoving = moveInput != Vector2.zero;
            SetFacingDirection(moveInput);
        }
        else
        {
            IsMoving = false;
        }

    }

    public bool isFacingRight
    {
        get { return _isFacingRight; }
        private set
        {
            if (_isFacingRight != value)
            {
                transform.localScale *= new Vector2(-1, 1);
            }
            _isFacingRight = value;
        }
    }
    public bool CanMove { get
        {
            return animator.GetBool(AnimationStrings.canMove);
        }
    }

    public bool IsAlive
    {
        get
        {
            return animator.GetBool(AnimationStrings.isAlive);
        }
    }

    private void SetFacingDirection(Vector2 moveInput)
    {
        if (moveInput.x > 0 && !isFacingRight)
        {
            isFacingRight = true;
        }
        else if (moveInput.x < 0 && isFacingRight)
        {
            isFacingRight = false;
        }
    }

    public void onRun(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            IsRunning = true;
        }
        else if (context.canceled)
        {
            IsRunning = false;
        }
    }


    private bool CanCoyoteJump()
    {
        return _coyoteUsable && !touchingDirections.IsGrounded && Time.time <  _timeSinceGrounded + coyoteTimer;
    }

    public void onJump(InputAction.CallbackContext context)
    {
        //Normal jump when grounded
        if (context.started && touchingDirections.IsGrounded && CanMove && IsAlive || CanCoyoteJump())
        {
            animator.SetTrigger(AnimationStrings.jumpTrigger);
            rb.velocity = new Vector2(rb.velocity.x, jumpImpulse);
            _coyoteUsable = false;
        }
        //Variable jump when cancel early
        if (context.canceled && rb.velocity.y > 0f && !touchingDirections.IsGrounded && CanMove && IsAlive)        
        {
            animator.ResetTrigger(AnimationStrings.jumpTrigger);
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.5f);
            _coyoteUsable = false;
        }
    }


    private void HandleGravity()
    {
        if(!touchingDirections.IsGrounded && rb.velocity.y < 0f)
        {
            rb.velocity = new Vector2(rb.velocity.x, Mathf.MoveTowards(rb.velocity.y, maxFallSpeed, fallAcceleration * Time.fixedDeltaTime));
            if (rb.velocity.y < maxFallSpeed)
                rb.velocity = new Vector2(rb.velocity.x, maxFallSpeed);
        }
    }

    public void onAttack(InputAction.CallbackContext context)
    {
        if (context.started && UIManager != null && !UIManager._isPaused)
        {
            if (UIManager.getMenuCliked())
            {
                //Do nothing, consuming the click if in menu
            }
            else 
                animator.SetTrigger(AnimationStrings.attackTrigger);
        }

    }

    public void onHit(int damage, Vector2 knockback)
    {
        rb.velocity = new Vector2(knockback.x, rb.velocity.y + knockback.y);
    }

    public void onBowAttack(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            animator.SetTrigger(AnimationStrings.bowAttackTrigger);
        }
    }

    public void onDeath(int delay, Vector2 startPos)
    {
        if (!IsAlive && damageable.Health <= 0)
        {
            gameOverScreen.Setup();
            StartCoroutine(waitUntilDeathAnim(0.6f));
        }
    }

    private IEnumerator waitUntilDeathAnim(float delay)
    {
        yield return new WaitForSeconds(delay);
        Time.timeScale = 0f;
    }

    private IEnumerator Dash()
    {
        animator.SetTrigger(AnimationStrings.isDashing);
        _canDash = false;
        _isDashing = true;
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        rb.velocity = new Vector2(transform.localScale.x * dashingPower, 0f);
        trailRenderer.emitting = true;
        yield return new WaitForSeconds(dashingTime);
        trailRenderer.emitting = false;
        rb.gravityScale = originalGravity;
        _isDashing = false;
        yield return new WaitForSeconds(dashingCooldown);
        _canDash = true;
    }

    public void onDash(InputAction.CallbackContext context)
    {
        if(context.started && IsAlive && CanMove && _canDash)
        {
            StartCoroutine(Dash());
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("FinishLine"))
        {
            playerInput.enabled = false;
        }
    }
}
