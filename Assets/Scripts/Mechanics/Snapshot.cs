
	using UnityEngine;

		/*
		* Representa el estado dinámico del sistema, que debe serializarse.
		*/

	public class Snapshot {

		// Timestamp y N° de secuencia de la snapshot:
		protected readonly float timestamp;
		protected readonly int sequence;

		// Propiedades del jugador:
		protected readonly Vector3 position;
		protected readonly Quaternion rotation;

		// Otros datos:
		// ...

		public Snapshot(Builder builder) {
			timestamp = builder.timestamp;
			sequence = builder.sequence;
			position = builder.position;
			rotation = builder.rotation;
		}

		public float GetTimestamp() {
			return timestamp;
		}

		public int GetSequence() {
			return sequence;
		}

		public Vector3 GetPosition() {
			return position;
		}

		public Quaternion GetRotation() {
			return rotation;
		}

		public class Builder {

			public float timestamp;
			public int sequence;
			public Vector3 position;
			public Quaternion rotation;

			public Builder Timestamp(float timestamp) {
				this.timestamp = timestamp;
				return this;
			}

			public Builder Sequence(int sequence) {
				this.sequence = sequence;
				return this;
			}

			public Builder Position(Vector3 position) {
				this.position = position;
				return this;
			}

			public Builder Rotation(Quaternion rotation) {
				this.rotation = rotation;
				return this;
			}

			public Snapshot Build() {
				return new Snapshot(this);
			}
		}
	}
