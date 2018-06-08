using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrowBone : MonoBehaviour {

	public Transform bone;
	public Transform returnPoint;
	public Rigidbody boneRB;
	public Collider boneCol;
	public DogAgent dog;
	public bool canThrowBone;
	public bool dogHasBone;
	public LayerMask groundLayer;
	public bool hasThrownBone;


	// Use this for initialization
	void Start () {
		canThrowBone = true;
		boneRB = bone.GetComponent<Rigidbody>();
		boneCol = bone.GetComponent<Collider>();
	}
	void Update()
	{
		// if(canThrowBone && !dog.hasBone)
		if(canThrowBone && !hasThrownBone)
		{
			for (int i = 0; i < Input.touchCount; ++i)
			{
				if (Input.GetTouch(i).phase == TouchPhase.Began)
				{
					ThrowRaycast(Input.GetTouch(i).position);
				}
			}

			if (Input.GetMouseButtonDown(0))
			{
				ThrowRaycast(Input.mousePosition);
			}
		}
    }

	void ThrowRaycast(Vector3 pos)
	{
		// Construct a ray from the current touch coordinates
		Ray ray = Camera.main.ScreenPointToRay(pos);
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit, 20, groundLayer))
		{
			bone.position = hit.point;
			// hasThrownBone = true;
			dog.target = bone;
			dog.shouldGoGetBone = true;
		}

	}



}
