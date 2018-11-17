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
	*	-La secuencia del último ACK para ese cliente.
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
	public int [] acks;
	public int [] lifes;
	public Vector3 [] positions;
	public Quaternion [] rotations;

	// Otros datos...

	/**
	* Asigna memoria suficiente para la cantidad de jugadores especificados.
	*/
	protected Snapshot Allocate(int maxPlayers) {
		ids = new int [maxPlayers];
		acks = new int [maxPlayers];
		lifes = new int [maxPlayers];
		positions = new Vector3 [maxPlayers];
		rotations = new Quaternion [maxPlayers];
		return this;
	}

	public Snapshot(int maxPlayers) {
		Allocate(maxPlayers);
		// Otros datos...
	}

	/**
	* Crea una snapshot desde un paquete de bytes.
	*/
	public Snapshot(Packet packet) {
		packet.Reset(1);
		sequence = packet.GetInteger();
		timestamp = packet.GetFloat();
		players = packet.GetInteger();
		Allocate(players);
		for (int k = 0; k < players; ++k) {
			ids[k] = packet.GetInteger();
			acks[k] = packet.GetInteger();
			lifes[k] = packet.GetInteger();
			positions[k] = packet.GetVector();
			rotations[k] = packet.GetQuaternion();
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
			acks[k] = from.acks[k];
			lifes[k] = from.lifes[k];
			positions[k] = Vector3.Lerp(from.positions[k], to.positions[k], Δn);
			rotations[k] = Quaternion.Slerp(from.rotations[k], to.rotations[k], Δn);
		}
		// Otros datos...
		return this;
	}

	/**
	* Devuelve la representación en forma de paquete de bytes de esta snapshot.
	*/
	public Packet ToPacket() {
		Packet.Builder builder = new Packet.Builder(13 + 40 * players)
			.AddPacketType(PacketType.SNAPSHOT)
			.AddInteger(sequence)
			.AddFloat(timestamp)
			.AddInteger(players);
		for (int k = 0; k < players; ++k) {
			builder.AddInteger(ids[k])
				.AddInteger(acks[k])
				.AddInteger(lifes[k])
				.AddVector(positions[k])
				.AddQuaternion(rotations[k]);
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
		ids[index] = id;
		return this;
	}

	public Snapshot ACK(int index, int sequence) {
		acks[index] = sequence;
		return this;
	}

	public Snapshot Life(int index, int life) {
		lifes[index] = life;
		return this;
	}

	public Snapshot Position(int index, Vector3 position) {
		positions[index] = position;
		return this;
	}

	public Snapshot Rotation(int index, Quaternion rotation) {
		rotations[index] = rotation;
		return this;
	}
}
