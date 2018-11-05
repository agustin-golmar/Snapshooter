using System.Collections.Generic;
using UnityEngine;

	/*
	* Interpolador de snapshots. Solo el cliente se encarga de utilizar este
	* sistema ya que el servidor posee información completa.
	*/

public class SnapshotInterpolator {

	protected readonly Queue<Snapshot> snapshots;
	protected readonly int window;
	protected readonly float Δs;

	protected float baseTime;
	protected float timestamp;
	protected int lastSequence;
	protected Snapshot from;
	protected Snapshot to;

	public SnapshotInterpolator(int windowSize, int sps) {
		window = windowSize;
		snapshots = new Queue<Snapshot>();
		lastSequence = -1;
		Δs = 1.0f/sps;
		timestamp = -1.0f;
		baseTime = -1.0f;
		from = null;
		to = null;
	}

	public SnapshotInterpolator AddPacket(Packet packet) {
		Snapshot snapshot = new Snapshot(packet);
		if (lastSequence < snapshot.sequence) {
			if (lastSequence < 0) {
				baseTime = Time.unscaledTime - GetTimeForSequence(snapshot.sequence);
				Debug.Log("\tBase time: " + baseTime);
			}
			lastSequence = snapshot.sequence;
			snapshots.Enqueue(snapshot);
			Debug.Log("\tSecuencia agregada: " + snapshot.sequence);
		}
		else {
			Debug.Log("\tSe eliminó un paquete porque su secuencia es antigua (" + snapshot.sequence + ", last = " + lastSequence + ").");
		}
		return this;
	}

	public SnapshotInterpolator AddPackets(Queue<Packet> packets) {
		if (lastSequence < 0) {
			while (1 < packets.Count) {
				packets.Dequeue();
			}
		}
		foreach (Packet packet in packets) {
			AddPacket(packet);
		}
		return this;
	}

	public Snapshot GetSnapshot() {
		// Agregar verificación de secuencias corridas y validar ventana.
		SlideWindow().TrySetTimestamp();
		if (CanInterpolate()) {
			float Δt = GetInterpolationDelta();
			float Δn = Δt/Δs;
			Debug.Log("\tDelta Δt = " + Δt);
			Debug.Log("\tClamp Δn = " + 100*Δn + " %");
			return new Snapshot(from, to, Δn);
		}
		else return null;
	}

	/// <summary>
	/// Indica si se puede aplicar o no una interpolación. Para que se
	/// aplique dicho proceso, deben existir dos snapshots, una por defecto
	/// y otra por exceso, en relación al tiempo de interpolación.
	/// </summary>
	/// <returns></returns>
	protected bool CanInterpolate() {
		return from != null && to != null;
	}

	/// <summary>
	/// Devuelve el tiempo dentro del intervalo de interpolación.
	/// </summary>
	protected float GetInterpolationDelta() {
		return GetInterpolationTime() - timestamp;
	}

	/// <summary>
	/// Devuelve el tiempo actual de interpolación, lo que implica tener en
	/// cuenta un tiempo de base, y un retraso provocado por la ventana de
	/// interpolación.
	/// </summary>
	/// <returns>El tiempo actual de interpolación.</returns>
	protected float GetInterpolationTime() {
		return Time.unscaledTime - baseTime - Δs;
	}

	/// <summary>
	/// Intenta hallar una snapshot cuyo tiempo sea inmediatamente anterior
	/// al indicado.
	/// </summary>
	/// <param name = "time">El tiempo indicado.</param>
	/// <returns>Una snapshot cuyo tiempo es mayor o igual al
		/// indicado.</returns>
	protected Snapshot GetSnapshotByDefault(float time) {
		Snapshot defaultSnapshot = null;
		while (0 < snapshots.Count) {
			Snapshot snapshot = snapshots.Peek();
			float t = GetTimeForSequence(snapshot.sequence);
			if (t <= time) {
				defaultSnapshot = snapshot;
				snapshots.Dequeue();
			}
			else break;
		}
		return defaultSnapshot;
	}

	/// <summary>
	/// Intenta hallar una snapshot cuyo tiempo asociado se encuentre
	/// inmediatamente por encima del especificado.
	/// </summary>
	/// <param name = "time">El tiempo especificado.</param>
	/// <returns>Una snapshot cuyo tiempo sea estrictamente mayor al
	/// indicado, o null en otro caso.</returns>
	protected Snapshot GetSnapshotByExcess(float time) {
		Snapshot excessSnapshot = null;
		while (0 < snapshots.Count) {
			Snapshot snapshot = snapshots.Peek();
			float t = GetTimeForSequence(snapshot.sequence);
			if (time < t) {
				excessSnapshot = snapshot;
				snapshots.Dequeue();
				break;
			}
			else snapshots.Dequeue();
		}
		return excessSnapshot;
	}

	/// <summary>
	/// Devuelve el tiempo asociado al número de secuencia indicado, como
	/// múltiplos del factor Δs.
	/// </summary>
	/// <param name = "sequence">El número de secuencia.</param>
	/// <returns>El tiempo en segundos.</returns>
	protected float GetTimeForSequence(int sequence) {
		return sequence * Δs;
	}

	protected SnapshotInterpolator SlideWindow() {
		float t = GetInterpolationTime();
		Debug.Log("\tInterpolation time: " + t);
		if (from == null) {
			from = GetSnapshotByDefault(t);
			if (from != null) Debug.Log("\t\tNew default time (from): " + GetTimeForSequence(from.sequence));
			else Debug.Log("\t\tNo se puede encontrar una snapshot por defecto (from).");
		}
		else {
			float fromTime = GetTimeForSequence(from.sequence);
			if (fromTime <= t && t < fromTime + Δs) {
				Debug.Log("\t\tDefault (from), todavía no venció: " + fromTime);
			}
			else {
				SwitchSnapshots();
			}
		}
		if (from != null) {
			if (to == null) {
				to = GetSnapshotByExcess(t);
				if (to != null) Debug.Log("\t\tNew excess time (to): " + GetTimeForSequence(to.sequence));
				else Debug.Log("\t\tNo se puede encontrar una snapshot por exceso (to).");
			}
			else {
				float toTime = GetTimeForSequence(to.sequence);
				if (t < toTime) {
					Debug.Log("\t\tExcess (to), todavía no venció: " + toTime);
				}
				else {
					to = GetSnapshotByExcess(t);
					if (to != null) Debug.Log("\t\tNew excess time (to): " + GetTimeForSequence(from.sequence));
					else Debug.Log("\t\tNo se puede encontrar una snapshot por exceso (to).");
				}
			}
		}
		return this;
	}

	/// <summary>
	/// Intercambia las snapshots por defecto y exceso, para que la
	/// interpolación avance correctamente.
	/// </summary>
	protected SnapshotInterpolator SwitchSnapshots() {
		timestamp = -1.0f;
		from = to;
		to = null;
		return SlideWindow().TrySetTimestamp();
	}

	/// <summary>
	/// En caso de que se pueda aplicar una interpolación en los próximos
	/// intervalos, se computa el tiempo base para esa interpolación. Esto
	/// permite computar el punto de interpolación entre dos snapshots.
	/// </summary>
	protected SnapshotInterpolator TrySetTimestamp() {
		if (CanInterpolate() && timestamp < 0) {
			timestamp = GetInterpolationTime();
		}
		return this;
	}
}
