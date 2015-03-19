﻿using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour {

	public float HorizontalSpeed = 20.0f;
	public float JumpSpeed = 24.0f;
	public float NormalGravity = 12.0f;
	public float JumpGravity = 4.0f;

	public Color CurrentColor {
		get {
			var sr = (SpriteRenderer)renderer;
			return sr.color;
		}
		
		set {
			var sr = (SpriteRenderer)renderer;
			sr.color = value;
		}
	}
	
	public AudioClip WalkSound;
	public AudioClip JumpSound;
	public AudioClip IdleSound;
	public AudioClip ColorSwitchSound;
	public AudioClip LandSound;
	
	bool IsJumping;
	Animator Anim;
	
	private void UpdateLocalScale() {
		Vector3 newLocalScale = transform.localScale;
		if (FacingRight)
			newLocalScale.x = Mathf.Abs (newLocalScale.x);
		else
			newLocalScale.x = -Mathf.Abs (newLocalScale.x);
		transform.localScale = newLocalScale;
	}
	
	private bool _facingRight; // do not change directly outside of FacingRight property
	private bool FacingRight {
		set {
			if (value == _facingRight) return;
			_facingRight = value;
			UpdateLocalScale();
		}
		
		get {
			return _facingRight;
		}
	}
	
	public static Player Instance {
		get; private set;
	}
	
	void Awake() {
		Instance = this;
	}

	// Use this for initialization
	void Start () {
		Anim = GetComponent<Animator>();
	}
	
	void Update() {		
		Vector2 pos = transform.position;
		Vector2 groundPos = pos - new Vector2(0, collider2D.bounds.extents.y + .1f);
		Vector2 velocity = rigidbody2D.velocity;
		
		// check every layer except player and real
		int layerMask = ~LayerMask.GetMask("Player", "Real");
		
		var bounds = collider2D.bounds;
		var bottomLeft = new Vector2(bounds.min.x + bounds.size.x * 0.1f, bounds.min.y);
		var underBottomRight = new Vector2(bounds.max.x - bounds.size.x * 0.1f, bounds.min.y - bounds.size.y * 0.1f);
		bool grounded = Physics2D.OverlapArea(bottomLeft, underBottomRight, layerMask);
//		Debug.DrawRay(pos, groundPos - pos);

		bool IsJumpingNow = !grounded;
		Anim.SetBool("Jumping", IsJumpingNow);
		
		if (IsJumpingNow && !IsJumping) {
		
		}
		
		
		if (grounded) {
			if (Input.GetAxisRaw("Vertical") > 0)
				// jump triggered
				velocity.y = JumpSpeed;
			rigidbody2D.gravityScale = JumpGravity;
		} else {
			if (Input.GetAxisRaw("Vertical") == 0) {
				rigidbody2D.gravityScale = NormalGravity;
			}
		}

		// Don't change direction on 0 or avatar will awkwardly face
		// one way if no key is pressed
		float horizontal = Input.GetAxis("Horizontal");
		if (horizontal > 0)
			FacingRight = true;
		else if (horizontal < 0)
			FacingRight = false;
		
		bool walking = horizontal != 0;
		Anim.SetBool("Walking", walking);
		
		velocity.x = horizontal * HorizontalSpeed;
		rigidbody2D.velocity = velocity;
		
		Vector2 bottom = new Vector2(bounds.center.x, bounds.min.y - bounds.size.y * 0.5f);
		var hit = Physics2D.Linecast(bounds.center, bottom, ~LayerMask.GetMask("Player"));
		Debug.DrawRay(bounds.center, bottom - (Vector2)bounds.center, Color.blue);
		
		Vector2 targetUp;
		if (hit) {
			targetUp = hit.normal;
		} else {
			targetUp = Vector2.up;
		}
		// Fix orientation so wolf doesn't end up upside down.
		targetUp.y = Mathf.Max (targetUp.y, 0.9f);
		
		transform.up = Vector2.Lerp(transform.up, targetUp, 0.5f);
		
		if (IsJumpingNow) {
			if (!IsJumping) {
				audio.Stop();
				audio.PlayOneShot(JumpSound);
			}
		} else if (!IsJumpingNow && IsJumping) {
			audio.Stop();
			audio.PlayOneShot(LandSound);
		} else if (walking) {
			if (audio.clip != WalkSound || !audio.isPlaying) {
				audio.clip = WalkSound;
				audio.loop = true;
				audio.Play();
			}
		} else {
			if (audio.clip != IdleSound || !audio.isPlaying) {
				audio.clip = IdleSound;
				audio.loop = true;
				audio.Play();
			}
		}
		
		IsJumping = IsJumpingNow;
		
		if (Input.GetButtonDown("ColorToggle")) {
			Collider2D[] overlapping = Physics2D.OverlapCircleAll(
				transform.position,
				bounds.size.magnitude/2 + 0.2f,
				LayerMask.GetMask("Real", "Drawing")
			);
			
			foreach (var collider in overlapping) {
				var colorToggle = collider.GetComponent<ColorToggle>();
				if (colorToggle == null)
					continue;
					
				audio.PlayOneShot(ColorSwitchSound);
				
				if (CurrentColor == Color.white){
					colorToggle.TakeColor();
				}
				//If the wolf is currently colored, then it is trying to give a color.
				else{
					colorToggle.GiveColor();
				}
				
				break;
			}
		}
	}
	
	Vector2 GetForward() {
		if (FacingRight)
			return -transform.right;
		else
			return transform.right;
	}
}
