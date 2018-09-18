
	using System.Collections.Generic;

		/*
		* Interpolador de snapshots.
		*/

	public class SnapshotInterpolator {

		protected Queue<Snapshot> snapshots;
		protected int windowSize;
		protected int sps;
		protected int lastSequence;

		public SnapshotInterpolator(int windowSize, int sps) {
			this.windowSize = windowSize;
			this.sps = sps;
			snapshots = new Queue<Snapshot>();
			lastSequence = -1;
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
			}
			packet.Reset();
			return this;
		}

		// Devuelve una nueva snapshot interpolada, para el tiempo actual.
		// Cuánto debe ser Count? Debe ser windowSize?
		// Qué pasa cuando hay packet-loss?
		public Snapshot GetSnapshot() {
			// Computar el número de secuencia correcto para el tiempo actual.
			// Según el número, ubicar las snapshots laterales:
			// Para el número s = 2, se toma 2 y 3:
			//		S0 - S1 - (S2 - S3) - S4 - ...
			// Se calcula dentro del intervalo el tiempo exacto (normalizado?).
			// Se aplican todas las interpolaciones en ese rango.
			// Cómo influye la ventana?
			if (0 < snapshots.Count) {
				return snapshots.Dequeue();
			}
			else return null;
		}
	}
