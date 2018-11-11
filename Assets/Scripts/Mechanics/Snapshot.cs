using System;
using UnityEngine;

	/*
	* Representa el estado dinámico del sistema, el cual debe serializarse.
	* Contiene toda la información del estado del mundo. Actualmente el estado
	* implica:
	*
	*	-El número de secuencia de la snapshot.
	*	-El timestamp de dicha snapshot.
	*	-La cantidad de jugadores en la partida.
	*	-El ID de cada jugador.
	*	-La vida de cada jugador (de 0 a 100).
	*	-Un vector de posición por cada jugador.
	*	-Un quaternion de rotación por cada jugador.
	*
	* Luego debería contener las entidades físicas (granadas).
	*/

public class Snapshot {

	// Timestamp y N° de secuencia de la snapshot:
	public int sequence;
	public float timestamp;

	// Cantidad de jugadores en la partida:
	public int players;

	// Propiedades de los jugadores:
	public int [] ids;
	public int [] lifes;
	public Transform [] transforms;

	// Otros datos...

	/**
	* Asigna memoria suficiente para la cantidad de jugadores especificados.
	*/
	protected Snapshot Allocate(int maxPlayers) {
		ids = new int [maxPlayers];
		lifes = new int [maxPlayers];
		transforms = new Transform [maxPlayers];
		for (int k = 0; k < maxPlayers; ++k) {
			transforms[k] = new GameObject().transform;
			transforms[k].position = Vector3.zero;
			transforms[k].rotation = Quaternion.identity;
		}
		return this;
	}

	public Snapshot(int maxPlayers) {
		Allocate(maxPlayers);
		// Otros datos...
	}

	/**
	* Crea una snapshot desde un paquete de bytes.
	*/
	public Snapshot(Packet packet) {				// Evitar que se creen transformadas cada vez!!!
		packet.Reset(1);
		sequence = packet.GetInteger();
		timestamp = packet.GetFloat();
		players = packet.GetInteger();
		Allocate(players);
		for (int k = 0; k < players; ++k) {
			ids[k] = packet.GetInteger();
			lifes[k] = packet.GetInteger();
			transforms[k].position = packet.GetVector();
			transforms[k].rotation = packet.GetQuaternion();
		}
		// Otros datos...
		packet.Reset();
	}

	/**
	* Crea una snapshot interpolada, utilizando dos snapshots como extremo. Ya
	* que pueden existir diferente cantidad de jugadores entre una snapshot y
	* otra, se toma el mínimo entre ambas snapshots. En el caso de que ciertos
	* parámetros no sean interpolables (como la vida), se toma el parámetro
	* antiguo (de from), ya que el nuevo parámetro todavía no ocurrió (es
	* decir, pertenece al futuro del jugador).
	*/
	public Snapshot Interpolate(Snapshot from, Snapshot to, float Δn) {
		sequence = from.sequence;
		timestamp = from.timestamp + Δn * (to.timestamp - from.timestamp);
		players = Math.Min(from.players, to.players);
		for (int k = 0; k < players; ++k) {
			ids[k] = from.ids[k];
			lifes[k] = from.lifes[k];
			transforms[k].position = Vector3.Lerp(from.transforms[k].position, to.transforms[k].position, Δn);
			transforms[k].rotation = Quaternion.Slerp(from.transforms[k].rotation, to.transforms[k].rotation, Δn);
		}
		// Otros datos...
		return this;
	}

	/**
	* Devuelve la representación en forma de paquete de bytes de esta snapshot.
	*/
	public Packet ToPacket() {
		Packet.Builder builder = new Packet.Builder(13 + 36 * players)
			.AddPacketType(PacketType.SNAPSHOT)
			.AddInteger(sequence)
			.AddFloat(timestamp)
			.AddInteger(players);
		for (int k = 0; k < players; ++k) {
			builder.AddInteger(ids[k])
				.AddInteger(lifes[k])
				.AddVector(transforms[k].position)
				.AddQuaternion(transforms[k].rotation);
		}
		// Otros datos...
		return builder.Build();
	}

	/** ***********************************************************************
	* Setters encadenables.
	*/

	public Snapshot Sequence(int sequence) {
		this.sequence = sequence;
		return this;
	}

	public Snapshot Timestamp(float timestamp) {
		this.timestamp = timestamp;
		return this;
	}

	public Snapshot Players(int players) {
		this.players = players;
		return this;
	}

	public Snapshot ID(int index, int id) {
		lifes[index] = id;
		return this;
	}

	public Snapshot Life(int index, int life) {
		lifes[index] = life;
		return this;
	}

	public Snapshot Position(int index, Vector3 position) {
		transforms[index].position = position;
		return this;
	}

	public Snapshot Rotation(int index, Quaternion rotation) {
		transforms[index].rotation = rotation;
		return this;
	}
}
