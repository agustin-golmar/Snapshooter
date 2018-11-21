using System.Collections.Generic;
using System.Linq;
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
	protected SnapshotInterpolator interpolator;
	protected Stream output;
	protected Threading threading;
	protected int timeout;
	protected int id;
	protected int sequence;
	protected SortedDictionary<int, Packet> packets;
	protected SortedDictionary<int, Packet> timedPackets;
	protected float lastTime;
	protected Player player;
	protected Predictor predictor;

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
		interpolator = new SnapshotInterpolator.Builder()
			.Snapshot(snapshot)
			.Window(config.slidingWindowSize)
			.SPS(config.snapshotsPerSecond)
			.Build();
		output = new Stream(config.maxPacketsInQueue);
		threading = new Threading();
		id = -1;
		sequence = 0;
		packets = new SortedDictionary<int, Packet>();
		timedPackets = new SortedDictionary<int, Packet>();
		lastTime = 0;
		timeout = config.timeout;
		player = null;
		predictor = null;
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
	protected Packet.Builder GetRequestHeader(PacketType type, Endpoint endpoint, int payloadSize) {
		return new Packet.Builder(10 + payloadSize)
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
		if (player != null) {
			Debug.Log("Player already created.");
		}
		else {
			Debug.Log("Creating player...");
			id = response.Reset(7).GetInteger();
			Vector3 respawn = response.GetVector();
			Quaternion rotation = response.GetQuaternion();
			response.Reset();
			// Crear el jugador y el predictor.
			player = GameObject.Find("World")
				.GetComponent<World>()
				.LoadSnapshot(snapshot)
				.CreatePlayer(respawn, rotation)
				.SetClient(this)
				.SetID(id)
				.SetSnapshot(snapshot);
			predictor = new Predictor(config, snapshot, player);
		}
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
		// Procesar ACKs:
		Packet ackResponse = input.Read(PacketType.ACK);
		if (ackResponse != null) {
			Endpoint endpoint = ackResponse.Reset(2).GetEndpoint();
			int sequence = ackResponse.GetInteger();
			ackResponse.Reset();
			//Debug.Log("ACK received for " + endpoint + ". Sequence: " + sequence);
			packets = new SortedDictionary<int, Packet>(packets
				.Where(x => x.Key > sequence)
				.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
			timedPackets = new SortedDictionary<int, Packet>(timedPackets
				.Where(x => x.Key > sequence)
				.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
			//Debug.Log("Packets Remaining: " + packets.Count);
			switch (endpoint) {
				case Endpoint.JOIN : {
					HandleJoin(ackResponse);
					break;
				}
			}
		}
		// Procesar SNAPSHOTs:
		Queue<Packet> snapshots = input.ReadAll(PacketType.SNAPSHOT);
		interpolator.AddPackets(snapshots).Update();
		// Predecir (si es necesario):
		if (config.usePrediction && player != null) {
			predictor.Validate();
		}
		// Procesar 'reliability':
		output.WriteAll(packets.Values);
		float curTime = Time.unscaledTime;
		if (curTime >= lastTime + timeout) {
			lastTime = curTime;
			output.WriteAll(timedPackets.Values);
		}
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
		Packet request = GetRequestHeader(PacketType.EVENT, Endpoint.JOIN, 19)
			.AddString(config.clientAddress)
			.AddInteger(config.clientListeningPort)
			.Build();
		timedPackets.Add(sequence - 1, request);
		output.Write(request);
	}

	/**
	* Mueve un jugador en alguna dirección. No maneja el 'straferunning'. Se
	* encarga de aplicar predicción, en caso de que esté habilitada.
	*/
	public void Move(List<Direction> directions) {
		float Δt = Time.deltaTime;
		if (config.usePrediction) {
			predictor.PredictMove(directions, Δt);
			predictor.SaveState(sequence);
		}
		Packet.Builder builder = GetRequestHeader(PacketType.FLOODING, Endpoint.MOVE, 8 + directions.Count)
			.AddFloat(Δt)
			.AddInteger(directions.Count);
		foreach (Direction direction in directions) {
			builder.AddDirection(direction);
		}
		Packet request = builder.Build();
		packets.Add(sequence - 1, request);
		output.Write(request);
	}

	/**
	* Efectúa un disparo con el rifle (usando hit-scan). Se envía el target.
	*/
	public void Shoot(Vector3 position,Vector3 target) {
		Packet request = GetRequestHeader(PacketType.FLOODING, Endpoint.SHOOT, 12)
			.AddVector(target)
			.Build();
		Debug.DrawRay(position,10*target,Color.red,5);
		if (Physics.Raycast(position,target)){
			Debug.Log("Hit!");
		} else {
			Debug.Log("Miss");
		}
		packets.Add(sequence - 1, request);
		output.Write(request);
	}

	/**
	* Lanza una granada cuyo daño infligido opera en área (AoE). Se envía la
	* dirección de la fuerza de lanzamiento.
	*/
	public void Frag(Vector3 force) {
		Packet request = GetRequestHeader(PacketType.FLOODING, Endpoint.FRAG, 12)
			.AddVector(force)
			.Build();
		packets.Add(sequence - 1, request);
		output.Write(request);
	}
}
