﻿using UnityEngine;
using System.Collections;

public class InitialVelocity : MonoBehaviour {
    static Rigidbody Ion;
	// Use this for initialization
	void Start () {
        gameObject.GetComponent<Rigidbody>().velocity = new Vector3(Random.Range(-20, 20), Random.Range(-20, 20), Random.Range(-20, 20));
    }
}
