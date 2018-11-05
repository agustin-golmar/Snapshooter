using UnityEngine;

public class Client : IClosable {

	protected Configuration config;
	protected Demultiplexer input;
	protected Stream output;
	protected Threading threading;
	protected Link local;
	protected Link server;
	protected Snapshot snapshot;
	protected int id;

	public Client(Configuration configuration) {
		Debug.Log("Creating client...");
		config = configuration;
		input = new Demultiplexer(config.maxPacketsInQueue);
		output = new Stream(config.maxPacketsInQueue);
		threading = new Threading();
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
		id = -1;
	}

	/**
	* Libera los sockets y cancela los threads.
	*/
	public void Close() {
		Debug.Log("Releasing client " + id + "...");
		threading.Shutdown();
		local.Close();
		server.Close();
	}

	/**
	* Levanta los threads de entrada y salida de paquetes, efectivamente
	* comenzando a recibir y enviar información hacia el servidor. Además,
	* envía un paquete solicitando conectarse al servidor, junto con su
	* dirección IP y puerto.
	*/
	public void Raise() {
		Debug.Log("Client listening on 0.0.0.0:" + config.clientListeningPort + "...");
		Debug.Log("\twith link to server on " + config.serverAddress + ":" + config.serverListeningPort + ".");
		threading.Submit(ProcessInput);
		threading.Submit(ProcessOutput);
		TryConnect();
	}

	/**
	* Bucle principal del cliente.
	*/
	public void Process() {
		Packet packet = input.Read(PacketType.ACK);
		if (packet != null) {
			Debug.Log("Connection ACK received...");
			FinishConnect(packet);
		}
	}

	/**
	* Recibe paquetes desde el servidor, y los agrega al demultiplexer de entrada.
	*/
	protected void ProcessInput() {
		while (!config.OnExit()) {
			byte [] payload = local.Receive(server);
			if (payload != null) {
				Debug.Log("Receiving from server (" + payload.Length + " bytes)...");
				Packet packet = new Packet(payload);
				input.Write(packet);
			}
		}
	}

	/**
	* Envía los paquetes hacia el servidor, si hay disponibles.
	*/
	protected void ProcessOutput() {
		while (!config.OnExit()) {
			if (0 < local.Send(server, output)) {
				Debug.Log("Sending to server...");
			}
		}
	}

	/**
	* Envía un paquete reliable de tipo EVENT solicitando entrar a la partida.
	* El servidor generará un ID y lo enviará devuelta, indicando que
	* efectivamente se ha unido. Si la sala estaba llena, recibe un ID
	* negativo.
	*/
	public void TryConnect() {
		Packet packet = new Packet.Builder(32)
			.AddPacketType(PacketType.EVENT)
			.AddString(config.clientAddress)
			.AddInteger(config.clientListeningPort)
			.Build();
		output.Write(packet);
	}

	/**
	* Luego de recibir una conexión satisfactoria, despliega los objetos
	* necesarios para comenzar la simulación.
	*/
	public void FinishConnect(Packet packet) {
		packet.Reset(1);
		id = packet.GetInteger();
		Debug.Log("Cliente conectado con HP = " + packet.GetInteger() + "! El servidor envió el ID = " + id + ".");
		config.CreatePlayer(packet.GetVector(), packet.GetQuaternion());
		packet.Reset();
	}
}
