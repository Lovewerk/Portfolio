using UnityEngine;

public class AdsrBehaviour : TriggerOrToggleBehaviour
{
	public AdsrEnvelope adsr;

	protected Timer adsrTimer;

	protected virtual void Awake()
	{
		adsrTimer = new Timer(this);
	}

	protected virtual void OnEnable()
	{
		if(adsrTimer != null)
		{
			adsrTimer.updatePlaybackPosition += OnAdsrTimerTick;
		}
	}

	protected virtual void OnDisable()
	{
		if(adsrTimer != null)
		{
			adsrTimer.updatePlaybackPosition -= OnAdsrTimerTick;
		}
	}
	
	// Called on every frame where the envelope is being processed.
	// Intended to calculate adsrValue and then take appropriate action with that value, dispatch events, etc.
	protected virtual void OnAdsrTimerTick(float timerValue)
	{
		if(adsr != null)
		{
			adsr.CalculateValue(Mathf.Clamp01(timerValue));
		}
	}

	/// <summary>
	/// Used to enter the Attack or Release phase of the AdsrEnvelope.
	/// </summary>
	/// <param name="toggleState"></param>
	public override void Toggle(bool toggleState)
	{
		if(toggleState)
		{
			adsr.TriggerAttack(adsrTimer);
		}
		else
		{
			adsr.TriggerRelease(adsrTimer);
		}
	}

	/// <summary>
	/// Attempts to trigger the AdsrEnvelope
	/// </summary>
	/// <returns> False if envelope is already triggered and allowRetrigger == false </returns>
	public override bool Trigger()
	{
		return adsr.TriggerAttack(adsrTimer);
	}
}