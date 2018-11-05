using System.Collections.Generic;
using UnityEngine;

	/*
	* Configuración global interna de la escena.
	*/

public class Configuration : MonoBehaviour {

	// Prefab de un jugador:
	public GameObject playerPrefab;

	// Indica si se trata de un servidor o no:
	public bool isServer;

	// Dirección del servidor remoto:
	public string serverAddress;

	// Puerto de escucha (la IP es 0.0.0.0, siempre):
	public int serverListeningPort;

	// Dirección del cliente:
	public string clientAddress;

	// Puerto de escucha para el cliente (debe ser diferente al del servidor):
	public int clientListeningPort;

	// Cantidad máxima de jugadores:
	public int maxPlayers;

	// Tamñao máximo en bytes de un paquete:
	public int maxPacketSize;

	// Cantidad máxima de paquetes en un stream:
	public int maxPacketsInQueue;

	// Timeouts para envío y recepción de paquetes:
	public int serverReceiveTimeout;
	public int serverSendTimeout;
	public int clientReceiveTimeout;
	public int clientSendTimeout;

	// Snapshots por segundo:
	public int snapshotsPerSecond;

	// Ventana de snapshots:
	public int slidingWindowSize;

	// Habilitar predicción:
	public bool usePrediction;

	// Latencia virtual:
	public int lag;

	// Porcentaje de pérdida de paquetes:
	public float packetLossRatio;

	// Velocidad de los jugadores en [m/s]:
	public float playerSpeed;

	/*************************************************************************/

	// Instancia del servidor y del cliente:
	protected Server server;
	protected Client client;

	// Recursos que deben liberarse:
	protected List<IClosable> resources;

	// Indica si se debe abortar la ejecución:
	protected bool onExit;

	protected void Start() {
		Debug.Log("Starting scene...");
		onExit = false;
		resources = new List<IClosable>();
		if (isServer) {
			Debug.Log("Loading server instance for " + maxPlayers + " players...");
			server = new Server(this);
			resources.Add(server);
			server.Raise();
		}
		Debug.Log("Loading client instance...");
		client = new Client(this);
		resources.Add(client);
		client.Raise();
		Debug.Log("Scene loaded.");
	}

	protected void Update() {
		if (isServer) {
			server.ProcessSnapshot();
		}
		client.Process();
	}

	protected void OnApplicationQuit() {
		Debug.Log("Exiting...");
		onExit = true;
		foreach (IClosable resource in resources) {
			resource.Close();
		}
		resources.Clear();
		Debug.Log("Finished.");
	}

	public bool OnExit() {
		return onExit;
	}

	public Player CreatePlayer(Vector3 position, Quaternion rotation) {
		return Instantiate(playerPrefab, position, rotation)
			.GetComponent<Player>();
	}
}
