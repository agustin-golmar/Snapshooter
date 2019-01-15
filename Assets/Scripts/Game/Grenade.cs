using UnityEngine;

	/**
	* Comportamiento de una granada explosiva.
	*/

public class Grenade : MonoBehaviour {

	protected Configuration config;
	protected Snapshot snapshot;
	protected int id;

	protected void Awake() {
		config = GameObject
			.Find("Configuration")
			.GetComponent<Configuration>();
	}

	protected void Start() {
		transform.GetChild(0)
			.localScale = (config.grenadeRadius / transform.localScale.x) * Vector3.one;
	}

	protected void Update() {
		snapshot.gPositions[id] = transform.position;
		snapshot.gRotations[id] = transform.rotation;
		snapshot.gFuses[id] -= Time.unscaledDeltaTime;
		if (snapshot.gFuses[id] <= 0) {
			snapshot.gFuses[id] = -1;
			for (int k = 0; k < snapshot.players; ++k) {
				if (Vector3.Distance(snapshot.positions[k], snapshot.gPositions[id]) < config.grenadeRadius) {
					Debug.Log("The grenade explosion hit player with ID = " + k);
					snapshot.lifes[k] -= config.grenadeDamage;
				}
			}
			Destroy(gameObject);
		}
	}

	/** **********************************************************************
	******************************* PUBLIC API ********************************
	 *********************************************************************** */

	public Grenade SetID(int id) {
		this.id = id;
		return this;
	}

	public Grenade SetSnapshot(Snapshot snapshot) {
		this.snapshot = snapshot;
		return this;
	}

	/**
	* Lanza una granada en la dirección en la cual está orientada, con una
	* fuerza impulsiva inicial.
	*/
	public Grenade Throw() {
		Rigidbody body = GetComponent<Rigidbody>();
		body.AddForce(2.0f * (transform.forward + transform.up), ForceMode.Impulse);
		return this;
	}
}
