using UnityEngine;

public class Client : IClosable {

	protected Configuration config;
	protected Demultiplexer input;
	protected Stream output;
	protected Threading threading;
	protected Link local;
	protected Link server;
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
			.ReceiveTimeout(config.receiveTimeout)
			.SendTimeout(config.sendTimeout)
			.Build();
		server = new Link.Builder()
			.Bind(false)
			.IP(config.serverAddress)
			.Port(config.serverListeningPort)
			.ReceiveTimeout(config.receiveTimeout)
			.SendTimeout(config.sendTimeout)
			.Build();
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
	* comenzando a recibir y enviar información hacia el servidor.
	*/
	public void Raise() {
		Debug.Log("Client listening on 0.0.0.0:" + config.clientListeningPort + "...");
		Debug.Log("\twith link to server on " + config.serverAddress + ":" + config.serverListeningPort + ".");
		threading.Submit(ProcessInput);
		threading.Submit(ProcessOutput);
	}

	/**
	* Recibe paquetes desde el servidor, y los agrega al demultiplexer de entrada.
	*/
	protected void ProcessInput() {
		while (!config.OnExit()) {
			Debug.Log("Receiving from server...");
			byte [] payload = local.Receive(server);
			Packet packet = new Packet(payload);
			input.Write(packet);
		}
	}

	/**
	* Envía los paquetes hacia el servidor, si hay disponibles.
	*/
	protected void ProcessOutput() {
		while (!config.OnExit()) {
			Debug.Log("Sending to server...");
			local.Send(server, output);
		}
	}

	/**
	* Envía un paquete reliable de tipo EVENT solicitando entrar a la partida.
	* El servidor generará un ID y lo enviará devuelta, indicando que
	* efectivamente se ha unido. Si la sala estaba llena, recibe un ID
	* negativo.
	*
	* Si el ID es satisfactorio, se debe reenviar en cada mensaje reliable
	* futuro, sea ACK, EVENT o FLOODING.
	*/
	public void Connect() {
	}
}
