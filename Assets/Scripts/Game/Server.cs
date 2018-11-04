using System.Collections.Generic;
using UnityEngine;

public class Server : IClosable {

	public const int SERVER_LINK = 0;

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
			.ReceiveTimeout(config.receiveTimeout)
			.SendTimeout(config.sendTimeout)
			.Build();
		links = new List<Link>();
		snapshot = new Snapshot();
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
	* Aplica round-robin: por cada enlace de entrada (cada cliente conectado), lee
	* un paquete (si puede), y lo agrega al Demultiplexer.
	*/
	protected void ProcessInput() {
		while (!config.OnExit()) {
			int id = 0;
			foreach (Link link in links) {
				Debug.Log("Receiving from client " + id + "...");
				byte [] payload = local.Receive(link);
				Packet packet = new Packet(payload);
				input.Write(packet);
				++id;
			}
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
				Debug.Log("Sending to client " + id + "...");
				local.Send(links[id], output);
				++id;
			}
		}
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
			// Generar snapshot global y enviar a todos, es decir, agrear a todos los outputs.
			/*
			Packet packet = null;
			foreach (Stream output in outputs) {
				output.Write(packet);
			}
			*/
		}
	}
}
