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

    [Tooltip("The amount of force applied to the legs to make them sway when turning.")]
    public float legSwayFactor = 50f;

    [Header("Ragdoll Components")]
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
            // Debug.Break();
            StartCoroutine(StopGliding());
        }
    }

    private void HandleGlidingMovement()
    {
        Debug.Log("Gliding?");
        float difference = 0;

        // Apply drag
        _velocityVector -= _parachuteDrag * Time.deltaTime * _velocityVector;

        // If y velocity is falling faster than the max, we'll set it to _fallSpeed.
        float ySpeed = _velocityVector.y + player.Gravity * Time.deltaTime;
        if (ySpeed < _fallSpeed)
        {
            difference = _fallSpeed - ySpeed;
            ySpeed = _fallSpeed;
        }
        _velocityVector.y = ySpeed;

        // The difference will be applied to our forward movement
        Vector3 _forwardMovement = transform.forward;
        _forwardMovement.y = 0;
        _forwardMovement = _forwardMovement.normalized * difference;

        _velocityVector += _forwardMovement;

        // We'll allow some rotation left and right?
        float h = locomotionInput.DirectionInput.x;
        transform.Rotate(0, h * _rotationSpeed * Time.deltaTime, 0);

        Debug.Log("Should be moving by " + _velocityVector);
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

    private IEnumerator StopGliding()
    {
        if (!InAction) { yield return null; }
        Debug.Log($"StopGliding");
        InAction = false;
        _floatObject.SetActive(false);
        Debug.Log("Setting Gliding to false");
        _animator.SetBool("Gliding", false);

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
        return Physics.CheckSphere(transform.TransformPoint(groundCheckOffset), groundCheckRadius);
    }

    #endregion
}