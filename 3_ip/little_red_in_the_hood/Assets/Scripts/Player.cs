﻿using UnityEngine;
using System.Collections;

public class Player : MyMonoBehaviour {
	public float WalkSpeed = 12f;
	public float SlowSpeed = 6f;
	public float RunSpeed = 18f;
	
	public GameObject PaintSplat;
	public GameObject Glowstick;
	public Vector3 startLoc;
	
	public float AngleOffset = 90f;
	public float AngleTurnSpeed = .5f;
	public float IsMovingThreshold = 0.1f;
	
	public int NumPaint = 5;
	public int NumGlowsticks = 5;
	
	public static Player Instance {
		get; private set;
	}
	
	public bool IsUnderStreetlight {
		get; set;
	}
	
	public bool IsOnSprayPaint {
		get; private set;
	}
	
	const string MovementMode = "MovementMode";
	enum AnimState {
		Idle = 0,
		Sneak = 1,
		Walk = 2,
		Run = 3		
	}
	
	void Awake() {
		Instance = this;
	}

	// Use this for initialization
	void Start () {
		startLoc = transform.position;
	}
	
	void Update() {
		IsUnderStreetlight = GetComponent<Collider2D>().IsTouchingLayers(LayerMask.GetMask("Streetlight"));	
		IsOnSprayPaint = GetComponent<Collider2D>().IsTouchingLayers(LayerMask.GetMask("SprayPaint"));	
	
	/*
		if (Input.GetKeyDown(KeyCode.P) && NumPaint > 0) {
			NumPaint -= 1;
			Instantiate(PaintSplat, transform.position, Quaternion.identity);
		}

		 if (Input.GetButtonDown("Fire1") && NumGlowsticks > 0) {		
			NumGlowsticks -= 1;
			Instantiate(Glowstick, (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition), Quaternion.identity);
		}
		*/
	}
	
	//update is called every frame at fixed intervals
	void FixedUpdate()
	{
		float speed;
		if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
			speed = RunSpeed;
			anim.SetInteger(MovementMode, (int)AnimState.Run);
		} else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) {
			speed = SlowSpeed;
			anim.SetInteger(MovementMode, (int)AnimState.Sneak);
		} else if (Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0){
			speed = WalkSpeed;			
			anim.SetInteger(MovementMode, (int)AnimState.Walk);
		} else {
			anim.SetInteger(MovementMode, (int)AnimState.Idle);
			speed = WalkSpeed; // TODO: what should this be
		}
	
		Vector2 velocity = GetComponent<Rigidbody2D>().velocity;
		velocity = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")) * speed;
		GetComponent<Rigidbody2D>().velocity = velocity;
		
		if (velocity.magnitude > IsMovingThreshold) {
			float angle = Mathf.Repeat(Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg + AngleOffset, 360F);
			Vector3 euler = transform.eulerAngles;
			euler.z = angle;
		
			transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(euler), AngleTurnSpeed);
//			transform.eulerAngles = Vector3.Lerp(transform.eulerAngles, euler, AngleTurnSpeed);
		}
	}

	public void ResetPosToStart() {
		transform.position = startLoc;
	}
}
