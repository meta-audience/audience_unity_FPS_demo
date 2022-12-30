using UnityEngine;
using Unity.FPS.Game;
using RootMotion.Demos;
using RootMotion;
using UnityEngine.Events;

namespace Unity.FPS.Gameplay

{

	/// <summary>
	/// Basic Mecanim character controller for 3rd person view.
	/// </summary>
	[RequireComponent(typeof(AnimatorController3rdPerson))]
	public class PlayerCharacterController : MonoBehaviour
	{

		[SerializeField] CameraController cam; // The camera

		[Header("References")]
		[Tooltip("Reference to the main camera used for the player")]
		public Camera PlayerCamera;

		[Header("General")]
		[Tooltip("Force applied downward when in the air")]
		public float GravityDownForce = 20f;

		[Tooltip("Audio source for footsteps, jump, etc...")]
		public AudioSource AudioSource;
		[Header("Audio")]
		[Tooltip("Amount of footstep sounds played when moving one meter")]
		public float FootstepSfxFrequency = 1f;

		[Tooltip("Amount of footstep sounds played when moving one meter while sprinting")]
		public float FootstepSfxFrequencyWhileSprinting = 1f;

		public float SprintSpeedModifier = 2f;

		[Tooltip("Sound played for footsteps")]
		public AudioClip FootstepSfx;

		[Tooltip("Sound played when jumping")] public AudioClip JumpSfx;

		[Tooltip("Sound played when landing")] public AudioClip LandSfx;

		public float GroundCheckDistance = 0.05f;

		public LayerMask GroundCheckLayers = -1;

		[Header("Movement")]
		[Tooltip("Max movement speed when grounded (when not sprinting)")]
		public float MaxSpeedOnGround = 10f;

		[Tooltip("Sound played when taking damage froma fall")]
		public AudioClip FallDamageSfx;

		[Header("Fall Damage")]
		[Tooltip("Whether the player will recieve damage when hitting the ground at high speed")]
		public bool RecievesFallDamage;

		[Tooltip("Minimun fall speed for recieving fall damage")]
		public float MinSpeedForFallDamage = 10f;

		[Tooltip("Fall speed for recieving th emaximum amount of fall damage")]
		public float MaxSpeedForFallDamage = 30f;

		[Tooltip("Damage recieved when falling at the mimimum speed")]
		public float FallDamageAtMinSpeed = 10f;

		[Tooltip("Damage recieved when falling at the maximum speed")]
		public float FallDamageAtMaxSpeed = 50f;
		public Vector3 CharacterVelocity { get; set; }
		public bool IsGrounded { get; private set; }
		public bool HasJumpedThisFrame { get; private set; }
		public bool IsDead { get; private set; }

		public float KillHeight = -50f;

		public UnityAction<bool> OnStanceChanged;

		public bool IsCrouching { get; private set; }

		private Health m_Health;

		private PlayerInputHandler m_InputHandler;

		private CharacterController m_Controller;

		private PlayerWeaponsManager m_WeaponsManager;

		private Actor m_Actor;

		private AnimatorController3rdPerson animatorController; // The Animator controller

		private Vector3 m_GroundNormal;

		private Vector3 m_CharacterVelocity;

		private Vector3 m_LatestImpactSpeed;

		private const float k_JumpGroundingPreventionTime = 0.2f;

		private const float k_GroundCheckDistanceInAir = 0.07f;

		void Awake()
		{
			ActorsManager actorsManager = FindObjectOfType<ActorsManager>();
			if (actorsManager != null)
				actorsManager.SetPlayer(gameObject);
		}

		void Start()
		{
			animatorController = GetComponent<AnimatorController3rdPerson>();
			

			m_InputHandler = GetComponent<PlayerInputHandler>();
			DebugUtility.HandleErrorIfNullGetComponent<PlayerInputHandler, PlayerCharacterController>(m_InputHandler,
				this, gameObject);

			m_WeaponsManager = GetComponent<PlayerWeaponsManager>();
			DebugUtility.HandleErrorIfNullGetComponent<PlayerWeaponsManager, PlayerCharacterController>(
				m_WeaponsManager, this, gameObject);

			m_Health = GetComponent<Health>();
			DebugUtility.HandleErrorIfNullGetComponent<Health, PlayerCharacterController>(m_Health, this, gameObject);

			m_Actor = GetComponent<Actor>();
			DebugUtility.HandleErrorIfNullGetComponent<Actor, PlayerCharacterController>(m_Actor, this, gameObject);


			m_Health.OnDie += OnDie;
			cam.enabled = false;
		}
		void Update()
		{
			if (!IsDead && transform.position.y < KillHeight)
			{
				m_Health.Kill();
			}
			HasJumpedThisFrame = false;


			// landing
			if (transform.position.y <= -50)
			{
				// Fall damage
				float fallSpeed = -Mathf.Min(CharacterVelocity.y, m_LatestImpactSpeed.y);
				float fallSpeedRatio = (fallSpeed - MinSpeedForFallDamage) /
									   (MaxSpeedForFallDamage - MinSpeedForFallDamage);
				if (RecievesFallDamage && fallSpeedRatio > 0f)
				{
					float dmgFromFall = Mathf.Lerp(FallDamageAtMinSpeed, FallDamageAtMaxSpeed, fallSpeedRatio);
					m_Health.TakeDamage(dmgFromFall, null);

					// fall damage SFX
					AudioSource.PlayOneShot(FallDamageSfx);
				}
				else
				{
					// land SFX
					AudioSource.PlayOneShot(LandSfx);
				}
			}

		}



		void LateUpdate()
		{
			// Update the camera first so we always have its final translation in the frame
			cam.UpdateInput();
			cam.UpdateTransform();

			// Read the input
			Vector3 input = inputVector;

			// Should the character be moving? 
			// inputVectorRaw is required here for not starting a transition to idle on that one frame where inputVector is Vector3.zero when reversing directions.
			bool isMoving = inputVector != Vector3.zero || inputVectorRaw != Vector3.zero;

			// Character look at vector.
			Vector3 lookDirection = cam.transform.forward;

			// Aiming target
			Vector3 aimTarget = cam.transform.position + (lookDirection * 10f);

			// Move the character.
			animatorController.Move(input, isMoving, lookDirection, aimTarget);
		}

		// Convert the input axis to a vector
		private static Vector3 inputVector
		{
			get
			{
				return new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
			}
		}

		// Convert the raw input axis to a vector
		private static Vector3 inputVectorRaw
		{
			get
			{
				return new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
			}
		}

		void OnDie()
		{
			IsDead = true;

			// Tell the weapons manager to switch to a non-existing weapon in order to lower the weapon
			m_WeaponsManager.SwitchToWeaponIndex(-1, true);

			EventManager.Broadcast(Events.PlayerDeathEvent);
		}

		void GroundCheck()
		{
			// Make sure that the ground check distance while already in air is very small, to prevent suddenly snapping to ground
			float chosenGroundCheckDistance =
				IsGrounded ? (m_Controller.skinWidth + GroundCheckDistance) : k_GroundCheckDistanceInAir;

			// reset values before the ground check
			IsGrounded = false;
			m_GroundNormal = Vector3.up;

			// only try to detect ground if it's been a short amount of time since last jump; otherwise we may snap to the ground instantly after we try jumping
			if (Time.time >= k_JumpGroundingPreventionTime)
			{
				// if we're grounded, collect info about the ground normal with a downward capsule cast representing our character capsule
				if (Physics.CapsuleCast(GetCapsuleBottomHemisphere(), GetCapsuleTopHemisphere(m_Controller.height),
					m_Controller.radius, Vector3.down, out RaycastHit hit, chosenGroundCheckDistance, GroundCheckLayers,
					QueryTriggerInteraction.Ignore))
				{
					// storing the upward direction for the surface found
					m_GroundNormal = hit.normal;

					// Only consider this a valid ground hit if the ground normal goes in the same direction as the character up
					// and if the slope angle is lower than the character controller's limit
					if (Vector3.Dot(hit.normal, transform.up) > 0f &&
						IsNormalUnderSlopeLimit(m_GroundNormal))
					{
						IsGrounded = true;

						// handle snapping to the ground
						if (hit.distance > m_Controller.skinWidth)
						{
							m_Controller.Move(Vector3.down * hit.distance);
						}
					}
				}
			}
		}


		bool IsNormalUnderSlopeLimit(Vector3 normal)
		{
			return Vector3.Angle(transform.up, normal) <= m_Controller.slopeLimit;
		}
		Vector3 GetCapsuleBottomHemisphere()
		{
			return transform.position + (transform.up * m_Controller.radius);
		}

		// Gets the center point of the top hemisphere of the character controller capsule    
		Vector3 GetCapsuleTopHemisphere(float atHeight)
		{
			return transform.position + (transform.up * (atHeight - m_Controller.radius));
		}

		public Vector3 GetDirectionReorientedOnSlope(Vector3 direction, Vector3 slopeNormal)
		{
			Vector3 directionRight = Vector3.Cross(direction, transform.up);
			return Vector3.Cross(slopeNormal, directionRight).normalized;
		}

	}
}