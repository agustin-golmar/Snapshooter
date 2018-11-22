using UnityEngine;

	/**
	* Representa un enemigo remoto, exclusivamente controlado por un cliente
	* externo, cuyos datos provienen de las snapshots.
	*/

public class Enemy : MonoBehaviour {

	// El estado del mundo:
	protected Snapshot snapshot;
	protected int id;

	protected void Start() {
		Debug.Log("Enemy created (id = " + id + ").");
	}

	protected void Update() {
		transform.SetPositionAndRotation(snapshot.positions[id], snapshot.rotations[id]);
	}

	/** **********************************************************************
	******************************* PUBLIC API ********************************
	 *********************************************************************** */

	public Enemy SetID(int id) {
		this.id = id;
		return this;
	}

	public Enemy SetSnapshot(Snapshot snapshot) {
		this.snapshot = snapshot;
		return this;
	}
}
