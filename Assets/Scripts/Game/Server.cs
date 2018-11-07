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
	protected Link local;
	protected List<Link> links;
	protected List<Stream> outputs;
	protected Snapshot snapshot;
	protected Threading threading;
	protected float lastSnapshot;
	protected float Δs;

	public Server(Configuration configuration) {
		config = configuration;
		input = new Demultiplexer(config.maxPacketsInQueue);
		local = new Link.Builder()
			.Bind(true)
			.IP("0.0.0.0")
			.Port(config.serverListeningPort)
			.ReceiveTimeout(config.serverReceiveTimeout)
			.SendTimeout(config.serverSendTimeout)
			.Build();
		links = new List<Link>();
		outputs = new List<Stream>();
		snapshot = new Snapshot(config.maxPlayers);
		threading = new Threading();
		lastSnapshot = 0.0f;
		Δs = 1.0f / config.snapshotsPerSecond;
	}

	/**
	* Según el endpoint del request, despacha el proceso adecuado y devuelve el
	* response correspondiente.
	*/
	protected Packet Dispatch(Packet request) {
		Endpoint endpoint = request.Reset(1).GetEndpoint();
		request.Reset();
		switch (endpoint) {
			case Endpoint.JOIN : {
				return Join(request);
			}
			case Endpoint.MOVE : {
				return Move(request);
			}
			case Endpoint.SHOOT : {
				return Shoot(request);
			}
			case Endpoint.FRAG : {
				return Frag(request);
			}
			default : {
				Debug.Log("Unknown Endpoint: " + endpoint);
				return GetResponseHeader(request, 7).Build();
			}
		}
	}

	/**
	* Aplica round-robin: por cada enlace de entrada (cada cliente conectado), lee
	* un paquete (si puede), y lo procesa o lo agrega al Demultiplexer.
	*/
	protected void RequestHandler() {
		IPEndPoint anyLink = new IPEndPoint(IPAddress.Any, 0);
		while (!config.OnExit()) {
			// Obtengo el paquete request:
			byte [] payload = local.Receive(anyLink);
			if (payload == null) continue;
			Packet request = new Packet(payload);
			PacketType type = request.GetPacketType();
			int id = request.Reset(6).GetInteger();
			request.Reset();
			// Proceso el mismo, y genero el response:
			switch (type) {
				case PacketType.EVENT :
				case PacketType.FLOODING : {
					Packet response = Dispatch(request);
					if (0 <= id) {
						local.Send(links[id], response);
					}
					break;
				}
				default : {
					// No debería pasar nunca:
					Debug.Log("Invalid PacketType: " + type);
					input.Write(request);
					break;
				}
			}
		}
	}

	/**
	* Aplica round-robin: para cada stream de salida, si hay algún paquete lo
	* envía y continúa con el siguiente stream.
	*/
	protected void ResponseHandler() {
		while (!config.OnExit()) {
			for (int id = 0; id < outputs.Count; ++id) {
				local.Send(links[id], outputs[id]);
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
	protected Packet.Builder GetResponseHeader(Packet request, int maxPacketSize) {
		Packet.Builder response = new Packet.Builder(maxPacketSize)
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
		int currentSnapshot = Mathf.FloorToInt(Time.unscaledTime/Δs);
		if (lastSnapshot < currentSnapshot) {
			lastSnapshot = currentSnapshot;
			Debug.Log("Snapshooting at " + Time.unscaledTime + " sec. (snapshot " + currentSnapshot + ").");
			snapshot.sequence = currentSnapshot;
			snapshot.timestamp = Time.unscaledTime;
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
		if (links.Count < config.maxPlayers) {
			// Extraer <ip:port> del cliente:
			string ip = request.Reset(10).GetString();
			int port = request.GetInteger();
			int id = links.Count;
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
			snapshot.Life(id, 100)
				.Position(id, GetRespawn(id))
				.Rotation(id, Quaternion.identity);
			// Agregar el nuevo ID al response y enviar:
			Packet response = GetResponseHeader(request.Reset(), 11)
					.AddInteger(id)
					.Build();
			local.Send(new IPEndPoint(IPAddress.Parse(ip), port), response);
		}
		return GetResponseHeader(request, 7).Build();
	}

	/**
	* Mueve un jugador en alguna dirección.
	*/
	public Packet Move(Packet request) {
		return GetResponseHeader(request, 7).Build();
	}

	/**
	* Efectúa un disparo con el rifle (usando hit-scan).
	*/
	public Packet Shoot(Packet request) {
		return GetResponseHeader(request, 7).Build();
	}

	/**
	* Lanza una granada cuyo daño es en área (AoE).
	*/
	public Packet Frag(Packet request) {
		return GetResponseHeader(request, 7).Build();
	}
}
