
	using System.Collections.Generic;
	using UnityEngine;

		/*
		* Interpolador de snapshots.
		*/

	public class SnapshotInterpolator {

		protected Queue<Snapshot> snapshots;
		protected int windowSize;
		protected int sps;
		protected int lastSequence;
		protected float Δs;

		// Testing...
		protected float baseTime;

		public SnapshotInterpolator(int windowSize, int sps) {
			this.windowSize = windowSize;
			this.sps = sps;
			snapshots = new Queue<Snapshot>();
			lastSequence = -1;
			Δs = 1.0f/sps;
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
				Snapshot snapshot = new Snapshot()
					.SetSequence(sequence)
					.SetPosition(packet.GetVector())
					.SetRotation(packet.GetQuaternion());
				snapshots.Enqueue(snapshot);
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

		// Devuelve una nueva snapshot interpolada, para el tiempo actual.
		// Cuánto debe ser Count? Debe ser windowSize?
		// Qué pasa cuando hay packet-loss?
		public Snapshot GetSnapshot() {
			// Computar el número de secuencia correcto para el tiempo actual.
			float currentTime = Time.fixedUnscaledTime;
			float Δt = currentTime - baseTime;
			int sequence = (int) Mathf.Floor(Δt/Δs);
			Debug.Log("Secuencia computada esperada: " + sequence);
			Debug.Log("última secuencia disponible: " + sequence);
			// Según el número, ubicar las snapshots laterales:
			// Para el número s = 2, se toma 2 y 3:
			//		S0 - S1 - (S2 - S3) - S4 - ... sería sequence y secuence + 1...
			// Se calcula dentro del intervalo el tiempo exacto (normalizado?). Es Δt...
			// Se aplican todas las interpolaciones en ese rango.
			// Cómo influye la ventana?
			if (0 < snapshots.Count) {
				Snapshot snapshot = snapshots.Dequeue();
				/*Snapshot interpolated = new Snapshot()
					.SetSequence(sequence)
					.SetPosition(Vector3.Lerp(snapshot.GetPosition, null, ));*/
				return snapshot;
			}
			else return null;
		}
	}
