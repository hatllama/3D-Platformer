using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float rotationSpeed = 15f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float gravityMultiplier = 2.5f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Double Jump Settings")]
    [SerializeField] private bool enableDoubleJump = true;
    [SerializeField] private float doubleJumpForce = 6f;
    [SerializeField] private ParticleSystem doubleJumpEffect;

    [Header("Dash Settings")]
    [SerializeField] private bool enableDash = true;
    [SerializeField] private float dashForce = 20f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1.5f;
    [SerializeField] private ParticleSystem dashEffect;
    [SerializeField] private TrailRenderer dashTrail;

    // References
    private Rigidbody rb;
    private Transform cameraTransform;
    private Animator animator;
    
    // State tracking
    private bool isGrounded;
    private bool canDoubleJump;
    private bool isDashing;
    private bool canDash = true;
    
    // Movement inputs
    private float horizontalInput;
    private float verticalInput;
    private bool jumpInput;
    private bool dashInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
        
        // Find the main camera
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cameraTransform = mainCamera.transform;
        }
        
        // Disable trail renderer initially
        if (dashTrail != null)
        {
            dashTrail.emitting = false;
        }
    }

    private void Update()
    {
        // Get player input
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");
        jumpInput = Input.GetButtonDown("Jump");
        dashInput = Input.GetKeyDown(KeyCode.LeftShift);
        
        // Check if the player is on the ground
        CheckGroundStatus();
        
        // Apply extra gravity for better jump feel
        if (!isGrounded && !isDashing)
        {
            rb.AddForce(Physics.gravity * gravityMultiplier, ForceMode.Acceleration);
        }
        
        // Handle jumping and double jumping
        HandleJump();
        
        // Handle dashing
        if (dashInput && canDash && enableDash && !isDashing)
        {
            StartCoroutine(PerformDash());
        }
        
        // Update animations if animator is available
        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        // Handle movement in FixedUpdate for physics consistency
        if (!isDashing)
        {
            Move();
        }
    }

    private void Move()
    {
        if (cameraTransform == null)
            return;

        // Calculate movement direction relative to camera
        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;
        
        // Project vectors onto the horizontal plane
        cameraForward.y = 0;
        cameraRight.y = 0;
        cameraForward.Normalize();
        cameraRight.Normalize();

        // Calculate the movement direction based on input and camera orientation
        Vector3 moveDirection = (cameraForward * verticalInput + cameraRight * horizontalInput).normalized;
        
        if (moveDirection.magnitude > 0.1f)
        {
            // Apply movement force
            Vector3 targetVelocity = moveDirection * moveSpeed;
            targetVelocity.y = rb.linearVelocity.y; // Preserve vertical velocity
            
            // Apply movement using velocity change for better control
            rb.linearVelocity = targetVelocity;
            
            // Rotate player to face movement direction
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void HandleJump()
    {
        if (jumpInput)
        {
            if (isGrounded)
            {
                Jump(jumpForce);
                
                // Enable double jump if feature is enabled
                if (enableDoubleJump)
                {
                    canDoubleJump = true;
                }
            }
            else if (canDoubleJump && enableDoubleJump)
            {
                // Perform double jump
                Jump(doubleJumpForce);
                canDoubleJump = false;
                
                // Play double jump effect
                if (doubleJumpEffect != null)
                {
                    doubleJumpEffect.Play();
                }
                
                // Trigger double jump animation if available
                if (animator != null)
                {
                    animator.SetTrigger("DoubleJump");
                }
            }
        }
    }

    private void Jump(float force)
    {
        // Reset vertical velocity before jumping for consistent jump height
        Vector3 velocity = rb.linearVelocity;
        velocity.y = 0;
        rb.linearVelocity = velocity;
        
        // Apply jump force
        rb.AddForce(Vector3.up * force, ForceMode.Impulse);
        
        // Trigger jump animation if available
        if (animator != null)
        {
            animator.SetTrigger("Jump");
        }
    }

    private IEnumerator PerformDash()
    {
        isDashing = true;
        canDash = false;
        
        // Store original drag
        float originalDrag = rb.linearDamping;
        rb.linearDamping = 0;
        
        // Calculate dash direction
        Vector3 dashDirection;
        if (new Vector2(horizontalInput, verticalInput).magnitude > 0.1f)
        {
            // Dash in input direction relative to camera
            Vector3 cameraForward = cameraTransform.forward;
            Vector3 cameraRight = cameraTransform.right;
            
            cameraForward.y = 0;
            cameraRight.y = 0;
            
            dashDirection = (cameraForward.normalized * verticalInput + cameraRight.normalized * horizontalInput).normalized;
        }
        else
        {
            // Dash in forward direction if no input
            dashDirection = transform.forward;
        }
        
        // Apply dash force
        rb.linearVelocity = dashDirection * dashForce;
        
        // Play dash effect
        if (dashEffect != null)
        {
            dashEffect.Play();
        }
        
        // Enable trail
        if (dashTrail != null)
        {
            dashTrail.emitting = true;
        }
        
        // Trigger dash animation if available
        if (animator != null)
        {
            animator.SetTrigger("Dash");
        }
        
        // Wait for dash duration
        yield return new WaitForSeconds(dashDuration);
        
        // Reset state
        isDashing = false;
        rb.linearDamping = originalDrag;
        
        // Disable trail
        if (dashTrail != null)
        {
            dashTrail.emitting = false;
        }
        
        // Start cooldown
        yield return new WaitForSeconds(dashCooldown - dashDuration);
        canDash = true;
    }

    private void CheckGroundStatus()
    {
        // Check if player is grounded
        bool wasGrounded = isGrounded;
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
        
        // Reset double jump when landing
        if (isGrounded && !wasGrounded)
        {
            canDoubleJump = false;
        }
        
        // Update animator parameter if available
        if (animator != null)
        {
            animator.SetBool("Grounded", isGrounded);
        }
    }

    private void UpdateAnimations()
    {
        if (animator == null)
            return;
            
        // Set movement animation parameters
        float movementMagnitude = new Vector2(horizontalInput, verticalInput).magnitude;
        animator.SetFloat("Speed", movementMagnitude);
        animator.SetBool("IsDashing", isDashing);
    }

    // Visualize the ground check radius in the editor
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}