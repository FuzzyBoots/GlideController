using FS_ThirdPerson;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SD_GlidingSystem
{
    public class GlideController : EquippableSystemBase
    {
        bool enableGlide = false;
        public bool InAction { get; private set; }
        public override SystemState State { get; } = SystemState.Gliding;
        public override List<Type> EquippableItems => new List<Type>() { typeof(GliderItem) };

        public GameObject _floatObject;

        ICharacter player;
        private Animator _animator;
        PlayerController _playerController;
        LocomotionInputManager _locomotionInput;
        CharacterController _characterController;
        ItemEquipper _equippableItemController;

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

        public GliderItem currentGlideData
        {
            get
            {
                return (_equippableItemController?.EquippedItem is GliderItem shooterWeaponData)
                    ? shooterWeaponData
                    : null;
            }
        }
        public GliderObject CurrentGlideRight => _equippableItemController?.EquippedItemRight as GliderObject;
        public GliderObject CurrentGlideLeft => _equippableItemController?.EquippedItemLeft as GliderObject;
        public GliderObject CurrentGlideItem => _equippableItemController.EquippedItemObject as GliderObject;

        private void Start()
        {
            player = GetComponent<LocomotionICharacter>();
            _animator = player.Animator;
            // environmentScanner = GetComponent<EnvironmentScanner>();
            _locomotionInput = GetComponent<LocomotionInputManager>();
            _characterController = GetComponent<CharacterController>();
            _equippableItemController = GetComponent<ItemEquipper>();
            // animGraph = GetComponent<AnimGraph>();
            _playerController = GetComponent<PlayerController>();
        }

        public override void HandleStart()
        {
            base.HandleStart();

            Debug.Log("Glide Controller Start");
        }

        public override void HandleUpdate()
        {
            base.HandleUpdate();

            HandleGlideInputs();

            Debug.Log($"InAir: {_playerController.IsInAir} Input: {GlideInputHolding}");
            if (_playerController.IsInAir && GlideInputHolding)
            {
                if (!InAction && HighEnough())
                {
                    StartCoroutine(StartGliding());
                }
                else
                {
                    StartCoroutine(StopGliding());
                }
            } // Should we have a setup for !HighEnough?

            if (InAction && HighEnough())
            {
                HandleGlidingMovement();
            }
            else if (InAction && !HighEnough())
            {
                StartCoroutine(StopGliding());
            }
        }

        private void HandleGlidingMovement()
        {
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
            float h = _locomotionInput.DirectionInput.x;
            transform.Rotate(0, h * _rotationSpeed * Time.deltaTime, 0);

            _characterController.Move(_velocityVector * Time.deltaTime);
        }

        private IEnumerator StartGliding()
        {
            if (InAction) { yield return null; }
            InAction = true;

            _animator.SetBool("Gliding", true);

            _velocityVector = _characterController.velocity;

            player.OnStartSystem(this);
            //if (playerController.WaitToStartSystem)
            //    yield return new WaitUntil(() => playerController.WaitToStartSystem == false);
            // Should be playing animation.
            _animator.CrossFadeInFixedTime("Hanging Idle", 0.1f);
            _floatObject.SetActive(true);
        }

        private bool HighEnough()
        {
            // return !Physics.SphereCast(transform.TransformPoint(groundCheckOffset), groundCheckRadius, Vector3.down, out _, groundCheckDistance, groundLayer);
            return !Physics.Raycast(transform.TransformPoint(groundCheckOffset), Vector3.down, groundCheckDistance);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0, 1, 0, 0.5f);
            Gizmos.DrawRay(transform.TransformPoint(groundCheckOffset), Vector3.down * groundCheckRadius);
        }

        private IEnumerator StopGliding()
        {
            if (!InAction) { yield return null; }
            InAction = false;

            player.OnEndSystem(this);
            _floatObject.SetActive(false);

            _animator.SetBool("Gliding", false);
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

        #region input manager

#if inputsystem
        FSSystemsInputAction input;

        private void OnEnable()
        {
            input = new FSSystemsInputAction();
            input.Enable();
        }

        private void OnDisable()
        {
            input.Disable();
        }
#endif

        [Tooltip("Key to deploy the glide.")]
        public KeyCode glideInput = KeyCode.Space;

        [Tooltip("Key to stop the glide.")]
        public KeyCode glideReleaseInput = KeyCode.Space;

        [Tooltip("The button to deploy the glide.")]
        public string glideInputButton;

        [Tooltip("The button to stop the glide.")]
        public string glideReleaseInputButton;


        public bool GlideInputHolding { get; private set; }
        public bool GlideReleaseDown { get; private set; }


        void HandleGlideInputs()
        {

#if inputsystem
            GlideInputHolding = input.Swing.Glide.inProgress;
            GlideReleaseDown = input.Swing.GlideRelease.WasPerformedThisFrame();
#else
            GlideInputHolding = Input.GetKey(glideInput) || (!string.IsNullOrEmpty(glideInputButton) && Input.GetButton(glideInputButton));
            GlideReleaseDown = Input.GetKeyDown(glideReleaseInput) || (!string.IsNullOrEmpty(glideReleaseInputButton) && Input.GetButtonDown(glideReleaseInputButton));
#endif
        }

        #endregion

        #region Equip And UnEquip

        public void EquipItem(EquippableItem weaponData)
        {
            if (weaponData is GliderItem)
            {
                enableGlide = true;
            }
        }
        public void UnEquipItem()
        {
            enableGlide = false;
        }

        #endregion
    }
}