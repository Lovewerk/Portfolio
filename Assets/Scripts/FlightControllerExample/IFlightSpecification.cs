/// <summary>
/// Used to define the capabilities of a given flying object
/// </summary>
public interface IFlightSpecification
{
	float MaxSpeed { get; }
	float CruisingSpeed { get; }
	float CruisingSpeedCoefficient { get; } //used to calculate CruisingSpeed. MaxSpeed * CruisingSpeedMultiplier
	float PercentMaxSpeedWhereLiftEqualsGravity { get; }
	float MaxLiftSpeed {get;}
	float PercentLiftVsGravity { get; }
	float ThrustStrength { get; }

	float BrakeStrength { get; }

	float BoostStrength { get; }

	float OverallRotationStrength { get; }
	float PitchRotationStrength { get; }
	float RollRotationStrength { get; }
	float YawRotationStrength { get; }
	float PitchLiftStrength { get; }

	float AltitudeStrength { get; }
	float AltitudeDragCoefficient { get; }

	float LateralForceStrength { get; }

}