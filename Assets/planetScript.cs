using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class planetScript : MonoBehaviour {

	public Texture[] textures;
	public Renderer rend;
	public Dropdown activeSkin;
	// Use this for initialization
	void Start () {
		rend = GetComponent<Renderer>();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void UpdatePlanetSkin(){
		if (activeSkin.value > textures.GetLength(0) - 1)
			return;
		Debug.Log ("img" + textures [activeSkin.value]);
		rend.material.mainTexture = textures[activeSkin.value];
	}

	public void seedTectonics(){
		Texture2D tectonic = new Texture2D (textures [2].width, textures [2].height);


	}
}
