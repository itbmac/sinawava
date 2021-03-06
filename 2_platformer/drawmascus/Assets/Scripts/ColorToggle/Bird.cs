﻿using UnityEngine;
using System.Collections;

public class Bird : MonoBehaviour {

	public GameObject Feather;
	public float SnakeRadius = 2.0f;
	public float EscapeDistance = 10.0f;
	public float EscapeVelocity = 5.0f;

	private GameObject snake;
	
	private bool scared;
	private Vector2 startPos;
	private Animator anim;
	private bool isActing = false;

	public AudioClip Sound;

	// Use this for initialization
	void Start () {
		snake = FindObjectOfType<Snake>().gameObject;
		startPos = transform.position;
		anim = GetComponent<Animator>();
	}
	
	// Update is called once per frame
	void Update () {
		if (!scared) {
			if (Vector2.Distance(snake.transform.position, transform.position) < SnakeRadius) {
				scared = true;
				anim.SetBool("Flying", true);
				Instantiate(Feather, transform.position, Quaternion.identity);
			}
		} else {
			if (Vector2.Distance(startPos, transform.position) > EscapeDistance) {
				Destroy(gameObject);
			} else {
				if (!isActing){
					audio.PlayOneShot(Sound);
					isActing = true;
				}
				transform.position += new Vector3(1, 1, 0) * EscapeVelocity * Time.deltaTime;
			}
		}
	}
}
