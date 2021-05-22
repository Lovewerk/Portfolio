using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
/// <summary>
/// Stores an int and an associated weight for use in probability-based calculations
/// </summary>
public struct WeightedInt
{
	[SerializeField] int value;
	[SerializeField] int weight;
	[SerializeField] bool initialized;


	public int Value { get => value;}
	public int Weight { get => weight;}
	public bool Initialized { get => initialized;}

	public WeightedInt(int value, int weight)
	{
		this.value = value;
		this.weight = Mathf.Max(0, weight);

		initialized = true;
	}
}

/// <summary>
/// Defines how a WeightedInt should be compared when sorting by weight
/// </summary>
public class WeightComparer : Comparer<WeightedInt>
{
	public override int Compare(WeightedInt x, WeightedInt y)
	{
		int result;
		if(x.Weight < y.Weight)
		{
			result = -1;
		}
		else if(x.Weight.Equals(y.Weight))
		{
			result = 0;
		}
		else
		{
			result = 1;
		}
		return result;
	}
}

/// <summary>
/// Defines how a WeightedInt should be compared when sorting by value
/// </summary>
public class ValueCompare : Comparer<WeightedInt>
{
	public override int Compare(WeightedInt x, WeightedInt y)
	{
		int result;
		if(x.Value < y.Value)
		{
			result = -1;
		}
		else if(x.Value == y.Value)
		{
			result = 0;
		}
		else
		{
			result = 1;
		}
		return result;
	}
}