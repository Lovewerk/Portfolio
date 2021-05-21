using UnityEngine;

public class AdsrEnvelope
{
	#region variables

		[SerializeField]
		[Tooltip("Used to differentiate envelopes in an ADSR collection when viewed in the inspector.")]
		private string name; 

		[Header("Timing Settings")]
		public AdsrEnvelopeTiming envelopeTiming; // a structure for hold attack, decay, and release time
		[Tooltip("If true, will use values below rather than envelopeTiming to control AdsrEnvelope.")]
		public bool useLocalEnvelopeTiming = true;

		[ConditionalHide("useLocalEnvelopeTiming", true)] // just an attribute to hide this element in inspector if useLocalEnvelopeTiming is false
		public float localAttackTime = 0.1f;
		[ConditionalHide("useLocalEnvelopeTiming", true)]
		public float localDecayTime = 0.5f;
		[ConditionalHide("useLocalEnvelopeTiming", true)]
		public float localReleaseTime = 1f;

		private float runtimeAttackTime;
		private float runtimeDecayTime;
		private float runtimeReleaseTime;

		[Space(5)]
		[Header("Amplitude Settings")]
		[Range(0f,1f)]
		public float initialLevel; // envelope starting value
		[Range(0f,1f)]
		public float peakLevel = 1f; // envelope peak value
		[Range(0f,1f)]
		public float sustainLevel = 1f; // envelope sustain value

		private float outputLevelAtEnterReleasePhase; //the amplitude level when the envelope is released, used internally to lerp from release to 0

		public bool bypassDecay = false; // if true, skip the decay phase
		public bool waitForRelease = true; // if true, the envelope will not proceed to the release phase until explicitly told to do so
		public bool allowRetrigger = true; // if true, the envelope will be restarted at the attack phase

		[SerializeField]
		AdsrPhase phase = AdsrPhase.Inactive;

		[SerializeField]
		[Range(0f,1f)]
		private float adsrValue; //the current amplitude of the envelope
		public float AdsrValue
		{
			get
			{
				return adsrValue;
			}
			set
			{
				adsrValue = value;
				RaiseAdsrValueChangeEvent();
			}
		}

	#endregion

	#region events and delegates

		public delegate void AdsrValueChangeEventHandler(float value, AdsrPhase phase);
		public event AdsrValueChangeEventHandler adsrValueChangeEvent;
		
		public delegate void AdsrPhaseChangeEventHandler(AdsrPhase phase, AdsrEnvelope thisEnvelope);

		// Indicates a change of phase. Do not assume that all phases will be entered. An envelope could be released or retriggered at any time so this is not guaranteed.
		public event AdsrPhaseChangeEventHandler adsrPhaseChangeEvent;

	#endregion

	#region methods

		public bool TriggerAttack(Timer timer)
		{
			bool success = false;
			if(allowRetrigger || phase == AdsrPhase.Inactive)
			{
				EnterAttackPhase(timer);
				success = true;
			}
			return success;
		}

		public bool TriggerRelease(Timer timer)
		{
			bool success = false;
			if(phase != AdsrPhase.Inactive && phase != AdsrPhase.Release)
			{
				EnterReleasePhase(timer);
				success = true;
			}
			return success;
		}

		void EnterAttackPhase(Timer timer)
		{
			float beginningValue = initialLevel;
			if(allowRetrigger && phase != AdsrPhase.Inactive)
			{
				beginningValue = adsrValue;
			}

			phase = AdsrPhase.Attack;
			RaiseAdsrPhaseChangeEvent();


			SetRuntimeTimingValues();

			timer.TotalPlaybackTime = runtimeAttackTime * (peakLevel - beginningValue);
			if(bypassDecay)
			{
				timer.stopPlaybackCallback = EnterSustainPhase;
			}
			else
			{
				timer.stopPlaybackCallback = EnterDecayPhase;
			}

			timer.Play(beginningValue);    
		}

		void EnterDecayPhase(Timer timer)
		{
			phase = AdsrPhase.Decay;
			RaiseAdsrPhaseChangeEvent();

			timer.TotalPlaybackTime = runtimeDecayTime;
			timer.stopPlaybackCallback = EnterSustainPhase;

			timer.Play();
		}

		void EnterSustainPhase(Timer timer)
		{
			phase = AdsrPhase.Sustain;

			if(!waitForRelease)
			{
				EnterReleasePhase(timer);
			}
			else
			{    
				RaiseAdsrPhaseChangeEvent(); //remember to evaluate waitForRelease in case of issues with going straight to release.
			}
		}

		void EnterReleasePhase(Timer timer)
		{
			phase = AdsrPhase.Release;
			outputLevelAtEnterReleasePhase = adsrValue;
			RaiseAdsrPhaseChangeEvent();

			timer.TotalPlaybackTime = runtimeReleaseTime * adsrValue;

			timer.stopPlaybackCallback = ResetEnvelope;

			timer.Play();
		}

		void ResetEnvelope(Timer timer)
		{
			phase = AdsrPhase.Inactive;
			RaiseAdsrPhaseChangeEvent();
		}

		public void CalculateValue(float inputValue)
		{
			switch(phase)
			{
				case AdsrPhase.Inactive:
					AdsrValue = initialLevel;
				break;

				case AdsrPhase.Attack:
					AdsrValue = inputValue * peakLevel;
				break;

				case AdsrPhase.Decay:
					AdsrValue = Mathf.Lerp(peakLevel, sustainLevel, inputValue);
				break;

				case AdsrPhase.Sustain:
				
				break;

				case AdsrPhase.Release:
					AdsrValue = Mathf.Lerp(outputLevelAtEnterReleasePhase, initialLevel, inputValue);
				break;

				default:
				break;
			}
		}

		private void SetRuntimeTimingValues()
		{
			if(useLocalEnvelopeTiming || envelopeTiming == null)
			{
				runtimeAttackTime = localAttackTime;
				runtimeDecayTime = localDecayTime;
				runtimeReleaseTime = localReleaseTime;
			}
			else
			{
				runtimeAttackTime = envelopeTiming.attackTime;
				runtimeDecayTime = envelopeTiming.decayTime;
				runtimeReleaseTime = envelopeTiming.releaseTime;
			}

			runtimeAttackTime = EnsureGreaterThanZero(runtimeAttackTime);
			runtimeDecayTime = EnsureGreaterThanZero(runtimeDecayTime);
			runtimeReleaseTime = EnsureGreaterThanZero(runtimeReleaseTime);
		}

		private float EnsureGreaterThanZero(float value)
		{
			if(value < 0)
			{
				value = Mathf.Abs(value);
			}
			
			if(Mathf.Approximately(value, 0f))
			{
			value += 0.001f; //add a small value so greater than 0. Not the nicest solution, but it works.
		}
			return value;
		}

		private void RaiseAdsrValueChangeEvent()
		{
			if(adsrValueChangeEvent != null)
			{
				adsrValueChangeEvent(adsrValue, phase);
			}
		}

		private void RaiseAdsrPhaseChangeEvent()
		{
			if(adsrPhaseChangeEvent != null)
			{
				adsrPhaseChangeEvent(phase, this);
			}
		}

	#endregion

}