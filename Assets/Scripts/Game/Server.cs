using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class Server : IClosable {

	protected Configuration config;
	protected Demultiplexer input;
	protected List<Stream> outputs;
	protected Threading threading;
	protected Link local;
	protected List<Link> links;
	protected Snapshot snapshot;
	protected float lastSnapshot;
	protected float Δs;

	public Server(Configuration configuration) {
		config = configuration;
		input = new Demultiplexer(config.maxPacketsInQueue);
		outputs = new List<Stream>();
		threading = new Threading();
		local = new Link.Builder()
			.Bind(true)
			.IP("0.0.0.0")
			.Port(config.serverListeningPort)
			.ReceiveTimeout(config.serverReceiveTimeout)
			.SendTimeout(config.serverSendTimeout)
			.Build();
		links = new List<Link>();
		snapshot = new Snapshot(config.maxPlayers);
		lastSnapshot = 0.0f;
		Δs = 1.0f / config.snapshotsPerSecond;
	}

	/**
	* Cierra los sockets utilizados, y cancela los threads.
	*/
	public void Close() {
		Debug.Log("Releasing server...");
		threading.Shutdown();
		local.Close();
		foreach (Link link in links) {
			link.Close();
		}
		outputs.Clear();
		links.Clear();
	}

	/**
	* Levanta los threads de entrada y salida de paquetes, efectivamente
	* comenzando a recibir y enviar información hacia los clientes.
	*/
	public void Raise() {
		Debug.Log("Server listening on 0.0.0.0:" + config.serverListeningPort + ".");
		threading.Submit(ProcessInput);
		threading.Submit(ProcessOutput);
	}

	/**
	* Agrega un nuevo cliente a la partida, creando su entidad y generando un
	* identificador único para él (el cual retorna). Si no se aceptan más
	* jugadores porque la sala está llena, se devuelve -1.
	*/
	public int JoinClient(string address, int port) {
		if (links.Count < config.maxPlayers) {
			int id = links.Count;
			links.Add(new Link.Builder()
				.Bind(false)
				.IP(address)
				.Port(port)
				.ReceiveTimeout(config.serverReceiveTimeout)
				.SendTimeout(config.serverSendTimeout)
				.Build());
			++snapshot.players;
			snapshot.Life(id, 100)
				.Position(id, GetRespawn(id))
				.Rotation(id, Quaternion.identity);
			return id;
		}
		else return -1;
	}

	/**
	* Devuelve el siguiente punto de respawn para el cliente indicado. Debería
	* evitar colisiones, siempre que sea posible.
	*/
	public Vector3 GetRespawn(int id) {
		return new Vector3(4 * id - 2 * config.maxPlayers, 1.0f, 0);
	}

	/**
	* Si es momento de enviar una snapshot, transforma la misma en un paquete y
	* agrega una copia a cada stream de salida. En algún momento los paquetes
	* serán enviados por ProcessOutput.
	*/
	public void ProcessSnapshot() {
		int currentSnapshot = Mathf.FloorToInt(Time.unscaledTime/Δs);
		if (lastSnapshot < currentSnapshot) {
			lastSnapshot = currentSnapshot;
			Debug.Log("Snapshooting at " + Time.unscaledTime + " sec. (snapshot " + currentSnapshot + ")");
			snapshot.sequence = currentSnapshot;
			snapshot.timestamp = Time.unscaledTime;
			Packet packet = snapshot.ToPacket();
			foreach (Stream output in outputs) {
				output.Write(packet);
			}
		}
	}

	/**
	* Aplica round-robin: por cada enlace de entrada (cada cliente conectado), lee
	* un paquete (si puede), y lo agrega al Demultiplexer.
	*/
	protected void ProcessInput() {
		IPEndPoint anyLink = new IPEndPoint(IPAddress.Any, 0);
		while (!config.OnExit()) {
			//int id = 0;
			// Si la sala no está llena:
			if (links.Count < config.maxPlayers) {
				byte [] payload = local.Receive(anyLink);
				if (payload != null) {
					Packet packet = new Packet(payload);
					PacketType type = packet.GetPacketType();
					// Sacar afuera este switch!!!
					switch (type) {
						case PacketType.EVENT : {
							string address = packet.GetString();
							int port = packet.GetInteger();
							Debug.Log("Event received from " + address + ":" + port + "...");
							int client = JoinClient(address, port);
							Packet response = new Packet.Builder(48)
									.AddPacketType(PacketType.ACK)
									.AddInteger(client)
									.AddInteger(snapshot.lifes[client])
									.AddVector(snapshot.positions[client])
									.AddQuaternion(snapshot.rotations[client])
									.Build();
							local.Send(new IPEndPoint(IPAddress.Parse(address), port), response);
							break;
						}
						default : {
							input.Write(packet.Reset());
							break;
						}
					}
				}
			}
			// Para los jugadores ya conectados:
			/*foreach (Link link in links) {
				byte [] payload = local.Receive(link);
				if (payload != null) {
					Debug.Log("Receiving from client " + id + "...");
					Packet packet = new Packet(payload);
					input.Write(packet);
				}
				++id;
			}*/
		}
	}

	/**
	* Aplica round-robin: para cada stream de salida, si hay algún paquete lo
	* envía y continúa con el siguiente stream.
	*/
	protected void ProcessOutput() {
		while (!config.OnExit()) {
			int id = 0;
			foreach (Stream output in outputs) {
				if (0 < local.Send(links[id], output)) {
					Debug.Log("Sending to client " + id + "...");
				}
				++id;
			}
		}
	}
}
