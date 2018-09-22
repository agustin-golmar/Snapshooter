
	using System.Collections.Generic;
	using UnityEngine;

		/*
		* Interpolador de snapshots.
		*/

	public class SnapshotInterpolator {

		protected readonly Queue<Snapshot> snapshots;
		protected readonly int windowSize;
		protected readonly int sps;
		protected readonly float Δs;

		// Testing...
		protected int lastSequence;
		protected float baseTime;
		protected Snapshot left;
		protected Snapshot right;

		public SnapshotInterpolator(int windowSize, int sps) {
			this.windowSize = windowSize;
			this.sps = sps;
			snapshots = new Queue<Snapshot>();
			lastSequence = -1;
			Δs = 1.0f/sps;
			left = null;
			right = null;
			baseTime = -1.0f;
		}

		public SnapshotInterpolator SetBaseTime(float baseTime) {
			this.baseTime = baseTime;
			return this;
		}

		public SnapshotInterpolator AddPacket(Packet packet) {
			packet.Reset(1); // Jump packet-type
			int sequence = packet.GetInteger();
			if (lastSequence < sequence) {
				lastSequence = sequence;
				Snapshot snapshot = new Snapshot.Builder()
					.Sequence(sequence)
					.Timestamp(packet.GetFloat())
					.Position(packet.GetVector())
					.Rotation(packet.GetQuaternion())
					.Build();
				snapshots.Enqueue(snapshot);
				if (baseTime < 0.0f) {
					baseTime = snapshot.GetTimestamp();
					Debug.Log("\t\tSe seteó el primer timestamp a " + baseTime + " segs.");
				}
				if (windowSize < snapshots.Count) {
					// Solo se retienen 'windowSize' snapshots...
					snapshots.Dequeue();
					Debug.Log("Se eliminó una snapshot antigua.");
				}
			}
			else {
				// Drop packet...
			}
			packet.Reset();
			return this;
		}

		public Snapshot GetSnapshot() {
			if (0 < snapshots.Count) {
				// No interpola en lo absoluto...
				return snapshots.Dequeue();
			}
			else return null;
		}

		/*public SnapshotInterpolator CleanOldSnapshots(int sequence) {
			// Revisar las secuencias en la cola.
			// Si la secuencia del tope es menor por 1, cargarla en left.
			// Cargar el siguiente en right.
			while (0 < snapshots.Count) {
				Snapshot snapshot = snapshots.Peek();
				if (snapshot.GetSequence() == sequence - 1) {
					left = snapshots.Dequeue();
				}
				else if (snapshot.GetSequence() < sequence) {
					snapshots.Dequeue();
				}
			}
			return this;
		}*/

		// Qué pasa cuando hay packet-loss?
		/*public Snapshot GetSnapshot() {
			// Computar el número de secuencia correcto para el tiempo actual.
			float currentTime = Time.fixedUnscaledTime;
			float Δt = currentTime - baseTime; // Puede ser negativo!
			float n = Δt/Δs;
			Debug.Log("Δt = " + Δt);
			int sequence = (int) Mathf.Floor(n);
			Debug.Log("Secuencia computada esperada: " + sequence);
			Debug.Log("Última secuencia disponible: " + lastSequence);

			if (left == null && right == null) {
				if (1 < snapshots.Count) {
					// Al menos debe haber 2...
					right = snapshots.Dequeue();
					left = snapshots.Dequeue();
				}
				else return null;
			}
			else {
				// Solo right es nulo;
			}
			Debug.Log("\t\t(baseTime, timestamp) = (" + baseTime + ", " + left.GetTimestamp() + ")");

			// En este punto, left y right poseen snapshots.
			if (sequence < left.GetSequence()) {
				// La secuencia necesaria es muy vieja. No sé si llega a pasar...
				Debug.Log("Secuencia requerida muy vieja!: " + sequence);
				Debug.Log("Última secuencia es: " + lastSequence);
			}
			else if (right.GetSequence() < sequence) {
				// Se requiere una secuencia más nueva.
				Debug.Log("Secuencia requerida muy nueva!: " + sequence);
				Debug.Log("Última secuencia es: " + lastSequence);
				CleanOldSnapshots(sequence);
				// Debo avanzar a un par de snapshots correctas...
			}
			else {
				// Entre left y right.
				Debug.Log("La secuencia se encuentra entre left y right, como era esperado...");
				Debug.Log("(l, s, r) = (" + left.GetSequence() + ", " + sequence + ", " + right.GetSequence() + ")");
			}

			float Δn = Δt - sequence * Δs;
			Debug.Log("Clamped Δn: " + Δn + "\n");
			return new Snapshot()
				.Sequence(sequence)
				.Timestamp(Δt)
				.Position(Vector3.Lerp(left.GetPosition(), right.GetPosition(), Δn))
				.Rotation(Quaternion.Slerp(left.GetRotation(), right.GetRotation(), Δn))
				.Build();
		}*/
	}
