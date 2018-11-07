using UnityEngine;

public class Player : MonoBehaviour {

	// Comandos de movimiento:
	private const string KEY_W = "w";
	private const string KEY_A = "a";
	private const string KEY_S = "s";
	private const string KEY_D = "d";

	protected Configuration config;
	protected Snapshot snapshot;

	/**
	* Aplica movimiento sobre el jugador (en caso de utilizar prediction).
	*/
	protected void Move() {
		float delta = config.playerSpeed * Time.deltaTime;
		if (Input.GetKey(KEY_W)) {
			transform.Translate(0, 0, delta);
		}
		if (Input.GetKey(KEY_A)) {
			transform.Rotate(Vector3.down, 30.0f * delta);
		}
		if (Input.GetKey(KEY_S)) {
			transform.Translate(0, 0, -delta);
		}
		if (Input.GetKey(KEY_D)) {
			transform.Rotate(Vector3.up, 30.0f * delta);
		}
	}

	/**
	* Falta validar que la predicción sea consistente con el estado de la
	* snapshot.
	*/
	protected void Update() {
		if (config.usePrediction) {
			Move();
		}
	}

	/**
	* Actualiza la posición de la cámara. Debe ser en 1ra persona.
	*/
	protected void LateUpdate() {
		Camera.main.transform.LookAt(transform);
	}
}
