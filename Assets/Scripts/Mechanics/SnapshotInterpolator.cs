using System.Collections.Generic;
using UnityEngine;

	/**
	* Interpolador de snapshots. Solo el cliente se encarga de utilizar este
	* sistema ya que el servidor posee información completa.
	*/

public class SnapshotInterpolator {

	protected readonly Queue<Snapshot> snapshots;
	protected readonly int window;
	protected readonly float Δs;
	protected readonly Snapshot now;

	protected float baseTime;
	protected float timestamp;
	protected int lastSequence;
	protected Snapshot from;
	protected Snapshot to;

	public SnapshotInterpolator(Builder builder) {
		snapshots = new Queue<Snapshot>();
		window = builder.window;
		Δs = 1.0f/builder.sps;
		baseTime = -1.0f;
		timestamp = -1.0f;
		lastSequence = -1;
		from = null;
		now = builder.snapshot;
		to = null;
	}

	/**
	* Indica si se puede aplicar o no una interpolación. Para que se aplique
	* dicho proceso, deben existir dos snapshots, una por defecto y otra por
	* exceso, en relación al tiempo de interpolación.
	*/
	protected bool CanInterpolate() {
		return from != null && to != null;
	}

	/**
	* Devuelve el tiempo asociado al número de secuencia indicado, como
	* múltiplos del factor Δs.
	*/
	public float GetTimeForSequence(int sequence) {
		return sequence * Δs;
	}

	/**
	* Devuelve el tiempo actual de interpolación, lo que implica tener en
	* cuenta un tiempo de base, y un retraso provocado por la ventana de
	* interpolación.
	*/
	public float GetInterpolationTime() {
		// Verifica si Δs debería multiplicarse por algún factor de la ventana.
		return Time.unscaledTime - baseTime - Δs;
	}

	/**
	* Devuelve el tiempo dentro del intervalo de interpolación.
	*/
	protected float GetInterpolationDelta() {
		return GetInterpolationTime() - timestamp;
	}

	/**
	* Intenta hallar una snapshot cuyo tiempo sea inmediatamente anterior al
	* indicado (por defecto). El tiempo debe ser mayor o igual al indicado.
	* Devuelve null si no encuentra una snapshot adecuada.
	*/
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

	/**
	* Intenta hallar una snapshot cuyo tiempo asociado se encuentre
	* inmediatamente por encima del especificado (por exceso). El tiempo debe
	* ser estrictamente mayor. Devuelve null si no encuentra una snapshot
	* disponible.
	*/
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

	/**
	* Desplaza la ventana actual de interpolación, para el tiempo actual.
	*/
	protected SnapshotInterpolator SlideWindow() {
		float t = GetInterpolationTime();
		if (from == null) {
			from = GetSnapshotByDefault(t);
		}
		else {
			float fromTime = GetTimeForSequence(from.sequence);
			if (fromTime <= t && t < fromTime + Δs) {
				// El tiempo por defecto (from), no venció.
			}
			else {
				SwitchSnapshots();
			}
		}
		if (from != null) {
			if (to == null) {
				to = GetSnapshotByExcess(t);
			}
			else {
				float toTime = GetTimeForSequence(to.sequence);
				if (t < toTime) {
					// El tiempo por exceso (to), no venció.
				}
				else {
					to = GetSnapshotByExcess(t);
				}
			}
		}
		return this;
	}

	/**
	* Intercambia las snapshots por defecto y exceso, para que la interpolación
	* avance correctamente.
	*/
	protected SnapshotInterpolator SwitchSnapshots() {
		timestamp = -1.0f;
		from = to;
		to = null;
		return SlideWindow().TrySetTimestamp();
	}

	/**
	* En caso de que se pueda aplicar una interpolación en los próximos
	* intervalos, se computa el tiempo base para esa interpolación. Esto
	* permite computar el punto de interpolación entre dos snapshots.
	*/
	protected SnapshotInterpolator TrySetTimestamp() {
		if (CanInterpolate() && timestamp < 0) {
			timestamp = GetInterpolationTime();
		}
		return this;
	}

	/**
	* Agrega una snapshot al interpolador, en caso de que sea válida (es decir,
	* en caso de que no sea antigua).
	*/
	protected SnapshotInterpolator AddPacket(Packet packet) {
		Snapshot snapshot = new Snapshot(packet);
		if (lastSequence < snapshot.sequence) {
			if (lastSequence < 0) {
				baseTime = Time.unscaledTime - GetTimeForSequence(snapshot.sequence);
			}
			lastSequence = snapshot.sequence;
			snapshots.Enqueue(snapshot);
		}
		else {
			// La secuencia es antigua, y el paquete se descarta.
		}
		return this;
	}

	/** **********************************************************************
	******************************* PUBLIC API ********************************
	 *********************************************************************** */

	/**
	* Agrega una cola de snapshots, ordenadas según el tiempo de llegada. Si es
	* la primera vez que se agregan paquetes, se descartan todos excepto el
	* último (el más reciente), para que la interpolación se acomode al estado
	* más actual de la simulación.
	*/
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

	/**
	* Actualiza la snapshot para el tiempo actual, efectivamente interpolando
	* el estado. Si no se puede interpolar, la snapshot no se ve afectada.
	*/
	public SnapshotInterpolator Update() {
		// Agregar verificación de secuencias corridas y validar ventana.
		SlideWindow().TrySetTimestamp();
		if (CanInterpolate()) {
			float Δt = GetInterpolationDelta();
			float Δn = Δt/Δs;
			now.Interpolate(from, to, Δn);
		}
		return this;
	}

	public class Builder {

		public Snapshot snapshot;
		public int window;
		public int sps;

		public Builder() {}

		public Builder Snapshot(Snapshot snapshot) {
			this.snapshot = snapshot;
			return this;
		}

		public Builder Window(int window) {
			this.window = window;
			return this;
		}

		public Builder SPS(int sps) {
			this.sps = sps;
			return this;
		}

		public SnapshotInterpolator Build() {
			return new SnapshotInterpolator(this);
		}
	}
}
