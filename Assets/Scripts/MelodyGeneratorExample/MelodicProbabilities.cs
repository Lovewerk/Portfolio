using System.Collections.Generic;
using Music.Probability;
using UnityEngine;

/// <summary>
/// A scriptable object which stores values related to melodic probabilities such as pitch collections, possible intervals, rhythmic durations, etc.
/// </summary>
[CreateAssetMenu(menuName = "Music/Melodic Ruleset")]
public class MelodicProbabilities : ScriptableObject
{
	[SerializeField] List<WeightedInt> pitches;
    [SerializeField] List<WeightedInt> intervals;
	[SerializeField] List<WeightedInt> durations;

	//elements stored with their cumulative weight, allowing generating a num from [0, weightSum], then binary searching for the chosen element.
	List<WeightedInt> pitchesCumulative;
	List<WeightedInt> intervalsCumulative;
	List<WeightedInt> durationsCumulative;

	int pitchesTotalWeight;
	int intervalsTotalWeight;
	int durationsTotalWeight;

	private WeightComparer weightComparer; // allows comparing/sorting WeightedInts according to weight rather than value.
	private WeightComparer WeightComparer { get { if(weightComparer == null) { weightComparer = new WeightComparer(); } return weightComparer; } }

	[Range(0f, 1f), SerializeField]
	private float repetitionProbability = 0.1f; //how likely it is that a pitch will be repeated.

	private void Awake()
	{
		Initialize();
	}

	public void Initialize()
	{
		if(pitches == null) { pitches = new List<WeightedInt>(); }
		if(intervals == null) { intervals = new List<WeightedInt>(); }
		if(durations == null) { durations = new List<WeightedInt>(); }
	}

	#if UNITY_EDITOR
	private void OnValidate()
	{
		if(pitches?.Count == 0 || intervals?.Count == 0 || durations?.Count == 0 )
		{
			Debug.LogWarning("At least one of the lists is empty in " + this.name);
		}

		pitchesTotalWeight = CalculateCumulativeWeights(pitches, pitchesCumulative);
		intervalsTotalWeight = CalculateCumulativeWeights(intervals, intervalsCumulative);
		durationsTotalWeight = CalculateCumulativeWeights(durations, durationsCumulative);
	}
	#endif

	/// <summary>
	/// Returns the total weight of all WeightedInts in list
	/// </summary>
	/// <param name="list"></param>
	/// <param name="cumulativeList"></param>
	/// <returns></returns>
	private int CalculateCumulativeWeights(List<WeightedInt> list, List<WeightedInt> cumulativeList)
	{
		cumulativeList?.Clear();
		int totalWeight = 0;
		if(list != null && list.Count > 0)
		{
			if(cumulativeList == null) { cumulativeList = new List<WeightedInt>(); }
			
			int cumulative = 0;
		
			for(int i = 0; i < list.Count; i++)
			{
				cumulativeList.Add(new WeightedInt(list[i].Value, list[i].Weight + cumulative));
				cumulative += (cumulativeList[i].Weight - cumulative);
			}
			totalWeight = cumulative;
			cumulativeList.Sort(WeightComparer); // sort the list by weight so that a binary search can be used to select an element, with selection probability dependent on weight
		}
		else
		{
			cumulativeList?.Clear();
		}
		return totalWeight;
	}

	public void AddElement(MelodyComponent component, WeightedInt element) //add element to list which corresponds to component. If element exists, replace it with the new one
	{
		if(element.Initialized)
		{
			switch(component)
			{
				case MelodyComponent.Pitch:
					AddToList(pitches, element);
					pitchesTotalWeight = CalculateCumulativeWeights(pitches, pitchesCumulative);
				break;

				case MelodyComponent.Interval:
					AddToList(intervals, element);
					intervalsTotalWeight = CalculateCumulativeWeights(intervals, intervalsCumulative);
				break;

				case MelodyComponent.Duration:
					AddToList(durations, element);
					durationsTotalWeight = CalculateCumulativeWeights(durations, durationsCumulative);
				break;
			}
		}
	}
	
	public void RemoveElement(MelodyComponent component, int element)
	{
		switch(component)
		{
			case MelodyComponent.Pitch:
				RemoveFromList(pitches, element);
				pitchesTotalWeight = CalculateCumulativeWeights(pitches, pitchesCumulative);
			break;

			case MelodyComponent.Interval:
				RemoveFromList(intervals, element);
				intervalsTotalWeight = CalculateCumulativeWeights(intervals, intervalsCumulative);
			break;

			case MelodyComponent.Duration:
				RemoveFromList(durations, element);
				durationsTotalWeight = CalculateCumulativeWeights(durations, durationsCumulative);
			break;
		}
	}

	private bool TryGetIndex(List<WeightedInt> list, WeightedInt element, out int index)
	{
		return TryGetIndex(list, element.Value, out index);
	}

	private bool TryGetIndex(List<WeightedInt> list, int element, out int index)
	{
		bool found = false;
		index = -1;

		if(list != null)
		{
			for(int i = 0; i < list.Count; i++)
			{
				if(list[i].Value == element)
				{
					found = true;
					index = i;
					break;
				}
			}
		}

		return found;
	}

	private void AddToList(List<WeightedInt> list, WeightedInt element)
	{
		if(list != null)
		{
			int index = -1;
			if(TryGetIndex(list, element, out index))
			{
				list[index] = element;
			}
			else
			{
				list.Add(element);
			}
		}
	}

	private void RemoveFromList(List<WeightedInt> list, int element)
	{
		if(list != null)
		{
			int index = -1;
			if(TryGetIndex(list, element, out index))
			{
				list.RemoveAt(index);
			}
		}
	}

	public Note GenerateNote(Note previousNote = default(Note))
	{
		return new Note(ChoosePitch(previousNote), ChooseDuration());
	}

	private int ChoosePitch(Note previousNote = default(Note))
	{
		int generatedPitch = SelectWeightedRandom(pitchesCumulative, pitchesTotalWeight).Value;

		if(previousNote.Pitch == generatedPitch)
		{
			if(Random.Range(0f, 1f) < (1 - repetitionProbability))
			{
				while(generatedPitch == previousNote.Pitch)
				{
					generatedPitch = SelectWeightedRandom(pitchesCumulative, pitchesTotalWeight).Value;
				}
				
			}
			#if UNITY_EDITOR
			else
			{
				Debug.LogWarning("Note will be repeated!");
			}
			#endif
		}
		return generatedPitch;
	}

	private int ChooseDuration()
	{
		return SelectWeightedRandom(durationsCumulative, durationsTotalWeight).Value;
	}

	/// <summary>
	/// Takes a list of WeightedInt sorted by weight and generates a random value in range [0, totalWeight], then binary searches list for element at that weight. 
	/// </summary>
	/// <param name="weightedList"></param>
	/// <param name="totalWeight"></param>
	/// <returns></returns>
	private WeightedInt SelectWeightedRandom(List<WeightedInt> weightedList, int totalWeight)
	{
		int randomIndex = weightedList.BinarySearch(new WeightedInt(0, Random.Range(0, totalWeight)), WeightComparer);
		// bitwise complement (~) of the value returned by BinarySearch is the index of the first element in the list that is larger than searched value

		if(randomIndex < 0)
		{
			randomIndex = ~randomIndex;
		}
		
		return weightedList[randomIndex];
	}
}

public enum MelodyComponent
{
	Pitch, Interval, Duration
}