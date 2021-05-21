using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Simplified representation of the Flight Controller. The high level FlightController really only polls for input and delegates the actual physics calculations to the FlightManager
/// </summary>
public class FlightControllerSnippet : MonoBehaviour
{
	private Controls controls; // uses Unity's InputSystem package to handle user input

	FlightInput fInput;
	FlightManager flightManager;

	void Update()
	{
		PollInput(); // get player input and store it 
		flightManager.UpdateData();
	}

	void FixedUpdate()
	{
		flightManager.ApplyPhysics();
	}

	private void PollInput()
	{
		if(controls != null)
		{
			// assign player input from the control.Flight input map to fInput
			fInput.pitch = controls.Flight.Pitch.ReadValue<float>();
			fInput.roll = controls.Flight.Roll.ReadValue<float>();
			fInput.lateral = controls.Flight.Lateral.ReadValue<float>();
			fInput.altitude = controls.Flight.Altitude.ReadValue<float>();
		}
	}
}

/// <summary>
/// Container for input state.
/// </summary>
[System.Serializable]
public class FlightInput
{
	public float pitch;
	public float Pitch(bool invert) => invert ? -pitch : pitch;

	public float roll;
	public float Roll(bool invert) => invert ? -roll : roll;

	public float lateral;
	public float Lateral(bool invert) => invert ? -lateral : lateral;

	public float altitude;
	public float Altitude(bool invert) => invert ? -altitude : altitude;

	public FlightInput() {  }

	public FlightInput(float inPitch, float inRoll, float inLateral, float inAltitude)
	{
		pitch = inPitch;
		roll = inRoll;
		lateral = inLateral;
		altitude = inAltitude;
	}
}

