using UnityEngine;

/// <summary>
/// Driver that generates perlin noise.
/// </summary>
public class NoiseDriver : Driver
{

	float noisePosition;

	[Tooltip("Controls the distance between each sampled noise coordinate")]
	[SerializeField] float noiseStepSize = 1f;

	float minStepSize = 0.1f; //only used to ensure inspector input does not set noiseStepSize below this value.

	protected override void Awake()
	{
		base.Awake();

		noisePosition = Random.value * 10000f; //start the noise in a reasonably random position
	}

	protected override void CalculateDriverValue()
	{
		Raw = MathUtil.Remap(Mathf.PerlinNoise(noisePosition, noisePosition), 0f, 1f, RawMin, RawMax);

		noisePosition += (noiseStepSize * GetDeltaTime());
	}

	protected override void OnValidate() 
	{
		base.OnValidate();
		
		noiseStepSize = Mathf.Max(minStepSize, noiseStepSize);
	}
}
