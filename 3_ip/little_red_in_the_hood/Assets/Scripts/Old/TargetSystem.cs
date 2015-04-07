﻿using UnityEngine;
using System.Collections;

public class TargetSystem : MonoBehaviour {

	public GameObject target0;
	public GameObject target1;
	public GameObject target2;
	public GameObject target3;
	public GameObject target4;
	public GameObject target5;
	public GameObject target6;
	public GameObject target7;
	
	public GameObject CurrentTarget {
		get {
			return TOList[currentTarget];
		}
	}

	GameObject[] TOList;
	TargetScript[] TList;

	public int currentTarget;
	public int numTargets = 8;
	
	public static TargetSystem Instance {
		get; private set;
	}
	
	void Awake() {
		Instance = this;
	}

	// Use this for initialization
	void Start () {
		TList = new TargetScript[8];
		TList[0] = target0.GetComponent<TargetScript>();
		TList[1] = target1.GetComponent<TargetScript>();
		TList[2] = target2.GetComponent<TargetScript>();
		TList[3] = target3.GetComponent<TargetScript>();
		TList[4] = target4.GetComponent<TargetScript>();
		TList[5] = target5.GetComponent<TargetScript>();
		TList[6] = target6.GetComponent<TargetScript>();
		TList[7] = target7.GetComponent<TargetScript>();

		TOList = new GameObject[8];
		TOList[0] = target0;
		TOList[1] = target1;
		TOList[2] = target2;
		TOList[3] = target3;
		TOList[4] = target4;
		TOList[5] = target5;
		TOList[6] = target6;
		TOList[7] = target7;

		currentTarget = numTargets-1;
		TList[currentTarget].hasBeenPassed = true;
	}
	
	// Update is called once per frame
	void Update () {
		if (TList[currentTarget].hasBeenPassed) {
			if (currentTarget+1 == numTargets) {
				GameObject[] playerz = GameObject.FindGameObjectsWithTag("Player");
				playerz[0].GetComponent<Player>().ResetPosToStart();
			}

			currentTarget = (currentTarget+1)%numTargets;
			SetCurrentTarget();
		}
	}

	void SetCurrentTarget() {
		TList[currentTarget].SetAsCurrentTarget(true);
	}
}
