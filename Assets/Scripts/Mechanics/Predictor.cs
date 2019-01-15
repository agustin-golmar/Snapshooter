using System.Collections.Generic;
using UnityEngine;

	/**
	* Permite predecir acciones sobre un jugador, y corregir las variaciones
	* de las mismas con respecto a una replicación remota.
	*/

public class Predictor {

	protected Configuration config;
	protected Snapshot snapshot;
	protected Player player;
	protected int id;
	protected SortedDictionary<int, State> states;

	public Predictor(Configuration config, Snapshot snapshot, Player player) {
		this.config = config;
		this.snapshot = snapshot;
		this.player = player;
		id = player.GetID();
		states = new SortedDictionary<int, State>();
	}

	/**
	* Elimina los estados para secuencias antiguas.
	*/
	protected void Clean() {
		List<int> oldSequences = new List<int>();
		foreach (var entry in states) {
			if (entry.Key <= snapshot.acks[id]) {
				oldSequences.Add(entry.Key);
			}
		}
		foreach (int sequence in oldSequences) {
			states.Remove(sequence);
		}
	}

	/** **********************************************************************
	******************************* PUBLIC API ********************************
	 *********************************************************************** */

	/**
	* Crea un nuevo estado y lo almacena bajo cierto número de secuencia.
	*/
	public void SaveState(int sequence) {
		states[sequence] = new State()
			.AddTransform(player.transform);
	}

	/**
	* Predice el movimiento sobre un jugador en la dirección indicada.
	*/
	public void PredictMove(List<Direction> directions, float Δt) {
		float delta = Δt * config.playerSpeed;
		foreach (Direction direction in directions) {
			switch (direction) {
				case Direction.FORWARD : {
					player.transform.Translate(0, 0, delta);
					break;
				}
				case Direction.STRAFING_LEFT : {
					player.transform.Translate(-delta, 0, 0);
					break;
				}
				case Direction.BACKWARD : {
					player.transform.Translate(0, 0, -delta);
					break;
				}
				case Direction.STRAFING_RIGHT : {
					player.transform.Translate(delta, 0, 0);
					break;
				}
				case Direction.ROTATE_RIGHT : {
					player.transform.Rotate(0, 10 * delta, 0);
					break;
				}
				case Direction.ROTATE_LEFT : {
					player.transform.Rotate(0, -10 * delta, 0);
					break;
				}
			}
		}
	}

	/**
	* Verifica el estado de la snapshot con respecto al estado actual del
	* jugador y aplica una corrección de ser necesaria.
	*/
	public void Validate() {
		if (states.TryGetValue(snapshot.acks[id], out State oldState)) {
			float ΔP = Vector3.Distance(oldState.position, snapshot.positions[id]);
			float ΔR = Quaternion.Angle(oldState.rotation, snapshot.rotations[id]);
			// Corregir si es necesario:
			if (config.ΔPosition < ΔP) {
				Debug.Log("Corrigiendo posición. ΔP = " + ΔP);
				player.transform.position = snapshot.positions[id];
			}
			if (config.ΔRotation < ΔR) {
				Debug.Log("Corrigiendo rotación. ΔR = " + ΔR);
				player.transform.rotation = snapshot.rotations[id];
			}
			Clean();
		}
	}

	/**
	* El estado predicho, en algún momento dado.
	*/
	public class State {

		public Vector3 position;
		public Quaternion rotation;

		public State AddTransform(Transform transform) {
			position = new Vector3(transform.position.x, transform.position.y, transform.position.z);
			rotation = new Quaternion(transform.rotation.x, transform.rotation.y, transform.rotation.z, transform.rotation.w);
			return this;
		}
	}
}
