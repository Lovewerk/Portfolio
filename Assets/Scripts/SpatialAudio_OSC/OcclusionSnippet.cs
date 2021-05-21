/// <summary>
/// //cast a ray from SoundEmitter to listener to determine if there is anything between them and, if so, how strong the occlusion should be.
/// </summary>
/// <param name="target"></param>
/// <returns></returns>
public float CalcOcclusions(Transform target) 
{

	float effectiveOcclusion;
	float occlusionStrengthModifier = 0.95f; // use this value to reduce occlusion strength to fake indirect sound
	Vector3 occludeAmount; //the amount of occlusion on each axis

	Vector3 deadzone; // if the raycast hits within the deadzone, occlusion should be at it's maximum
	float deadzoneScale = 0.6f; // scales the deadzone where a value of 1 would cover the entire bounding box

	Vector3 localHitPoint; // point relative to the hit collider's center

	RaycastHit rayHit;
	if(Physics.Raycast(transform.position, (target.position - transform.position), out rayHit, listenerDistance, (int)LayerMasks.OccludesAudio , QueryTriggerInteraction.Collide))
	{
		deadzone = rayHit.collider.bounds.extents * deadzoneScale;

		localHitPoint = AbsVectorComponents(rayHit.collider.bounds.center - rayHit.point);

		occludeAmount = CalcOcclusionStrength(localHitPoint, deadzone, rayHit.collider.bounds.extents, deadzoneScale);
		
		effectiveOcclusion = Mathf.Min(occludeAmount.x, occludeAmount.y, occludeAmount.z) * occlusionStrengthModifier;
	}
	else // no collision, therefore no occlusion
	{
		effectiveOcclusion = 0f;
	}

	if(Mathf.Approximately(effectiveOcclusion, 0f)) // if close enough to 0, set occlusion to 0 and do not send continued updates to audio engine.
	{
		effectiveOcclusion = 0f;
	}

	return effectiveOcclusion;
}

float 

/// <summary>
/// Applies absolute value to each component of a Vector3
/// </summary>
/// <param name="vec"></param>
/// <returns></returns>
Vector3 AbsVectorComponents(Vector3 vec)
{
	vec.x = Mathf.Abs(vec.x);
	vec.y = Mathf.Abs(vec.y);
	vec.z = Mathf.Abs(vec.z);

	return vec;
}

/// <summary>
/// somewhat arbitrary formula for determining occlusion strength based on distance between rayhit point and extents of the object's bounding box
/// </summary>
/// <param name="hitPoint"></param>
/// <param name="deadzone"></param>
/// <param name="colliderExtents"></param>
/// <param name="deadzoneScale"></param>
/// <returns></returns>
Vector3 CalcOcclusionStrength(Vector3 hitPoint, Vector3 deadzone, Vector3 colliderExtents, float deadzoneScale)
{
	Vector3 result;
	result.x = Mathf.Sqrt((1f - (Mathf.Max(hitPoint.x, deadzone.x) / colliderExtents.x)) / (1f - deadzoneScale));
	result.y = Mathf.Sqrt((1f - (Mathf.Max(hitPoint.y, deadzone.y) / colliderExtents.y)) / (1f - deadzoneScale));
	result.z = Mathf.Sqrt((1f - (Mathf.Max(hitPoint.z, deadzone.z) / colliderExtents.z)) / (1f - deadzoneScale));
	
	return result;
}