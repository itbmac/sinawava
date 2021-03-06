﻿using UnityEngine;
//using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

//example. moving between some points at random
[RequireComponent(typeof(PolyNavAgent))]
public class Police : MyMonoBehaviour {

	const float PingPongRate = 180.0f;
	const float PlayerSeenTimeBeforeArrest = 1.5f;
	const float PlayerDetectedTime = 5.0f;
	const float DetectedInvestigativeRadius = 1.5f;
	const float DetectionRange = 7.0f;
	const float HealthToDrain = 1.0f; // per second

	public AudioClip PlayerSeen;
	public float InvestigativeSpeedMultiplier = 2.0f;
	const float CommunicationRange = 0.0f;
	public bool DebugMode;
	public bool DogMode;
	public bool WilliamMode;
	public GameObject CircleNotifierPrefab;
	
	public enum State {Normal, PlayerDetected, PlayerVisible}
	
	private State _currentState;
	public State CurrentState {
		get {
			return _currentState;
		}
		
		set {
			if (value == _currentState)
				return;
		
			if (value == State.Normal) {
				agent.maxSpeed = maxSpeed;
				SetNewDestination();
			} else {
				pingPongMode = false;
				agent.maxSpeed = maxSpeed * InvestigativeSpeedMultiplier;

				float dist = Vector2.Distance(transform.position, player.transform.position); 
				
				if ((dist < DetectionRange) && _currentState == State.Normal && value == State.PlayerVisible) {
					GetComponent<AudioSource>().PlayOneShot(PlayerSeen);
					TheGameManager.Instance.Detected();
				}
			}
		
			_currentState = value;
		}
	}
	
	
	GameObject player;
	
	public Vector2[] WPoints;
	public bool ViceCopMode;
	
	Collider2D visionCone;
	
	private PolyNavAgent _agent;
	public PolyNavAgent agent{
		get 
		{
			if (!_agent)
				_agent = GetComponent<PolyNavAgent>();
			return _agent;			
		}
	}
	
	public int WPointsIndex = 0;
	
	Vector2 playerLastSeenPos;
	float playerLastSeenTime;
	
	void OnEnable(){
		agent.OnDestinationReached += SetNewDestination;
		agent.OnDestinationInvalid += OnDestinationInvalid;
	}
	
	void OnDisable(){
		agent.OnDestinationReached -= SetNewDestination;
		agent.OnDestinationInvalid -= OnDestinationInvalid;
	}
	
	public Vector2 Direction {
		get; private set;
	}
	Vector2 pingPongBaseDirection;
	float pingPongStartTime;
	float pingPongEndTime;
	bool pingPongMode;
	
	float nextChange;
	void Update() {	
		anim.SetBool("Walking", !TheGameManager.Instance.MotionStopped);
	
		if (TheGameManager.Instance.MotionStopped) {
			return;
		}

		bool IsUnderStreetlight = GetComponent<Collider2D>().IsTouchingLayers(LayerMask.GetMask("Streetlight"));	
		
		if (IsUnderStreetlight) {
			color = Color.white;
		} else {
			color = new Color(0.7f,0.7f,0.7f);
		}
	
		if (pingPongMode && CurrentState == State.Normal) {
			if (Time.time > pingPongEndTime) {
				pingPongMode = false; 
				Direction = pingPongBaseDirection; // This might be jittery?
				SetNewDestination();
			} else {
				float dAngle = Mathf.PingPong(PingPongRate*(Time.time - pingPongStartTime) + 90, 180) - 90; // ping pong from -90 to 90, starting at 0
				Direction = pingPongBaseDirection.Rotated(dAngle * Mathf.Deg2Rad);
			}				
			
		} else {
			if (GetComponent<PolyNavAgent>().movingDirection != Vector2.zero)
				Direction = GetComponent<PolyNavAgent>().movingDirection;
		}
		
		bool playerVisible = IsPlayerVisible();
		
		if (playerVisible) {
			player.GetComponent<Player>().Health = Mathf.Max(0.0f, player.GetComponent<Player>().Health - HealthToDrain * Time.deltaTime);
		}
		
		if (CurrentState == State.Normal) {
			if (playerVisible) {
				CurrentState = State.PlayerVisible;
			} else if (ViceCopMode && Time.time > nextChange)
				SetNewDestination();	
		} else {			
			if (CurrentState == State.PlayerVisible) {
				playerLastSeenTime = Time.time;
				playerLastSeenPos = player.transform.position;
				agent.SetDestination(playerLastSeenPos);
			
				if (playerVisible) {
					CommunicatePlayerPos();
				} else
					CurrentState = State.PlayerDetected;
			} else if (CurrentState == State.PlayerDetected) {
				if (Time.time - playerLastSeenTime > PlayerDetectedTime)
					CurrentState = State.Normal;
				else if (playerVisible) {
					playerLastSeenTime = Time.time;
					CurrentState = State.PlayerVisible;
					TheGameManager.Instance.Detected();
				}
			}
		}
	}
	
	float maxSpeed;
	Vector2 startPos;
	Collider2D hearingRadius;
	void Start(){
		if (PlayerSeen == null)
			PlayerSeen = (AudioClip)Resources.Load ("police-player-seen-edit");
			
		var policeLayer = LayerMask.NameToLayer("Police");
		if (gameObject.layer != policeLayer) {
			gameObject.layer = policeLayer;
			Debug.LogWarning("Fixing layer on " + name);
		}		
	
		player = FindObjectOfType<Player>().gameObject;		
		visionCone = transform.GetChild(0).GetComponent<Collider2D>();
		maxSpeed = agent.maxSpeed;
		startPos = transform.position;
		
		if (WPoints.Length > 0) {
			SetNewDestination();
		}
		
		var hearingRadiusGameObject = transform.Find("HearingRadius");
		if (hearingRadiusGameObject)
			hearingRadius = hearingRadiusGameObject.GetComponent<Collider2D>();
	}
	
	void SetNewDestination() {
		if (WilliamMode
			&& (WPointsIndex == WPoints.Length - 1) 
		    && (transform.Distance(WPoints[WPointsIndex]) < 20.0f)) {
			anim.SetBool("Walking", false);
			TargetSystem.Instance.waitTimeToLaunchNextLevel = 3.0f;
			TargetSystem.Instance.TList[TargetSystem.Instance.currentTarget].hasBeenPassed = true;
			return;
		}

		if (CurrentState == State.Normal) {	
			if (ViceCopMode) {
				//WPointsIndex = Random.Range(0, WPoints.Length); // disabled for being too complicated
				WPointsIndex = (WPointsIndex + 1) % WPoints.Length;	
				nextChange = Time.time + Random.Range(2.0F, 10.0F);
			} else {		
				WPointsIndex = (WPointsIndex + 1) % WPoints.Length;	
			}
			Vector2 dest = WPoints[WPointsIndex];
			
			if (Mathf.Approximately(dest.x, 1337)) {
				pingPongMode = true;
				pingPongBaseDirection = Direction;
				pingPongStartTime = Time.time;
				pingPongEndTime = Time.time + 360/PingPongRate;
			} else 				
				agent.SetDestination(startPos + dest);
		} else if (CurrentState == State.PlayerDetected) {			
			int maxIterations = 5;
			Vector2 potentialPos;
			do {
				potentialPos = playerLastSeenPos + Random.insideUnitCircle * DetectedInvestigativeRadius;
			} while (--maxIterations > 0 && Physics2D.OverlapPoint(potentialPos, LayerMask.GetMask("Obstacle")));
		
			agent.SetDestination(potentialPos);		
		}		
	}
	
	IEnumerator MoveRandomSoon() {
		yield return new WaitForSeconds(1);
		SetNewDestination();
	}
	
	void OnDestinationInvalid() {
		StartCoroutine(MoveRandomSoon());
	}
	
	void OnDrawGizmosSelected(){
		var pos = Application.isPlaying ? startPos : (Vector2)transform.position;
		
		for ( int i = 0; i < WPoints.Count(); i++) {
			int prev = i - 1;
			if (prev < 0)
				prev = WPoints.Count() - 1;
			Gizmos.DrawLine(WPoints[prev] + pos, WPoints[i] + pos);
			Gizmos.DrawSphere(WPoints[i] + pos, 0.15f);			
		}
	}
	
	bool IsPlayerVisible() {
		if (Player.Instance.StealthMode)
			return false;

		float dist = Vector2.Distance(transform.position, player.transform.position); 

		if (dist > DetectionRange)
			return false;

		if (Player.Instance.IsOnSprayPaint)
			return false;
	
		int layerMask = LayerMask.GetMask("Obstacle");		
		if (Physics2D.Linecast(transform.position, player.transform.position, layerMask)) {
			return false;
		}
			
		if (DogMode)
			return true;
		
		if (Player.Instance.IsUnderStreetlight && Vector2.Angle(-transform.up, player.transform.position - transform.position) < 45f) {
			return true;
		}
		
		var playerCollider = player.GetComponent<Collider2D>();
		if (hearingRadius && hearingRadius.IsTouching(playerCollider))
			return true;
			
		
		return visionCone.IsTouching(playerCollider);
	}
	
	float nextCommunicate;
	void CommunicatePlayerPos() {	
		if (WilliamMode)
			return;

		if (Time.time > nextCommunicate)
			nextCommunicate = Time.time + 2.0f;
		else
			return;
			
		var circleNotifier = (GameObject)Instantiate(CircleNotifierPrefab, transform.position, Quaternion.identity);
		circleNotifier.GetComponent<CircleNotifier>().LastSeenPosition = player.transform.position;
	}
	
	public void NotifyPlayerPos(Vector2 pos) {
		NotifyPlayerPos(pos, Time.time);
	}
	
	public void NotifyPlayerPos(Vector2 pos, float lastSeenTime) {
		if (WilliamMode)
			return;

		if (CurrentState != State.PlayerVisible && lastSeenTime > playerLastSeenTime) {
			playerLastSeenPos = pos;
			playerLastSeenTime = lastSeenTime;
			CurrentState = State.PlayerDetected;
		}
	}
	
	void OnCollisionEnter2D(Collision2D coll) {
		if (coll.gameObject.tag == "Player") {
			if (!Player.Instance.StealthMode)
				CurrentState = State.PlayerVisible;
		}
		
	}
}
