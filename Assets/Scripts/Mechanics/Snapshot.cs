using UnityEngine;

	/*
	* Representa el estado dinámico del sistema, el cual debe serializarse.
	* Contiene toda la información del estado del mundo.
	*/

public class Snapshot {

	// Timestamp y N° de secuencia de la snapshot:
	protected int sequence;
	protected float timestamp;

	// Propiedades de los jugadores:
	protected Vector3 position;
	protected Quaternion rotation;

	// Otros datos:
	// ...

	public Snapshot() {
	}

	public Snapshot(Builder builder) {
		sequence = builder.sequence;
		timestamp = builder.timestamp;
		position = builder.position;
		rotation = builder.rotation;
	}

	public Snapshot(Packet packet) {
		packet.Reset(1);
		sequence = packet.GetInteger();
		timestamp = packet.GetFloat();
		position = packet.GetVector();
		rotation = packet.GetQuaternion();
		packet.Reset();
	}

	public Snapshot(Snapshot from, Snapshot to, float Δn) {
		sequence = from.GetSequence();
		timestamp = Time.unscaledTime;
		position = Vector3.Lerp(from.GetPosition(), to.GetPosition(), Δn);
		rotation = Quaternion.Slerp(from.GetRotation(), to.GetRotation(), Δn);
	}

	public int GetSequence() {
		return sequence;
	}

	public float GetTimestamp() {
		return timestamp;
	}

	public Vector3 GetPosition() {
		return position;
	}

	public Quaternion GetRotation() {
		return rotation;
	}

	public class Builder {

		public int sequence;
		public float timestamp;
		public Vector3 position;
		public Quaternion rotation;

		public Builder Sequence(int sequence) {
			this.sequence = sequence;
			return this;
		}

		public Builder Timestamp(float timestamp) {
			this.timestamp = timestamp;
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
