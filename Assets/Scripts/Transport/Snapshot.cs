
	using UnityEngine;

		/*
		* Representa el estado dinámico del sistema, que debe serializarse.
		*/

	public class Snapshot {

		// N° de secuencia de la snapshot:
		protected int sequence;

		// Propiedades del jugador:
		protected Vector3 position;
		protected Quaternion rotation;

		// Otros datos:
		// ...

		public int GetSequence() {
			return sequence;
		}

		public Vector3 GetPosition() {
			return position;
		}

		public Quaternion GetRotation() {
			return rotation;
		}

		public Snapshot SetSequence(int sequence) {
			this.sequence = sequence;
			return this;
		}

		public Snapshot SetPosition(Vector3 position) {
			this.position = position;
			return this;
		}

		public Snapshot SetRotation(Quaternion rotation) {
			this.rotation = rotation;
			return this;
		}
	}
