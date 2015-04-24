﻿using UnityEngine;
using System.Collections;

public class Player : MyMonoBehaviour {
	public float WalkSpeed = 12f;
	public float SlowSpeed = 6f;
	public float RunSpeed = 18f;
	public float IdleSpeed = 1f;

	public GameObject RedDiscovered;
	public GameObject PaintSplat;
	public GameObject Glowstick;
	public Vector3 startLoc;
	
	public float AngleOffset = 90f;
	public float AngleTurnSpeed = .5f;
	public float IsMovingThreshold = 0.1f;
	
	public int NumPaint = 5;
	public int NumGlowsticks = 5;
	
	public float Health = 1.0f;

	public AudioClip WalkSound;
	public AudioClip RunSound;
	public AudioClip StealthSound;
	public bool lockedMovement = false;

	AudioSource audio;
	
	public static Player Instance {
		get; private set;
	}
	
	public bool IsUnderStreetlight {
		get; set;
	}
	
	public bool IsOnSprayPaint {
		get; set;
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
		StartCoroutine(RedDiscoveredCoroutine());
		startLoc = transform.position;
		audio = GetComponent<AudioSource>();
	}
	
	public float HealthRegenRate = 0.1f;
	
	private bool GodMode;
	public bool StealthMode {
		get; private set;
	}
	
	IEnumerator RedDiscoveredCoroutine()
	{
		SpriteRenderer renderer = RedDiscovered.GetComponent<SpriteRenderer>();
		
		while (true)
		{
			Color newColor = renderer.color;
			newColor.a = 1.0f - Health;
			renderer.color = newColor;
			
			yield return null;
		}
	}
	
	IEnumerator GameOverCoroutine()
	{
		lockedMovement = true;
		float timeToSpin = 1.5f;
		float timeSpinning = 0.0f;
		
		while (timeSpinning <= timeToSpin)
		{
			timeSpinning += Time.deltaTime;
			
			Camera.main.transform.Rotate(new Vector3(0.0f, 0.0f, 0.25f));
			
			yield return null;
		}
		
		Application.LoadLevel(Application.loadedLevel);
	}
	
	void Update() {
		IsUnderStreetlight = GetComponent<Collider2D>().IsTouchingLayers(LayerMask.GetMask("Streetlight"));	
		IsOnSprayPaint = GetComponent<Collider2D>().IsTouchingLayers(LayerMask.GetMask("SprayPaint"));	
		
		if (IsOnSprayPaint) {
			color = Color.red;
		} else if (IsUnderStreetlight) {
			color = Color.yellow;
		} else {
			color = Color.white;
		}
		
		if (Input.GetKey(KeyCode.G) && Input.GetKey(KeyCode.M) && Input.anyKeyDown) {
			GodMode = !GodMode;
			if (GodMode)
				print("God mode on");
			else
				print("God mode off");
		}
		
		if (Input.GetKey(KeyCode.S) && Input.GetKey(KeyCode.M) && Input.anyKeyDown) {
			StealthMode = !StealthMode;
			if (StealthMode)
				print("Stealth mode on");
			else
				print("Stealth mode off");
		}
		
		if (Health < 0.0f && !GodMode) {
			StartCoroutine(GameOverCoroutine());
			return;
		}
		Health += HealthRegenRate * Time.deltaTime;
		Health = Mathf.Min(1, Health);

//		if (Input.GetKeyDown(KeyCode.P) && NumPaint > 0) {
//			NumPaint -= 1;
//			Instantiate(PaintSplat, transform.position, Quaternion.identity);
//		}
//		
//		if (Input.GetButtonDown("Fire1") && NumGlowsticks > 0) {		
//			NumGlowsticks -= 1;
//			Instantiate(Glowstick, (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition), Quaternion.identity);
//		}
	}
	
	//update is called every frame at fixed intervals
	void FixedUpdate()
	{
		float speed;
		if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
			speed = RunSpeed;
		} else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) {
			speed = SlowSpeed;
		} else if (Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0){
			speed = WalkSpeed;			
		} else {
			speed = IdleSpeed; // TODO: what should this be
		}

		Vector2 velocity = GetComponent<Rigidbody2D>().velocity;
		float velSpeed = velocity.magnitude;

		if (velSpeed > WalkSpeed) {
			anim.SetInteger(MovementMode, (int)AnimState.Run);
			audio.clip = RunSound;
		} else if (velSpeed > SlowSpeed) {
			anim.SetInteger(MovementMode, (int)AnimState.Walk);
			audio.clip = WalkSound; 
		} else if (velSpeed > 0){
			anim.SetInteger(MovementMode, (int)AnimState.Sneak);
			audio.clip = StealthSound;
		} else {
			anim.SetInteger(MovementMode, (int)AnimState.Idle);
			audio.Stop();
		}
	
		if (!audio.isPlaying)
			audio.Play ();

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
