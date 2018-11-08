using System.Collections;
using System.Collections.Generic;
using UnityEngine;

	/**
	* Representa una instancia de cliente dentro del juego Snapshooter, la cual
	* se encarga de gestionar la comunicación con el servidor, actualizando el
	* estado de las entidades en el escenario de forma transparente.
	*/

public class Client : IClosable {

	protected Configuration config;
	protected Demultiplexer input;
	protected Link local;
	protected Link server;
	protected Snapshot snapshot;
	protected Stream output;
	protected Threading threading;
	protected int timeout;
	protected int id;
	protected int sequence;
	protected SortedDictionary<int,Packet> packets;
	protected SortedDictionary<int,Packet> timedPackets;
	protected float lastTime;

	public Client(Configuration configuration) {
		config = configuration;
		input = new Demultiplexer(config.maxPacketsInQueue);
		local = new Link.Builder()
			.Bind(true)
			.IP("0.0.0.0")
			.Port(config.clientListeningPort)
			.ReceiveTimeout(config.clientReceiveTimeout)
			.SendTimeout(config.clientSendTimeout)
			.Build();
		server = new Link.Builder()
			.Bind(false)
			.IP(config.serverAddress)
			.Port(config.serverListeningPort)
			.ReceiveTimeout(config.serverReceiveTimeout)
			.SendTimeout(config.serverSendTimeout)
			.Build();
		snapshot = new Snapshot(config.maxPlayers);
		output = new Stream(config.maxPacketsInQueue);
		threading = new Threading();
		id = -1;
		sequence = 0;
		packets = new SortedDictionary<int,Packet>();
		timedPackets = new SortedDictionary<int,Packet>();
		lastTime = 0;
		timeout = configuration.timeout;
	}

	/**
	* Recibe paquetes desde el servidor, y los agrega al demultiplexer de
	* entrada.
	*/
	protected void RequestHandler() {
		while (!config.OnExit()) {
			byte [] payload = local.Receive(server);
			if (payload != null) {
				Packet packet = new Packet(payload);
				input.Write(packet);
			}
		}
	}

	/**
	* Envía los paquetes hacia el servidor, si hay disponibles.
	*/
	protected void ResponseHandler() {
		while (!config.OnExit()) {
			local.Send(server, output);
		}
	}

	/**
	* Genera el encabezado de un request, cuyo formato es:
	*
	*	<PacketType> <Endpoint> <Sequence> <ID>
	*
	* La secuencia es autoincrementable, por lo cual todos los request deberían
	* construirse bajo este método. El tamaño del paquete final no contiene
	* bytes de más debido a que la clase Packet aplica shrinking.
	*/
	protected Packet.Builder GetRequestHeader(PacketType type, Endpoint endpoint, int maxPacketSize) {
		return new Packet.Builder(maxPacketSize)
			.AddPacketType(type)
			.AddEndpoint(endpoint)
			.AddInteger(sequence++)
			.AddInteger(id);
	}

	/**
	* Finaliza la conexión del cliente con el servidor, al recibir una
	* respuesta afirmativa, con un ID válido (mayor o igual a cero).
	*/
	protected void HandleJoin(Packet response) {
		id = response.Reset(7).GetInteger();
	}

	/** **********************************************************************
	******************************* PUBLIC API ********************************
	 *********************************************************************** */

	/**
	* Levanta los threads de entrada y salida de paquetes, efectivamente
	* comenzando a recibir y enviar información hacia el servidor. Además,
	* envía un paquete solicitando conectarse al servidor, junto con su
	* dirección IP y puerto.
	*/
	public void Raise() {
		Debug.Log("Client listening on 0.0.0.0:" + config.clientListeningPort
			+ ", and connected to server on " + config.serverAddress + ":" + config.serverListeningPort + ".");
		threading.Submit(RequestHandler);
		threading.Submit(ResponseHandler);
		Join();
	}

	/**
	* Bucle principal del cliente. Recibe paquetes de tipo ACK y SNAPSHOT,
	* exclusivamente.
	*/
	public void FrameHandler() {
		Packet ackResponse = input.Read(PacketType.ACK);
		if (ackResponse != null) {
			Endpoint endpoint = ackResponse.Reset(2).GetEndpoint();
			int seq = ackResponse.Reset(3).GetInteger();
			ackResponse.Reset();
			Debug.Log("ACK received for " + endpoint + "... seq: "+seq);
			for(int i=0;i<=seq;i++){
				packets.Remove(i);
				timedPackets.Remove(i);
			}
			Debug.Log("Packets Remaining: "+packets.Count);
			switch (endpoint) {
				case Endpoint.JOIN : {
					HandleJoin(ackResponse);
					break;
				}
			}
		}
		Packet snapshotResponse = input.Read(PacketType.SNAPSHOT);
		if (snapshotResponse != null) {
			Debug.Log("SNAPSHOT received...");
		}
		foreach(KeyValuePair<int,Packet> p in packets){
			//Debug.Log("Writing seq: "+p.Key);
			output.Write(p.Value);
		}
		float curTime = Time.unscaledTime;
		if (curTime >= lastTime + timeout) {
			lastTime = curTime;
			foreach(KeyValuePair<int,Packet> p in timedPackets){
				output.Write(p.Value);
			}
		}

		Move(Direction.FORWARD);
	}

	/**
	* Libera los sockets y cancela los threads.
	*/
	public void Close() {
		Debug.Log("Closing client with ID = " + id + "...");
		threading.Shutdown();
		local.Close();
		server.Close();
	}

	/**
	* Conecta un cliente a la partida (si la sala no está llena).
	*/
	public void Join() {
		Packet request = GetRequestHeader(PacketType.EVENT, Endpoint.JOIN, 32)
			.AddString(config.clientAddress)
			.AddInteger(config.clientListeningPort)
			.Build();
		timedPackets.Add(sequence-1,request);
		output.Write(request);
	}

	/**
	* Mueve un jugador en alguna dirección. No maneja el 'straferunning'.
	*/
	public void Move(Direction direction) {
		Packet request = GetRequestHeader(PacketType.FLOODING, Endpoint.MOVE, 11)
			.AddDirection(direction)
			.Build();
		packets.Add(sequence-1,request);
		output.Write(request);
	}

	/**
	* Efectúa un disparo con el rifle (usando hit-scan). Se envía el target.
	*/
	public void Shoot(Vector3 target) {
		Packet request = GetRequestHeader(PacketType.FLOODING, Endpoint.SHOOT, 32)
			.AddVector(target)
			.Build();
		packets.Add(sequence-1,request);
		output.Write(request);
	}

	/**
	* Lanza una granada cuyo daño infligido opera en área (AoE). Se envía la
	* dirección de la fuerza de lanzamiento.
	*/
	public void Frag(Vector3 force) {
		Packet request = GetRequestHeader(PacketType.FLOODING, Endpoint.FRAG, 32)
			.AddVector(force)
			.Build();
		packets.Add(sequence-1,request);
		output.Write(request);
	}
}
