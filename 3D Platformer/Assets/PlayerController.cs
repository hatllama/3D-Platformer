using UnityEngine;
using System.Collections;

public class ImprovedPlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float acceleration = 80f;
    [SerializeField] private float deceleration = 20f;
    [SerializeField] private float airControl = 0.7f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 20f;             // Increased for sharper jumps
    [SerializeField] private float jumpCutMultiplier = 0.5f;    // New parameter for sharp cut when releasing jump
    [SerializeField] private float fallMultiplier = 3f;         // Increased for faster falling
    [SerializeField] private float maxFallSpeed = 30f;          // Cap on fall speed
    [SerializeField] private float jumpBufferTime = 0.08f;      // Reduced for more precise timing

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.1f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Double Jump Settings")]
    [SerializeField] private bool enableDoubleJump = true;
    [SerializeField] private float doubleJumpForce = 18f;       // Increased for sharper double jump
    [SerializeField] private ParticleSystem doubleJumpEffect;

    [Header("Dash Settings")]
    [SerializeField] private bool enableDash = true;
    [SerializeField] private float dashSpeed = 25f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 1.2f;
    [SerializeField] private ParticleSystem dashEffect;
    
    // Private variables
    private Rigidbody rb;
    private Transform cameraTransform;
    private Vector3 moveDirection;
    
    // State tracking
    private bool isGrounded;
    private bool isJumping;
    private bool hasDoubleJumped;
    private bool isDashing;
    private bool canDash = true;
    
    // Input buffers
    private float lastJumpTime;
    private float lastGroundedTime;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        // Configure rigidbody for sharper movement
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotation; // Prevent the cube from rotating
        
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cameraTransform = mainCamera.transform;
        }
        
        if (groundCheck == null)
        {
            Debug.LogWarning("Ground check transform not assigned. Creating one at the bottom of the player.");
            groundCheck = new GameObject("GroundCheck").transform;
            groundCheck.SetParent(transform);
            groundCheck.localPosition = new Vector3(0, -0.5f, 0); // Assuming 1 unit tall player
        }
    }
    
    private void Update()
    {
        // Check ground status
        CheckGrounded();
        
        // Get input and calculate movement direction
        GetMovementInput();
        
        // Handle jump with more immediate response
        HandleJumpInput();
        
        // Handle dash input
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash && enableDash && !isDashing)
        {
            StartCoroutine(PerformDash());
        }
        
        // Apply sharper jump physics
        ApplySharpJumpPhysics();
    }
    
    private void FixedUpdate()
    {
        if (!isDashing)
        {
            ApplyMovement();
        }
    }
    
    private void CheckGrounded()
    {
        bool wasGrounded = isGrounded;
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
        
        // Reset states when landing
        if (isGrounded && !wasGrounded)
        {
            hasDoubleJumped = false;
            isJumping = false;
        }
        
        if (isGrounded)
        {
            lastGroundedTime = Time.time;
        }
    }
    
    private void GetMovementInput()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");
        
        // Calculate camera-relative movement direction
        if (cameraTransform != null)
        {
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;
            
            // Project camera direction onto the horizontal plane
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();
            
            moveDirection = (forward * verticalInput + right * horizontalInput).normalized;
        }
        else
        {
            moveDirection = new Vector3(horizontalInput, 0, verticalInput).normalized;
        }
    }
    
    private void HandleJumpInput()
    {
        // Jump button pressed
        if (Input.GetButtonDown("Jump"))
        {
            lastJumpTime = Time.time;
        }
        
        // Jump button released while ascending (sharp cutoff)
        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0 && isJumping)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier, rb.linearVelocity.z);
        }
        
        // Check for jump execution (shorter buffer for more precise timing)
        bool jumpBuffered = Time.time - lastJumpTime < jumpBufferTime;
        bool canJumpNow = isGrounded || (Time.time - lastGroundedTime < 0.1f); // Shorter coyote time
        
        if (jumpBuffered && canJumpNow)
        {
            ExecuteJump(jumpForce);
            lastJumpTime = 0; // Reset jump buffer
        }
        // Double jump with immediate execution (no buffer for double jump)
        else if (Input.GetButtonDown("Jump") && !isGrounded && !hasDoubleJumped && enableDoubleJump)
        {
            ExecuteDoubleJump();
        }
    }
    
    private void ExecuteJump(float force)
    {
        isJumping = true;
        // Reset vertical velocity for consistent jump height
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * force, ForceMode.Impulse);
    }
    
    private void ExecuteDoubleJump()
    {
        isJumping = true;
        hasDoubleJumped = true;
        
        // Reset vertical velocity for consistent double jump height
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * doubleJumpForce, ForceMode.Impulse);
        
        // Play double jump effect if assigned
        if (doubleJumpEffect != null)
        {
            doubleJumpEffect.Play();
        }
    }
    
    private void ApplySharpJumpPhysics()
    {
        // Apply stronger gravity when falling for snappier feel
        if (rb.linearVelocity.y < 0)
        {
            // Apply increased gravity when falling
            rb.linearVelocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
            
            // Cap fall speed for better control
            if (rb.linearVelocity.y < -maxFallSpeed)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, -maxFallSpeed, rb.linearVelocity.z);
            }
        }
    }
    
    private void ApplyMovement()
    {
        // Get current horizontal velocity
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        
        // Target velocity based on input
        Vector3 targetVelocity = moveDirection * moveSpeed;
        
        // Different acceleration based on grounded state
        float currentAccel = isGrounded ? acceleration : acceleration * airControl;
        
        // If no input, decelerate to stop
        if (moveDirection.magnitude < 0.1f)
        {
            currentAccel = deceleration;
            targetVelocity = Vector3.zero;
        }
        
        // Calculate smooth movement
        horizontalVelocity = Vector3.Lerp(horizontalVelocity, targetVelocity, currentAccel * Time.fixedDeltaTime);
        
        // Apply the new horizontal velocity while preserving vertical velocity
        rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);
    }
    
    private IEnumerator PerformDash()
    {
        isDashing = true;
        canDash = false;
        
        // Store original values
        float originalDrag = rb.linearDamping;
        
        // Set dash properties
        rb.linearDamping = 0;
        
        // Determine dash direction (use movement direction or forward if no input)
        Vector3 dashDirection = moveDirection.magnitude > 0.1f ? moveDirection : transform.forward;
        
        // Set velocity directly for consistent dash speed
        rb.linearVelocity = dashDirection * dashSpeed;
        
        // Disable gravity during dash for cleaner movement
        bool useGravity = rb.useGravity;
        rb.useGravity = false;
        
        // Play dash effect if assigned
        if (dashEffect != null)
        {
            dashEffect.Play();
        }
        
        // Wait for dash duration
        yield return new WaitForSeconds(dashDuration);
        
        // Restore gravity
        rb.useGravity = useGravity;
        
        // Limit vertical velocity after dash to prevent unwanted height gain
        if (rb.linearVelocity.y > 0 && !isJumping)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x * 0.9f, 0, rb.linearVelocity.z * 0.9f);
        }
        else
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x * 0.9f, rb.linearVelocity.y, rb.linearVelocity.z * 0.9f);
        }
        
        // Restore drag
        rb.linearDamping = originalDrag;
        isDashing = false;
        
        // Wait for cooldown
        yield return new WaitForSeconds(dashCooldown - dashDuration);
        canDash = true;
    }
    
    // Ground detection visualization
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer) ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}