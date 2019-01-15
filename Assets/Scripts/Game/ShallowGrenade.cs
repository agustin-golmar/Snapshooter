using UnityEngine;

	/**
	* Comportamiento de una granada explosiva, del lado del cliente.
	*/

public class ShallowGrenade : MonoBehaviour {

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
		transform.SetPositionAndRotation(snapshot.gPositions[id], snapshot.gRotations[id]);
		if (snapshot.gFuses[id] <= 0) {
			Destroy(gameObject);
		}
	}

	/** **********************************************************************
	******************************* PUBLIC API ********************************
	 *********************************************************************** */

	public ShallowGrenade SetID(int id) {
		this.id = id;
		return this;
	}

	public ShallowGrenade SetSnapshot(Snapshot snapshot) {
		this.snapshot = snapshot;
		return this;
	}
}
