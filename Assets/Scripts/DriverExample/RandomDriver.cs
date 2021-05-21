using UnityEngine;

public class RandomDriver : Driver
{
    [SerializeField] float scalar = 1f;

    protected override void CalculateDriverValue()
    {
        Raw = Random.value * scalar;
    }
}
