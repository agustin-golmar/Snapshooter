using System.Collections.Generic;
using UnityEngine;

	/*
	* Configuración global interna de la escena. Punto de entrada principal de
	* una instancia de Snapshooter.
	*/

public class Configuration : MonoBehaviour {

	// Instancia del servidor y del cliente:
	protected Server server;
	protected Client client;

	// Recursos que deben liberarse:
	protected List<IClosable> resources;

	// Indica si se debe abortar la ejecución:
	protected bool onExit;

	// Indica si se trata de un servidor o no:
	public bool isServer;

	// Indica si también se debería desplegar un cliente:
	public bool isClient;

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

	// Timeouts para envío y recepción de paquetes (en [ms]):
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

	// Latencia virtual (en [ms]):
	public int lag;

	// Timeout para eventos reliable (en [s]):
	public int timeout;

	// Porcentaje de pérdida de paquetes (entre 0 y 1):
	public float packetLossRatio;

	// Velocidad de los jugadores en [m/s]:
	public float playerSpeed;

	/**
	* Carga la escena compelta, e instancia un servidor y/o cliente según se
	* indique en la configuración.
	*/
	protected void Start() {
		Debug.Log("Loading scene...");
		onExit = false;
		resources = new List<IClosable>();
		if (isServer) {
			Debug.Log("Loading server instance...");
			server = new Server(this);
			resources.Add(server);
			server.Raise();
		}
		if (isClient) {
			Debug.Log("Loading client instance...");
			client = new Client(this);
			resources.Add(client);
			client.Raise();
		}
		Debug.Log("Scene loaded.");
	}

	/**
	* Se encarga de ejecutar el ciclo principal del servidor y/o cliente en el
	* thread principal lo cual permite utilizar el API de Unity dentro de estas
	* instancias.
	*/
	protected void Update() {
		if (isServer) {
			server.FrameHandler();
		}
		if (isClient) {
			client.FrameHandler();
		}
	}

	/**
	* Libera los recursos utilizados (en especial, los threads desplegados), al
	* finaizar la ejecución del juego completo.
	*/
	protected void OnApplicationQuit() {
		Debug.Log("Finishing...");
		onExit = true;
		foreach (IClosable resource in resources) {
			resource.Close();
		}
		resources.Clear();
		Debug.Log("Finished.");
	}

	/** **********************************************************************
	******************************* PUBLIC API ********************************
	 *********************************************************************** */

	/**
	* Permite que otros threads controlen y estén alerta de la finalización del
	* juego, lo antes posible.
	*/
	public bool OnExit() {
		return onExit;
	}
}
