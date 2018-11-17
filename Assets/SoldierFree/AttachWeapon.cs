using UnityEngine;
using System.Collections;

public class AttachWeapon : MonoBehaviour {
	public Transform attachPoint;
	public Transform Weapon;
	// Use this for initialization
	void Start () {
		Weapon.parent = attachPoint;
		Weapon.position = attachPoint.position;
		Weapon.rotation = attachPoint.rotation;
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
