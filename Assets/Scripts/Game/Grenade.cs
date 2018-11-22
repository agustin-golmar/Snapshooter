using UnityEngine;

	/**
	* Comportamiento de una granada explosiva.
	*/

public class Grenade : MonoBehaviour {

	protected Configuration config;
	protected Snapshot snapshot;
	protected int id;

	protected float countdown;
	protected bool hasExploded;

	protected void Awake() {
		config = GameObject
			.Find("Configuration")
			.GetComponent<Configuration>();
	}

	protected void Start() {
		countdown = config.grenadeFuseTime;
		hasExploded = false;
	}

	protected void Update() {
		countdown = snapshot.gFuses[id];
		transform.SetPositionAndRotation(snapshot.gPositions[id], snapshot.gRotations[id]);
		if (countdown <= 0 && !hasExploded) {
			// Explotar...
			// Obtener vecinos
			Collider [] nearbyObjects = Physics.OverlapSphere(transform.position, config.grenadeRadius);
			foreach (Collider collider in nearbyObjects) {
				Enemy enemy = collider.GetComponent<Enemy>();
				if (enemy != null) {
					// Es un jugador válido...
					// Inferir daño
					// Es mejor computar diferencias entre centros contra todos los jugadores.
					// No es escalable, pero es simple y rápido.
				}
			}
			// Eliminar granada
			Destroy(gameObject);
			hasExploded = true;
		}
	}
}
