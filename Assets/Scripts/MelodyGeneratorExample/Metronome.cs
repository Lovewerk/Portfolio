using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Class that sends a metronome tick at a rate defined by bpm
/// </summary>
public class Metronome : MonoBehaviour
{
	public static event Action MetronomeTickEvent;

	[Range(1f, 1000f), SerializeField]
	private float bpm;
	public float Bpm { get => bpm; set { bpm = Mathf.Clamp(value, 1f, 1000f); } }
	public bool spoofFmodTicks;

	private WaitForSeconds waitForSeconds;

    void Start()
    {
    	StartCoroutine(_RunTicks());
	}

    private IEnumerator _RunTicks()
	{
		while(true)
		{
			RaiseTick();
			yield return new WaitForSeconds(60f / bpm);
		}
	}

	public void RaiseTick()
	{
		MetronomeTickEvent?.Invoke();
	}
}
