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

	public Vector3 startingPos;
	public Vector3 currentPos;
	public Vector3 previousPos;

	
	public Vector2 startSwipePos;
	public float startHoldingTime;
	public Vector3 startMousePos;
    public Vector2 swipeDir;
	public bool currentlyTouching;
	// public bool currentlyHolding;
	public bool directionChosen;
	public Touch currentTouch;
	public bool usingTouchInput;
	public bool usingMouseInput;
	public Vector3 holdingPos;
	public Vector3 holdingPosOffset;
	public float holdingBoneTargetVelocity;
	public float holdingBoneMaxVelocityChange;
	public float swipeSpeed;
	public float throwSpeed;
	public Vector3 throwDir;
	Camera cam;

	// Use this for initialization
	void Start () {
		cam = Camera.main;
		canThrowBone = true;
		boneRB = bone.GetComponent<Rigidbody>();
		boneCol = bone.GetComponent<Collider>();
	}
	
	void StartSwipe()
	{
		startingPos = cam.ScreenToViewportPoint(Input.mousePosition) - new Vector3(0.5f, 0.5f, 0.0f);
		swipeDir = Vector2.zero;
		startHoldingTime = Time.time;
		// currentlyHolding = true;
		usingTouchInput = true;
		currentlyTouching = true;
		currentTouch = Input.GetTouch(0);
		startSwipePos = currentTouch.position;
		directionChosen = false;
		if(!dog.returningBone)
		{
			dog.target = bone;
		}
	}

	void StartMouseDrag()
	{
		startingPos = cam.ScreenToViewportPoint(Input.mousePosition) - new Vector3(0.5f, 0.5f, 0.0f);
		startHoldingTime = Time.time;
		swipeDir = Vector2.zero;
		usingMouseInput = true;
		currentlyTouching = true;
		startMousePos = Input.mousePosition;
		directionChosen = false;
		if(!dog.returningBone)
		{
			dog.target = bone;
		}
	}

	void EndSwipe()
	{
		//Throw using dir

		directionChosen = true;
		currentlyTouching = false;
		if(!dog.hasBone)
		{
			Throw();
		}
	}

	void EndMouseDrag()
	{
		//Throw using dir

		directionChosen = true;
		currentlyTouching = false;
		if(!dog.hasBone)
		{
			Throw();
		}
	}

	void Throw()
	{
		// throwSpeed
		boneRB.velocity *= .1f;
		// var dir = cam.ScreenToViewportPoint(throwDir.normalized + cam.transform.forward);
		// var d = throwDir;
		// throwDir.Normalize();
		// throwDir.y = 0;
		// throwDir.z = 0;
		// throwDir = (currentPos - startingPos).normalized;
		throwDir = (currentPos - startingPos);
		// Vector3 dir = new Vector3(throwDir.normalized.x, 0, )
		// var dir = cam.ScreenToViewportPoint((throwDir + cam.transform.forward).normalized);
		// var dir = cam.ScreenToViewportPoint((d + cam.transform.forward));
		// var dir = throwDir + cam.transform.forward;
		// var dir = cam.transform.TransformPoint((throwDir + cam.transform.forward));
		// var dir = cam.transform.TransformDirection((throwDir + cam.transform.forward));
		var dir = cam.transform.TransformDirection(throwDir) + cam.transform.forward;
		// var dir = cam.ScreenToViewportPoint((throwDir + cam.transform.forward));
		// var dir = cam.ScreenToViewportPoint((throwDir + cam.transform.forward));
		dir.y = 0;
		// print(throwDir.normalized);
		// print(dir * throwSpeed);
		boneRB.AddForce(dir * throwSpeed, ForceMode.VelocityChange);
			// 		dog.target = bone;
			// dog.shouldGoGetBone = true;
		StartCoroutine(DelayedThrow());
	}

	IEnumerator DelayedThrow()
	{
		float elapsed = 0;
		while(elapsed < 2)
		{
			elapsed += Time.deltaTime;
			yield return null;
		}
		StartCoroutine(dog.GoGetBone());
	}

	// void Throw()
	// {
	// 	// throwSpeed
	// 	boneRB.velocity *= .1f;
	// 	Vector3 throwdir = cam.transform.forward;
	// 	throwdir.y = 0;
	// 	boneRB.AddForce(throwdir * throwSpeed, ForceMode.VelocityChange);
	// }

	void FixedUpdate()
	{
		if(currentlyTouching)
		{
			// holdingPos = cam.transform.forward
			if(usingTouchInput)
			{
				currentTouch = Input.GetTouch(0);

				currentPos = cam.ScreenToViewportPoint(currentTouch.position) - new Vector3(0.5f, 0.5f, 0.0f);
			}
			if(usingMouseInput)
			{
				currentPos = cam.ScreenToViewportPoint(Input.mousePosition) - new Vector3(0.5f, 0.5f, 0.0f);
			}
 			// currentPos -= new Vector3(0.5f, 0.5f, 0.0f);
			// swipeDir = currentPos - previousPos;
			// swipeSpeed = Mathf.Clamp(Mathf.Abs(swipeDir.magnitude/Time.deltaTime), 0, 15f) ;
			// throwSpeed = Mathf.Clamp(Mathf.Abs((currentPos - previousPos).magnitude/Time.deltaTime), 0, 15f) ;
			holdingPos = cam.transform.TransformPoint(holdingPosOffset + (currentPos * 2));
			// holdingPos.y = cam.transform.position.y;

			// holdingPos = cam.transform.TransformPoint(holdingPosOffset);
			// bone.position = holdingPos;
			// bone.velocity = Vector3.MoveTowards(bone.velocity) position = holdingPos;
			Vector3 moveToPos = holdingPos - boneRB.position;  //cube needs to go to the standard Pos
			// Vector3 velocityTarget = (moveToPos * targetVel * curve.Evaluate(curveTimer)) * Time.deltaTime; //not sure of the logic here, but it modifies velTarget
			Vector3 velocityTarget = moveToPos * holdingBoneTargetVelocity * Time.deltaTime; //not sure of the logic here, but it modifies velTarget
            boneRB.velocity = Vector3.MoveTowards(boneRB.velocity, velocityTarget, holdingBoneMaxVelocityChange);
			previousPos = currentPos;
		}
		// if(currentlyTouching)
		// {
		// 	// holdingPos = cam.transform.forward
		// 	Vector3 mousePos = Camera.main.ScreenToViewportPoint(Input.mousePosition);
 		// 	mousePos -= new Vector3(0.5f, 0.5f, 0.0f);
		// 	holdingPos = cam.transform.TransformPoint(holdingPosOffset + mousePos);
		// 	// holdingPos = cam.transform.TransformPoint(holdingPosOffset);
		// 	// bone.position = holdingPos;
		// 	// bone.velocity = Vector3.MoveTowards(bone.velocity) position = holdingPos;
		// 	Vector3 moveToPos = holdingPos - boneRB.position;  //cube needs to go to the standard Pos
		// 	// Vector3 velocityTarget = (moveToPos * targetVel * curve.Evaluate(curveTimer)) * Time.deltaTime; //not sure of the logic here, but it modifies velTarget
		// 	Vector3 velocityTarget = moveToPos * holdingBoneTargetVelocity * Time.deltaTime; //not sure of the logic here, but it modifies velTarget
        //     boneRB.velocity = Vector3.MoveTowards(boneRB.velocity, velocityTarget, holdingBoneMaxVelocityChange);
		// }
		if(directionChosen)
		{

		}
		
	}




	void Update()
	{
		// if(currentlyTouching)
		// {			
		// 	throwSpeed = (currentPos - previousPos).magnitude/Time.deltaTime;
		// 	currentPos = Camera.main.ScreenToViewportPoint(Input.mousePosition);
 		// 	currentPos -= new Vector3(0.5f, 0.5f, 0.0f);
		// 	holdingPos = cam.transform.TransformPoint(holdingPosOffset + currentPos);
		// }
		
		// if(canThrowBone && !dog.hasBone)
		if(canThrowBone && !hasThrownBone)
		{
				// Track a single touch as a direction control.
			if (Input.touchCount > 0 && !currentlyTouching)
			{
				currentTouch = Input.GetTouch(0);
				if(currentTouch.phase == TouchPhase.Began)
				{
					StartSwipe();
				}
			}

			if(usingTouchInput && currentlyTouching)
			{
				currentTouch = Input.GetTouch(0);
				if(currentTouch.phase == TouchPhase.Moved)
				{
					// swipeDir = currentTouch.position - startSwipePos;
				}
				if(currentTouch.phase == TouchPhase.Ended)
				{
					EndSwipe();
				}
			}




        // if (Input.touchCount > 0)
        // {

        //     currentTouch = Input.GetTouch(0);

        //     // Handle finger movements based on touch phase.
        //     switch (currentTouch.phase)
        //     {
        //         // Record initial touch position.
        //         case TouchPhase.Began:
		// 		{
		// 			if(!currentlyTouching)
		// 			{
		// 				StartSwipe();
		// 			}
        //             // startPos = touch.position;
        //             // directionChosen = false;
        //             break;
		// 		}

        //         // Determine direction by comparing the current touch position with the initial one.
        //         case TouchPhase.Moved:
        //             swipeDir = currentTouch.position - startPos;
        //             break;

        //         // Report that a direction has been chosen when the finger is lifted.
        //         case TouchPhase.Ended:
        //             directionChosen = true;
        //             break;
        //     }
        // }
		// 	//if touchcount > 0

		// 	//currently monitoring touch?


		// 	for (int i = 0; i < Input.touchCount; ++i)
		// 	{
		// 		if (Input.GetTouch(i).phase == TouchPhase.Began)
		// 		{
		// 			//record pos of began

		// 			//on that touch ends or gets cancelled get pos

		// 			//then you have the dir vector to throw



		// 			ThrowRaycast(Input.GetTouch(i).position);
		// 		}
		// 	}







			if (Input.GetMouseButtonDown(0) && !currentlyTouching)
			{
				StartMouseDrag();
				// ThrowRaycast(Input.mousePosition);
			}

			if(usingMouseInput && currentlyTouching)
			{
				if(Input.GetMouseButton(0))
				{
					// swipeDir = Input.mousePosition - startMousePos;
				}
				if(Input.GetMouseButtonUp(0))
				{
					EndMouseDrag();
				}
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
