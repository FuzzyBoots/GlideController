using FS_ThirdPerson;
using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class GliderController2 : SystemBase
{
    [Tooltip("Enables or disables the glide feature.")]
    public bool enableGlide = true;

    public bool InAction { get; private set; }
    public override SystemState State { get; } = SystemState.Other;
    public bool GlideInputHolding { get; private set; }

    public GameObject _floatObject;

    ICharacter player;
    PlayerController playerController;
    LocomotionInputManager locomotionInput;
    CharacterController characterController;
    LocomotionController locomotionController;

    [Header("Ground Check Settings")]
    [Tooltip("Radius of ground detection sphere")]
    [SerializeField] float groundCheckRadius = 0.7f;

    [Tooltip("Distance for ground detection ray")]
    [SerializeField] float groundCheckDistance = 2f;

    [Tooltip("Offet between the player's root position and the ground detection sphere")]
    [SerializeField] Vector3 groundCheckOffset = new Vector3(0f, 0.15f, 0.07f);

    [Tooltip("All layers that should be considered as ground")]
    public LayerMask groundLayer = 1;

    [SerializeField] Vector3 _velocityVector;
    [SerializeField] private float _parachuteDrag = 0.1f;
    [SerializeField] private float _rotationSpeed = 180f;
    [SerializeField] private float _fallSpeed = -0.2f;
    [SerializeField] Animator _animator;
    private Vector3 forwardLandForceMultiplier;

    [Header("Gliding Turning Physics")]
    [Tooltip("How quickly the glider rolls into a turn. Higher is more responsive.")]
    [SerializeField] private float _turnSpeed = 2.5f;

    [Tooltip("The maximum angle in degrees the glider can bank.")]
    [SerializeField] private float _maxRollAngle = 40f;

    [Tooltip("How much yaw (turning) is applied based on the roll. Higher makes for sharper turns.")]
    [SerializeField] private float _yawFromRoll = 0.8f;

    [Tooltip("How quickly the glider stabilizes and levels out with no input.")]
    [SerializeField] private float _rollDampening = 3f;

    // We need a variable to track the current roll angle
    private float _currentRoll = 0f;

    [Header("Gliding Speed")]
    [SerializeField] private float _maxGlideSpeed = 25f;

    [Header("Ragdoll Paramters")]
    [Tooltip("The amount of force applied to the legs to make them sway when turning.")]
    public float legSwayFactor = 50f;

    [Tooltip("The Rigidbody components of the character's legs (thighs and calves).")]
    public Rigidbody[] legRigidbodies;

    private void Start()
    {
        player = GetComponent<ICharacter>();
        playerController = GetComponent<PlayerController>();
        characterController = GetComponent<CharacterController>();
        locomotionController = GetComponent<LocomotionController>();

        _animator = GetComponent<Animator>();

        locomotionInput = GetComponent<LocomotionInputManager>();
    }

    public void Update()
    {
//#if inputsystem
//            GlideInputHolding = input.Gliding.Gliding.inProgress;
//#else
//        var grapplingInput = Input.GetKey(glideInput) || (!string.IsNullOrEmpty(glideInputButton) && Input.GetButton(glideInputButton));
//#endif

        GlideInputHolding = Input.GetKeyDown(KeyCode.Space);
    }

    public override void HandleUpdate()
    {
        base.HandleUpdate();

        if (playerController.IsInAir && GlideInputHolding)
        {
            Debug.Log("In Air and input down");
            Debug.Log($"InAction: {InAction} and High Enough: {HighEnough()}");
            if (!InAction && HighEnough())
            {
                StartCoroutine(StartGliding());
            }
            else if (InAction)
            {
                // Debug.Log($"InAction: {InAction} and HighEnouch: {HighEnough()}");
                StartCoroutine(StopGliding());
            }
        } // Should we have a setup for !HighEnough?

        if (InAction && HighEnough())
        {
            HandleGlidingMovement();
        }
        else if (InAction && !HighEnough())
        {
            Debug.Log("Not High Enough");
            StartCoroutine(StopGliding());
        }
    }

    private void HandleGlidingMovement()
    {
        float difference = 0;

        // Apply drag
        _velocityVector -= _parachuteDrag * Time.deltaTime * _velocityVector;

        // If y velocity is falling faster than the max, clamp it to _fallSpeed.
        float ySpeed = _velocityVector.y + player.Gravity * Time.deltaTime;
        if (ySpeed < _fallSpeed)
        {
            difference = _fallSpeed - ySpeed;
            ySpeed = _fallSpeed;
        }
        _velocityVector.y = ySpeed;

        float h = locomotionInput.DirectionInput.x;

        // Determine the target roll angle from player input
        float targetRoll = -h * _maxRollAngle;

        // Smoothly interpolate to the target roll for a fluid motion
        _currentRoll = Mathf.Lerp(_currentRoll, targetRoll, _turnSpeed * Time.deltaTime);

        // Apply dampening to level out automatically when there is no input
        if (Mathf.Approximately(h, 0f))
        {
            _currentRoll = Mathf.Lerp(_currentRoll, 0f, _rollDampening * Time.deltaTime);
        }

        // Calculate the yaw rotation based on how much we are currently rolled
        float yawChange = -_currentRoll * _yawFromRoll * Time.deltaTime;

        // Apply the final rotations
        // Yaw is applied in world space to turn the character horizontally.
        // Roll is applied in local space to bank the character model.
        transform.Rotate(0, yawChange, 0, Space.World);
        transform.localRotation = Quaternion.Euler(transform.localEulerAngles.x, transform.localEulerAngles.y, _currentRoll);

        // Convert the potential energy difference into forward thrust
        Vector3 forwardThrust = transform.forward * difference;
        _velocityVector += forwardThrust;

        // Apply the final calculated movement to the CharacterController
        characterController.Move(_velocityVector * Time.deltaTime);
    }

    private IEnumerator StartGliding()
    {
        if (InAction) { yield return null; }
        InAction = true;

        _animator.SetBool("Gliding", true);

        _velocityVector = characterController.velocity;

        player.OnStartSystem(this);
        _animator.CrossFadeInFixedTime("Hanging Idle", 0.1f);
        _floatObject.SetActive(true);
    }

    private bool HighEnough()
    {
        bool grounded = CheckGround();
        return !grounded;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0, 0.5f);
        Gizmos.DrawRay(transform.TransformPoint(groundCheckOffset), Vector3.down * groundCheckRadius);
    }

    private IEnumerator LevelOutRotation()
    {
        // The duration for the leveling out animation
        float time = 0;
        float duration = 0.25f; // A quarter of a second to level out

        // Capture the roll angle when we start landing
        float startingRoll = _currentRoll;

        while (time < duration)
        {
            // Calculate the new roll by smoothly interpolating from our starting roll to zero
            _currentRoll = Mathf.Lerp(startingRoll, 0f, time / duration);

            // Update the character's local rotation to reflect the change
            transform.localRotation = Quaternion.Euler(transform.localEulerAngles.x, transform.localEulerAngles.y, _currentRoll);

            time += Time.deltaTime;
            yield return null; // Wait for the next frame
        }

        // After the loop, snap to a perfect zero roll to ensure it's correct
        _currentRoll = 0f;
        transform.localRotation = Quaternion.Euler(transform.localEulerAngles.x, transform.localEulerAngles.y, 0f);
    }

    private IEnumerator StopGliding()
    {
        if (!InAction) { yield return null; }
        Debug.Log($"StopGliding");
        InAction = false;
        _floatObject.SetActive(false);
        Debug.Log("Setting Gliding to false");
        _animator.SetBool("Gliding", false);

        // Call the new coroutine to handle leveling out the character
        StartCoroutine(LevelOutRotation());

        // StartCoroutine(HandleLandingMomentum());
        player.OnEndSystem(this);

    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Collision");
        // If we have a collision while gliding, we stop gliding.
        if (InAction)
        {
            StartCoroutine(StopGliding());
        }
    }

    IEnumerator SetRotation(Vector3 lookDir, float rotateSpeed)
    {
        var dir = Quaternion.LookRotation(lookDir);
        while (Quaternion.Angle(transform.rotation, dir) > .1f)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, dir, rotateSpeed * Time.deltaTime * 50);
            yield return null;
        }
        transform.rotation = dir;
    }

    IEnumerator CrossFadeAsync(string anim, float crossFadeTime = .2f, bool enableRootmotion = false, Action onComplete = null)
    {
        Debug.Log($"Fading to {anim}");
        if (enableRootmotion)
            EnableRootMotion();
        _animator.CrossFadeInFixedTime(anim, crossFadeTime);
        yield return null;
        while (_animator.IsInTransition(0))
        {
            yield return null;
        }
        var animState = _animator.GetCurrentAnimatorStateInfo(0);

        float timer = 0f;

        while (timer <= animState.length)
        {
            timer += Time.deltaTime * _animator.speed;
            yield return null;
        }
        if (enableRootmotion)
            ResetRootMotion();
        onComplete?.Invoke();
    }

    #region rootmotion

    bool prevRootMotionVal;
    private string glideInputButton;
    private KeyCode glideInput;

    public void EnableRootMotion()
    {
        prevRootMotionVal = player.UseRootMotion;
        player.UseRootMotion = true;
    }
    public void ResetRootMotion()
    {
        player.UseRootMotion = prevRootMotionVal;
    }

    public override void HandleOnAnimatorMove(Animator animator)
    {
        transform.rotation *= animator.deltaRotation;
        characterController.Move(animator.deltaPosition);
    }

    #endregion

    #region Landing

    IEnumerator HandleLandingMomentum(bool playLandAnimation = true)
    {
        Debug.Log("Calling HandleLandingMomentum");
        // Get the current direction of movement, excluding the y value.
        Vector3 currDir = _velocityVector.normalized;
        currDir.y = 0;

        InAction = false;

        StartCoroutine(CrossFadeAsync("DropFallIdle", .2f, false));

        // Calculate the velocity required for the jump landing.
        float ySpeed = _velocityVector.y;

        // While the player is not grounded, apply gravity and move the player towards the landing position.
        while (!locomotionController.IsGrounded)
        {
            ySpeed += player.Gravity * Time.deltaTime; // Apply gravity.
            _velocityVector.y = ySpeed;
            characterController.Move(_velocityVector * Time.deltaTime);
            yield return null;
        }
        _velocityVector = Vector3.zero;
    }

    bool CheckGround()
    {
        return Physics.CheckSphere(transform.TransformPoint(groundCheckOffset), this.groundCheckDistance, groundLayer);
    }

    #endregion
}