using System.Collections.Generic;
using System.Net;
using UnityEngine;

	/**
	* Expone un servicio de red que permite gestionar una partida de
	* Snapshooter. Para ello ofrece varios endpoints bajo los cuales se pueden
	* gestionar diferentes operaciones (conectarse, moverse, disparar, etc.).
	*
	* Adicionalmente, y para los clientes conectados, el servicio entrega
	* regularmente una snapshot con el estado actual de la partida.
	*/

public class Server : IClosable, IAPI {

	protected Configuration config;
	protected Demultiplexer input;
	protected Dictionary<string, Packet> joins;
	protected GameObject ghost;
	protected LagSimulator lagSimulator;
	protected Link local;
	protected List<Link> links;
	protected List<Stream> outputs;
	protected Snapshot snapshot;
	protected SortedDictionary<int, Packet> [] acks;
	protected Threading threading;
	protected Transform ghostTransform;
	protected World world;
	protected float lastSnapshot;
	protected volatile float timestamp;

	public Server(Configuration configuration) {
		config = configuration;
		input = new Demultiplexer(config.maxPacketsInQueue);
		joins = new Dictionary<string, Packet>();
		ghost = new GameObject("Server Ghost");
		lagSimulator = new LagSimulator(config);
		local = new Link.Builder()
			.Bind(true)
			.IP("0.0.0.0")
			.Port(config.serverListeningPort)
			.ReceiveTimeout(config.serverReceiveTimeout)
			.SendTimeout(config.serverSendTimeout)
			.Build();
		links = new List<Link>();
		outputs = new List<Stream>();
		snapshot = config.GetServerSnapshot();
		acks = new SortedDictionary<int, Packet>[config.maxPlayers];
		for (int i = 0; i < config.maxPlayers; ++i) {
			acks[i] = new SortedDictionary<int, Packet>();
		}
		threading = new Threading();
		ghostTransform = ghost.transform;
		world = GameObject.Find("World")
			.GetComponent<World>()
			.LoadSnapshot(snapshot);
		lastSnapshot = 0.0f;
		timestamp = 0.0f;
	}

	/**
	* Según el endpoint del request, despacha el proceso adecuado y devuelve el
	* response correspondiente.
	*/
	protected Packet Dispatch(Packet request) {
		Endpoint endpoint = request.Reset(1).GetEndpoint();
		int sequence = request.GetInteger();
		int id = request.GetInteger();
		request.Reset();
		Packet response;
		if (0 <= id && acks[id].TryGetValue(sequence, out response)) {
			return response;
		}
		switch (endpoint) {
			case Endpoint.JOIN : {
				response = Join(request);
				break;
			}
			case Endpoint.MOVE : {
				response = Move(request);
				break;
			}
			case Endpoint.SHOOT : {
				response = Shoot(request);
				break;
			}
			case Endpoint.FRAG : {
				response = Frag(request);
				break;
			}
			default : {
				Debug.Log("Unknown Endpoint: " + endpoint);
				return GetResponseHeader(request, 0).Build();
			}
		}
		if (0 <= id) {
			acks[id].Add(sequence, response);
			snapshot.acks[id] = sequence;
		}
		return response;
	}

	/**
	* Aplica round-robin: por cada enlace de entrada (cada cliente conectado), lee
	* un paquete (si puede), y lo procesa o lo agrega al Demultiplexer.
	*/
	protected void RequestHandler() {
		IPEndPoint anyLink = new IPEndPoint(IPAddress.Any, 0);
		System.Random random = new System.Random();
		while (!config.OnExit()) {
			byte [] payload = local.Receive(anyLink);
			if (payload != null && config.packetLossRatio <= random.NextDouble()) {
				Packet packet = new Packet(payload);
				lagSimulator.Write(packet, timestamp);
			}
			Packet futurePacket = lagSimulator.Read(timestamp);
			if (futurePacket != null) {
				input.Write(futurePacket);
			}
		}
	}

	/**
	* Aplica round-robin: para cada stream de salida, si hay algún paquete lo
	* envía y continúa con el siguiente stream.
	*/
	protected void ResponseHandler() {
		System.Random random = new System.Random();
		LagSimulator [] lagSimulators = new LagSimulator [config.maxPlayers];
		Debug.Log("Loading " + lagSimulators.Length + " lag simulators for response.");
		for (int k = 0; k < lagSimulators.Length; ++k) {
			lagSimulators[k] = new LagSimulator(config);
		}
		while (!config.OnExit()) {
			for (int id = 0; id < outputs.Count; ++id) {
				Packet packet = outputs[id].Read();
				if (packet != null && config.packetLossRatio <= random.NextDouble()) {
					lagSimulators[id].Write(packet, timestamp);
				}
			}
			for (int id = 0; id < outputs.Count; ++id) {
				Packet futurePacket = lagSimulators[id].Read(timestamp);
				if (futurePacket != null) {
					local.Send(links[id], futurePacket);
				}
			}
		}
	}

	/**
	* Genera un paquete de respuesta genérico. El paquete de respuesta contiene
	* los siguientes campos, en este orden:
	*
	*	<ACK> <PacketType> <Endpoint> <Sequence>
	*
	* Donde el tipo de paquete, endpoint y número de secuencia se corresponden
	* con el request que originó este response.
	*/
	protected Packet.Builder GetResponseHeader(Packet request, int payloadSize) {
		Packet.Builder response = new Packet.Builder(7 + payloadSize)
					.AddPacketType(PacketType.ACK)
					.AddPacketType(request.GetPacketType())
					.AddEndpoint(request.GetEndpoint())
					.AddInteger(request.GetInteger());
		request.Reset();
		return response;
	}

	/**
	* Devuelve el siguiente punto de respawn para el cliente indicado. Debería
	* evitar colisiones, siempre que sea posible.
	*/
	protected Vector3 GetRespawn(int id) {
		return new Vector3(4 * id - 2 * config.maxPlayers, 1.0f, 0);
	}

	/**
	* Procesa todos los paquetes de cierto tipo en el thread principal, lo cual
	* permite que cada endpoint adquiera acceso al API de Unity.
	*/
	protected void HandleAllRequest(PacketType type) {
		Queue<Packet> packets = input.ReadAll(type);
		foreach (Packet request in packets) {
			int id = request.Reset(6).GetInteger();
			Packet response = Dispatch(request.Reset());
			if (0 <= id) {
				outputs[id].Write(response);
			}
		}
	}

	/**
	* Carga en la transformada fantasma los parámetros de un cliente
	* específico, de modo que pueda utilizarse para computar traslaciones u
	* otras operaciones complejas.
	*/
	protected Transform LoadGhostFor(int id) {
		ghostTransform.SetPositionAndRotation(snapshot.positions[id], snapshot.rotations[id]);
		return ghostTransform;
	}

	/**
	* Actualiza el estado de la snapshot, luego de computar una transformación
	* sobre el objeto fantasma.
	*/
	protected Transform SaveGhostFor(int id) {
		snapshot.positions[id] = ghostTransform.position;
		snapshot.rotations[id] = ghostTransform.rotation;
		return ghostTransform;
	}

	/** **********************************************************************
	******************************* PUBLIC API ********************************
	 *********************************************************************** */

	/**
	* Levanta los threads de entrada y salida de paquetes, efectivamente
	* comenzando a recibir y enviar información hacia los clientes.
	*/
	public void Raise() {
		Debug.Log("Server listening on 0.0.0.0:" + config.serverListeningPort + ".");
		threading.Submit(RequestHandler);
		threading.Submit(ResponseHandler);
	}

	/**
	* Si es momento de enviar una snapshot, transforma la misma en un paquete y
	* agrega una copia a cada stream de salida. En algún momento los paquetes
	* serán enviados por ResponseHandler.
	*/
	public void FrameHandler() {
		// Actualizar timestamp:
		timestamp = Time.unscaledTime;
		// Procesar paquetes reliable:
		HandleAllRequest(PacketType.EVENT);
		HandleAllRequest(PacketType.FLOODING);
		// Procesar snapshot:
		int currentSnapshot = Mathf.FloorToInt(timestamp * config.snapshotsPerSecond);
		if (lastSnapshot < currentSnapshot) {
			lastSnapshot = currentSnapshot;
			snapshot.sequence = currentSnapshot;
			snapshot.timestamp = timestamp;
			if (snapshot.players == 0) return;
			Packet packet = snapshot.ToPacket();
			foreach (Stream output in outputs) {
				output.Write(packet);
			}
		}
	}

	/**
	* Cierra los sockets utilizados, y cancela los threads. Además, vacía los
	* streams de salida.
	*/
	public void Close() {
		Debug.Log("Closing server...");
		threading.Shutdown();
		local.Close();
		foreach (Link link in links) {
			link.Close();
		}
		outputs.Clear();
		links.Clear();
	}

	/**
	* Conecta un cliente a la partida (si la sala no está llena). No se maneja
	* el caso en que el ACK se pierde, pero las estructuras ya fueron creadas.
	*/
	public Packet Join(Packet request) {
		// Extraer <ip:port> del cliente:
		string ip = request.Reset(10).GetString();
		int port = request.GetInteger();
		request.Reset();
		IPEndPoint client = new IPEndPoint(IPAddress.Parse(ip), port);
		string key = ip + ":" + port;
		if (joins.ContainsKey(key)) {
			// El cliente ya se había conectado, pero no recibió el ACK:
			Debug.Log("Server already joined the client from host: " + key);
			local.Send(client, joins[key]);
		}
		else if (links.Count < config.maxPlayers) {
			int index = links.Count;
			// Por ahora, el ID es equivalente al índice:
			int id = index;
			// Crear enlace y stream de paquetes:
			links.Add(new Link.Builder()
				.Bind(false)
				.IP(ip)
				.Port(port)
				.ReceiveTimeout(config.serverReceiveTimeout)
				.SendTimeout(config.serverSendTimeout)
				.Build());
			outputs.Add(new Stream(config.maxPacketsInQueue));
			// Actualizar snapshot global:
			++snapshot.players;
			snapshot.ID(index, id)
				.Life(index, config.playerLife)
				.Position(index, GetRespawn(id))
				.Rotation(index, Quaternion.identity)
				.Fuse(index, -1)
				.GrenadePosition(index, Vector3.zero)
				.GrenadeRotation(index, Quaternion.identity);
			// Agregar el nuevo ID al response y enviar:
			Packet response = GetResponseHeader(request, 32)
					.AddInteger(id)
					.AddVector(snapshot.positions[id])
					.AddQuaternion(snapshot.rotations[id])
					.Build();
			// Reliable join:
			joins.Add(key, response);
			Debug.Log("Server successfully join a new client: " + id);
			local.Send(client, response);
		}
		return GetResponseHeader(request, 0).Build();
	}

	/**
	* Mueve un jugador en alguna dirección.
	*/
	public Packet Move(Packet request) {
		int id = request.Reset(6).GetInteger();
		BitBuffer bb = request.GetBitBuffer();
		float Δt = bb.GetFloat(0, 1, 0.0001f);
		int directions = bb.GetInt(0, 10);
		float delta = Δt * config.playerSpeed;
		LoadGhostFor(id);
		for (int k = 0; k < directions; ++k) {
			switch (bb.GetDirection()) {
				case Direction.FORWARD : {
					ghostTransform.Translate(0, 0, delta);
					break;
				}
				case Direction.STRAFING_LEFT : {
					ghostTransform.Translate(-delta, 0, 0);
					break;
				}
				case Direction.BACKWARD : {
					ghostTransform.Translate(0, 0, -delta);
					break;
				}
				case Direction.STRAFING_RIGHT : {
					ghostTransform.Translate(delta, 0, 0);
					break;
				}
				case Direction.ROTATE_RIGHT : {
					ghostTransform.Rotate(0, 10 * delta, 0);
					break;
				}
				case Direction.ROTATE_LEFT : {
					ghostTransform.Rotate(0, -10 * delta, 0);
					break;
				}
			}
		}
		SaveGhostFor(id);
		return GetResponseHeader(request.Reset(), 0).Build();
	}

	/**
	* Efectúa un disparo con el rifle (usando hit-scan).
	*/
	public Packet Shoot(Packet request) {
		byte hit_id = request.Reset(10).GetByte();
		if (hit_id != 255) {
			int id = (int) hit_id;
			snapshot.lifes[id]--;
		}
		return GetResponseHeader(request.Reset(), 0).Build();
	}

	/**
	* Lanza una granada cuyo daño es en área (AoE).
	*/
	public Packet Frag(Packet request) {
		Debug.Log("Frag requested...");
		int id = request.Reset(6).GetInteger();
		snapshot.Fuse(id, config.grenadeFuseTime);
		world.CreateGrenade(snapshot.positions[id], snapshot.rotations[id])
			.SetID(id)
			.SetSnapshot(snapshot)
			.Throw();
		Debug.Log("Grenade created on server for ID = " + id);
		return GetResponseHeader(request.Reset(), 0).Build();
	}
}
