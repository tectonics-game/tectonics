using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanCameraScript : MonoBehaviour {

	// Use this for initialization
	void Start () {
		offset = transform.position - target.transform.position;
	}
	
	float speed = 3.0f;
	float fieldOfView = 70;
	float minFOV = 50;
	float maxFOV = 150;
	public Transform target;

	Vector3 offset;

	void Update () {
		if (Input.GetMouseButton(0)) {
			offset = transform.position - target.transform.position;
			transform.LookAt(target);
			if (Input.GetAxis ("Mouse X")*Input.GetAxis ("Mouse X") > Input.GetAxis ("Mouse Y")*Input.GetAxis ("Mouse Y")) {
				transform.RotateAround (target.position, transform.up, Input.GetAxis ("Mouse X") * speed);
			} else {
				transform.RotateAround (target.position, Vector3.Cross(transform.up, offset), Input.GetAxis ("Mouse Y") * speed);
			}
		}

		if (Input.mouseScrollDelta.y != 0){
			offset = transform.position - target.transform.position;
			transform.position = transform.position + 0.01f * offset * ((float) Input.mouseScrollDelta.y);
		}

	}
}
