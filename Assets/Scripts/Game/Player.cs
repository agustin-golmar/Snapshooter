using UnityEngine;

	/**
	* El jugador local.
	*/

public class Player : MonoBehaviour {

	// Comandos de movimiento:
	private const string KEY_W = "w";
	private const string KEY_A = "a";
	private const string KEY_S = "s";
	private const string KEY_D = "d";

	protected Configuration config;
	protected Snapshot snapshot;
	protected Client client;
	protected int id;

	/**
	* Obtiene la configuración global.
	*/
	protected void Awake() {
		config = GameObject
			.Find("Configuration")
			.GetComponent<Configuration>();
	}

	/**
	* Envía un comando de movimiento al servidor.
	*/
	protected void Move() {
		if (Input.GetKey(KEY_W)) {
			client.Move(Direction.FORWARD);
		}
		if (Input.GetKey(KEY_A)) {
			client.Move(Direction.STRAFING_LEFT);
		}
		if (Input.GetKey(KEY_S)) {
			client.Move(Direction.BACKWARD);
		}
		if (Input.GetKey(KEY_D)) {
			client.Move(Direction.STRAFING_RIGHT);
		}
	}

	/**
	* Aplica movimiento sobre el jugador (en caso de utilizar prediction).
	*/
	protected void PredictMove() {
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
	* Actualiza el estado del jugador desde la snapshot. En caso de utilizar
	* predicción, se debe comparar el estado contra la predicción. Si hay una
	* diferencia substancial, se aplica una corrección.
	*/
	protected void Update() {
		Move();
		if (config.usePrediction) {
			PredictMove();
			// Contra qué frame debería comparar?
			if (config.ΔPosition < Vector3.Distance(transform.position, snapshot.transforms[id].position)) {
				Debug.Log("Corrigiendo posición (" + Vector3.Distance(transform.position, snapshot.transforms[id].position) + ").");
				transform.position = snapshot.transforms[id].position;
			}
			if (config.ΔRotation < Quaternion.Angle(transform.rotation, snapshot.transforms[id].rotation)) {
				Debug.Log("Corrigiendo rotación (" + Quaternion.Angle(transform.rotation, snapshot.transforms[id].rotation) + ").");
				transform.rotation = snapshot.transforms[id].rotation;
			}
		}
		else {
			// Actualizar vida en HUD.
			// snapshot.lifes[id];
			transform.SetPositionAndRotation(snapshot.transforms[id].position, snapshot.transforms[id].rotation);
		}
	}

	/**
	* Actualiza la posición de la cámara. Debe ser en 1ra persona.
	*/
	protected void LateUpdate() {
		Camera.main.transform.LookAt(transform);
	}

	/** **********************************************************************
	******************************* PUBLIC API ********************************
	 *********************************************************************** */

	public Player SetClient(Client client) {
		this.client = client;
		return this;
	}

	public Player SetID(int id) {
		this.id = id;
		return this;
	}

	public Player SetSnapshot(Snapshot snapshot) {
		this.snapshot = snapshot;
		return this;
	}
}
