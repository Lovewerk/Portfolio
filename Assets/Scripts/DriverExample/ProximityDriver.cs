using UnityEngine;

/// <summary>
/// Driver that depends on the distance in meters between a point and a target. Can optionally disable up to axes to refine what distance is measured.
/// </summary>
public class ProximityDriver : Driver
{
	[SerializeField] Transform target;
	[SerializeField] Transform referenceTransform;

	[SerializeField] bool ignoreX;
	[SerializeField] bool ignoreY;
	[SerializeField] bool ignoreZ;

	protected override void Awake()
	{
		base.Awake();
		
		if(!referenceTransform) { referenceTransform = this.transform; }
	}

	protected override void CalculateDriverValue()
	{
		if (target && referenceTransform)
		{
			if (!ignoreX | !ignoreY | !ignoreZ)
			{
				Vector3 targetPosition = target.position;

				if (ignoreX)
				{
					targetPosition.x = referenceTransform.position.x;
				}
				if (ignoreY)
				{
					targetPosition.y = referenceTransform.position.y;
				}
				if (ignoreZ)
				{
					targetPosition.z = referenceTransform.position.z;
				}

				Raw = Vector3.Distance(targetPosition, referenceTransform.position);
			}
		}
		else
		{
			Debug.LogError("Driver requires non-null targets.");
		}
	}
}
