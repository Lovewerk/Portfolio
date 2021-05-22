using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class MaterialController : MonoBehaviour
{

	Renderer render;
	MaterialPropertyBlock mpb;

	[SerializeField] Color baseColor = Color.white;
	[SerializeField] Color BaseColor
	{
		get => baseColor;
		set
		{
			baseColor = value;
			SetMaterialColor(baseColor);
		}
	}

	[SerializeField] Color emissionColor = Color.white;
	private Color runtimeEmissionColor;
	public Color EmissionColor
	{
		get => emissionColor;
		set
		{
			emissionColor = value;
			runtimeEmissionColor = emissionColor;
			SetEmissionColor(emissionColor * emissionStrength);
		}
	}

	[SerializeField] private float emissionStrength = 0f;
	public float EmissionStrength
	{
		get
		{
			return emissionStrength;
		}
		set
		{
			emissionStrength = value;
			SetEmissionStrength(emissionStrength);
		}
	}

	[SerializeField] Driver emissionStrengthDriver;


	[Header("Texture Settings")]
	[SerializeField] int textureIndex;
	public int TextureIndex
	{
		get => textureIndex;
		set
		{
			textureIndex = value;

			if(textureIndex < 0) // ensure textureIndex is positive
			{
				textureIndex = 0;
			}

			// ensure textureIndex does not exceed the last element in textures. Might be better to allow to set whatever, and just not try to set invalid indices
			if(textures != null && textures.Count <= textureIndex)
			{
				textureIndex = textures.Count - 1;

				if(textureIndex < 0)
				{
					textureIndex = 0;
				}
			}
			
			// set the texture on the material
			if(textures?.Count > 0 && textureIndex < (textures.Count))
			{
				mpb.SetTexture("_BaseMap", textures[textureIndex]);
			}
		}
	}

	[SerializeField] List<Texture> textures;


	void Awake()
	{
		render = gameObject.GetComponent<Renderer>();
		mpb = new MaterialPropertyBlock();

		if(textures == null) { textures = new List<Texture>(); }
	}

	void OnEnable()
	{
		if(emissionStrengthDriver != null)
		{
			emissionStrengthDriver.ValueChangedEvent += HandleEmissionStrengthChange;
		}
	}

	void OnDisable()
	{
		if(emissionStrengthDriver != null)
		{
			emissionStrengthDriver.ValueChangedEvent -= HandleEmissionStrengthChange;
		}
	}

	public void SetMaterialColor(Color targetColor)
	{
		if(mpb != null && render != null)
		{
			mpb.SetColor("_BaseColor", targetColor);
			render.SetPropertyBlock(mpb);
			// RaiseMaterialColorChangeEvent(targetColor);
		}
	}

	public void SetEmissionColor(Color targetColor)
	{
		if (mpb != null && render != null)
		{
			mpb.SetColor("_EmissionColor", targetColor);
			render.SetPropertyBlock(mpb);
			// RaiseEmissionColorChangeEvent(targetColor);
		}
	}

	public void SetEmissionStrength(float strength)
	{
		if(mpb != null && render != null)
		{
			mpb.SetColor("_EmissionColor", runtimeEmissionColor * strength);
			render.SetPropertyBlock(mpb);
			// RaiseEmissionStrengthChangeEvent(strength);
		}
	}

	void HandleEmissionStrengthChange(object sender, EventArgs e)
	{
		if (e is DriverArgs args)
		{
			EmissionStrength = args.Scaled;
		}
	}

	private void OnValidate()
	{
		TextureIndex = textureIndex;

		BaseColor = baseColor;
		EmissionColor = emissionColor;
	}


}
