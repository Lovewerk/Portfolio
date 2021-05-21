/// <summary>
/// Used to hold and calculate the runtime data of a flying object, such as speed, velocity, targetSpeed, etc.
/// </summary>
[System.Serializable]
public class FlightManager
{
	private IFlightSpecification fSpecs;
	private FlightInput fInput;
	private Rigidbody rb;
	private Transform tf;

	#region  Speed Variables

		// Note that the [ReadOnly] attribute only makes the inspector values readonly, not the actual values themselves
		
		[Header("Speed")]
		[ReadOnly] [SerializeField] Vector3 currVel; // current velocity
		public Vector3 CurrVel => currVel;
		
		[ReadOnly] [SerializeField] Vector3 currVelNormalized; // current velocity
		public Vector3 CurrVelNormalized => currVelNormalized;

		[ReadOnly] [SerializeField] float currSpeed; // current speed
		public float CurrSpeed => currSpeed;

		[ReadOnly] [SerializeField] float targetSpeed; // the desired speed
		public float TargetSpeed { get => targetSpeed; set => targetSpeed = Mathf.Min(Mathf.Max(0f, value), GetMaxSpeed()); } //remove ability to set target speed directly

		[ReadOnly] [SerializeField] float maxLiftSpeed; //speed at which lift (against gravity) reaches its max value.
		public float MaxLiftSpeed => (GetMaxLiftSpeed());

		[ReadOnly] [SerializeField] private float ratioCurrSpeedToMax;
		public float RatioCurrSpeedToMax => ratioCurrSpeedToMax = GetRatio(currSpeed, GetMaxSpeed());

		[ReadOnly] [SerializeField] private float ratioCurrSpeedToTarget;
		public float RatioCurrSpeedToTarget => ratioCurrSpeedToTarget = GetRatio(currSpeed, targetSpeed);

		[ReadOnly] [SerializeField] private float ratioTargetSpeedToMax;
		public float RatioTargetSpeedToMax => ratioCurrSpeedToTarget = GetRatio(targetSpeed, GetMaxSpeed());
		
		[ReadOnly] [SerializeField] private float ratioCurrSpeedToMaxLiftSpeed;
		public float RatioCurrSpeedToMaxLiftSpeed => ratioCurrSpeedToMaxLiftSpeed = GetRatio(currSpeed, MaxLiftSpeed);

		[ReadOnly] [SerializeField] private float ratioNonVerticalSpeedToMaxLiftSpeed; //used to determine lift strength. If object is falling straight down, it's not generating lift from flight.
		public float RatioNonVerticalSpeedToMaxLiftSpeed => ratioNonVerticalSpeedToMaxLiftSpeed = GetRatio(new Vector3(currVel.x, 0f, currVel.z).magnitude, MaxLiftSpeed);


		//Speed values used for giving the effect of acceleration. These are used to modify target speed.
		[ReadOnly] [SerializeField] private float thrustSpeed; 
		public float ThrustSpeed => thrustSpeed;

		[ReadOnly] [SerializeField] private float brakeSpeedCut; 
		public float BrakeSpeedCut => brakeSpeedCut;

		[ReadOnly] [SerializeField] private float altitudeSpeedCut; 
		public float AltitudeSpeedCut => altitudeSpeedCut;

		[ReadOnly] [SerializeField] private float turnSpeedCut; 
		public float TurnSpeedCut => turnSpeedCut;

		
	#endregion

	#region Angle Variables

		[Header("Angles")]
		[ReadOnly] [SerializeField] float pitchAngleToWorld;
		public float PitchAngleToWorld => pitchAngleToWorld;
		[ReadOnly] [SerializeField] float pitchAngleToVelocity;
		public float PitchAngleToVelocity => pitchAngleToVelocity;

	#endregion

	#region Force Variables

		[Header("Forces")]
		[ReadOnly] [SerializeField] private Vector3 forceComposite;
		private List<Vector3> forceVectors;

		[ReadOnly] [SerializeField] private Vector3 thrustForce;
		[ReadOnly] [SerializeField] private Vector3 dragForce;
		[ReadOnly] [SerializeField] private Vector3 liftAcceleration;
		[ReadOnly] [SerializeField] private Vector3 gravityForce;
		[ReadOnly] [SerializeField] private Vector3 pitchLiftForce;
		[ReadOnly] [SerializeField] private Vector3 altitudeForce;
		[ReadOnly] [SerializeField] private Vector3 altitudeDragIncreaseForce; // pitching the wings for altitude should increase drag.
		[ReadOnly] [SerializeField] private Vector3 lateralForce;
	#endregion

	#region Torque Variables
		
		[Header("Torques")]
		[ReadOnly] [SerializeField] private Vector3 torqueComposite;

		[ReadOnly] [SerializeField] private Vector3 pitchTorque;
		[ReadOnly] [SerializeField] private Vector3 rollTorque;
		[ReadOnly] [SerializeField] private Vector3 yawTorque;

	#endregion
	


	#region  Methods

		public FlightManager(IFlightSpecification fSpec, Rigidbody rigidbody, Transform trans, FlightInput input)
		{
			this.fSpecs = fSpec;
			fInput = input;
			rb = rigidbody;
			tf = trans;

			
			if(fSpec != null && rigidbody != null && tf != null)
			{
				UpdateData();
			}
			else
			{
				Dbg.Error(this.GetType() + " instantiated with at least one null parameter and is unlikely to function properly.");
			}
		}

		public void UpdateData()
		{
			GetVelocity();
			AssignPitchAngles();
			
		}

		/// <summary> /// This method applies all flight forces to the flying object. Call from FixedUpdate. /// </summary>
		public void ApplyPhysics()
		{
			CalculateForces();
			ApplyForces();

			CalculateTorques();
			ApplyTorques();
		}

		#region  Velocity Methods

			private void GetVelocity()
			{
				if(rb != null)
				{
					currVel = rb.velocity;
					currVelNormalized = currVel.normalized;
					currSpeed = currVel.magnitude;
				}
			}

			private void CalculateTargetSpeed()
			{
				targetSpeed = thrustSpeed + brakeSpeedCut + altitudeSpeedCut + turnSpeedCut;
			}

		#endregion

		#region Angle Methods

			private void AssignPitchAngles()
			{
				pitchAngleToWorld = tf.forward.y * 90f;				
				pitchAngleToVelocity = Vector3.SignedAngle(tf.forward, CurrVelNormalized, tf.right); // Note that this is not entirely correct; The angle will change if the player rotates about the y. Can figure it out another time though.
			}

		#endregion

		#region Force Methods

			private void CalculateForces()
			{
				Vector3 normalToVelocity =  (Vector3.Cross(CurrVelNormalized, tf.right)).normalized;
				thrustForce = tf.forward * TargetSpeed * RatioTargetSpeedToMax;
				dragForce = -CurrVelNormalized * CurrSpeed * RatioCurrSpeedToMax;
				pitchLiftForce = normalToVelocity * Mathf.Pow(fInput.pitch, 3) * Mathf.Abs(pitchAngleToVelocity / 20f) * RatioCurrSpeedToMax * fSpecs.PitchLiftStrength; //hard coded values just for prototyping

				altitudeDragIncreaseForce = -thrustForce * fSpecs.AltitudeDragCoefficient * RatioCurrSpeedToMaxLiftSpeed * Mathf.Abs(fInput.altitude);
				altitudeForce = normalToVelocity  * fSpecs.AltitudeStrength * RatioCurrSpeedToMaxLiftSpeed  * fInput.altitude;

				lateralForce = tf.right * fInput.lateral * ratioCurrSpeedToMax * fSpecs.LateralForceStrength;

				liftAcceleration = -Physics.gravity * Mathf.Clamp01(RatioNonVerticalSpeedToMaxLiftSpeed) * fSpecs.PercentLiftVsGravity;
				
			}

			private void ApplyForces()
			{
				//forces, therefore use mass
				rb.AddForce((((thrustForce + dragForce + altitudeDragIncreaseForce) * fSpecs.ThrustStrength) + pitchLiftForce + altitudeForce + lateralForce) * rb.mass);

				//accelerations, so don't use mass
				rb.AddForce(Physics.gravity + liftAcceleration, ForceMode.Acceleration);
			}

		#endregion

		#region Torque Methods

			private void CalculateTorques()
			{
				pitchTorque = tf.right * fInput.pitch * fSpecs.PitchRotationStrength;
				rollTorque = -tf.forward * fInput.roll * fSpecs.RollRotationStrength;
				yawTorque = tf.up * fInput.lateral  * fSpecs.YawRotationStrength; 
			}

			private void ApplyTorques()
			{
				torqueComposite = (pitchTorque + rollTorque + yawTorque) * fSpecs.OverallRotationStrength;
				rb.AddTorque(torqueComposite, ForceMode.Acceleration);
			}
		#endregion

		#region Calculation Helpers

			private float GetRatio(float numerator, float denominator)
			{
				float result = 0f;
				if(fSpecs != null)
				{
					result = MathHelp.TryDivide(numerator, denominator, 0f, 0f); // prevents divide by zero if given a denominator of 0
				}
				else
				{
					Dbg.Warn($"A reference in {this.GetType()} is null. ");
				}
				return result;
			}

			private float GetMaxSpeed()
			{
				return !CheckIfFlightSpecsNullAndLogErrorIfYes(fSpecs) ? fSpecs.MaxSpeed : 0f;
			}

			private float GetMaxLiftSpeed()
			{
				return !CheckIfFlightSpecsNullAndLogErrorIfYes(fSpecs) ? fSpecs.MaxLiftSpeed : 0f;
			}

			bool CheckIfFlightSpecsNullAndLogErrorIfYes(IFlightSpecification obj)
			{
				bool result = false;
				if(obj == null)
				{
					Dbg.Error(obj + " is null");
					result = true;
				}
				return result;
			}

		#endregion

	#endregion
}