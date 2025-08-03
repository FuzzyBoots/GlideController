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

    [Header("Gliding Pitch Control")]
    [Tooltip("How quickly the glider pitches forward or backward.")]
    [SerializeField] private float _pitchSpeed = 2.5f;

    [Tooltip("The maximum angle in degrees the glider can pitch down (dive).")]
    [SerializeField] private float _maxPitchAngle = 30f;

    [Tooltip("The maximum angle in degrees the glider can pitch up.")]
    [SerializeField] private float _minPitchAngle = -20f;

    [Tooltip("How much extra forward speed is gained when diving.")]
    [SerializeField] private float _speedBoostFromPitch = 5f;

    [Tooltip("How much lift (reduced fall speed) is generated when pitching up.")]
    [SerializeField] private float _liftFromPitch = 2f;

    [Tooltip("How much forward speed is lost to generate lift when pulling up. >1 is a net loss.")]
    [SerializeField] private float _forwardMomentumCost = 1.2f;

    [Tooltip("How quickly the glider's pitch stabilizes with no input.")]
    [SerializeField] private float _pitchDampening = 3f;

    // We need a variable to track the current pitch angle
    private float _currentPitch = 0f;

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
        // --- BASIC DRAG & INPUT ---
        _velocityVector -= _parachuteDrag * Time.deltaTime * _velocityVector;
        float v = locomotionInput.DirectionInput.y;
        float h = locomotionInput.DirectionInput.x;

        // --- PITCH & ROLL CALCULATIONS ---
        // Pitch
        float targetPitch = v > 0 ? v * _maxPitchAngle : v * -_minPitchAngle;
        _currentPitch = Mathf.Lerp(_currentPitch, targetPitch, _pitchSpeed * Time.deltaTime);
        if (Mathf.Approximately(v, 0f))
        {
            _currentPitch = Mathf.Lerp(_currentPitch, 0f, _pitchDampening * Time.deltaTime);
        }
        // Roll
        float targetRoll = -h * _maxRollAngle;
        _currentRoll = Mathf.Lerp(_currentRoll, targetRoll, _turnSpeed * Time.deltaTime);
        if (Mathf.Approximately(h, 0f))
        {
            _currentRoll = Mathf.Lerp(_currentRoll, 0f, _rollDampening * Time.deltaTime);
        }

        // --- VERTICAL & HORIZONTAL PHYSICS ---
        Vector3 horizontalVelocity = new Vector3(_velocityVector.x, 0, _velocityVector.z);
        float ySpeed = _velocityVector.y + player.Gravity * Time.deltaTime;

        // --- STATE 1: PULLING UP (Trade Speed for Lift) ---
        if (_currentPitch < -0.1f)
        {
            // Lift is proportional to how fast you're already going.
            float liftFactor = (_currentPitch / _minPitchAngle); // A 0-1 value based on how far you're pulled back.
            float generatedLift = horizontalVelocity.magnitude * liftFactor * _liftFromPitch;

            // Apply the upward lift. This can overcome gravity if you have enough speed.
            ySpeed += generatedLift * Time.deltaTime;

            // That lift costs forward momentum (induced drag).
            float liftCost = generatedLift * _forwardMomentumCost;
            _velocityVector -= transform.forward * liftCost * Time.deltaTime;
        }
        // --- STATE 2: DIVING (Trade Altitude for Speed) ---
        else
        {
            float difference = 0;
            // If falling faster than max fall speed, convert the difference to forward thrust.
            if (ySpeed < _fallSpeed)
            {
                difference = _fallSpeed - ySpeed;
                ySpeed = _fallSpeed;
            }

            // Add extra boost for how steeply you are diving.
            float pitchToSpeed = (_currentPitch > 0) ? (_currentPitch / _maxPitchAngle) * _speedBoostFromPitch : 0;

            Vector3 forwardThrust = transform.forward * (difference + pitchToSpeed);
            _velocityVector += forwardThrust;
        }

        _velocityVector.y = ySpeed;

        // --- APPLY ROTATION & MOVEMENT ---
        float yawChange = -_currentRoll * _yawFromRoll * Time.deltaTime;
        transform.Rotate(0, yawChange, 0, Space.World);
        transform.localRotation = Quaternion.Euler(_currentPitch, transform.localEulerAngles.y, _currentRoll);

        // Clamp total speed
        if (_velocityVector.magnitude > _maxGlideSpeed)
        {
            _velocityVector = _velocityVector.normalized * _maxGlideSpeed;
        }

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
        float time = 0;
        float duration = 0.25f; // A quarter of a second to level out

        // Capture the pitch and roll angles when we start landing
        float startingRoll = _currentRoll;
        float startingPitch = _currentPitch;

        while (time < duration)
        {
            // Smoothly interpolate both roll and pitch from their starting angles to zero
            _currentRoll = Mathf.Lerp(startingRoll, 0f, time / duration);
            _currentPitch = Mathf.Lerp(startingPitch, 0f, time / duration);

            // Update the character's local rotation to reflect the change
            transform.localRotation = Quaternion.Euler(_currentPitch, transform.localEulerAngles.y, _currentRoll);

            time += Time.deltaTime;
            yield return null; // Wait for the next frame
        }

        // After the loop, snap to a perfect zero rotation to ensure it's correct
        _currentRoll = 0f;
        _currentPitch = 0f;
        transform.localRotation = Quaternion.Euler(0f, transform.localEulerAngles.y, 0f);
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
        return Physics.SphereCast(transform.TransformPoint(groundCheckOffset), groundCheckRadius, Vector3.down, out _, groundCheckDistance, groundLayer);
    }

    #endregion
}