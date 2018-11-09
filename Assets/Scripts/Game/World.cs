using UnityEngine;

	/**
	* Se encarga de cargar todo el escenario y los jugadores, según el estado
	* de las snapshots.
	*/

public class World : MonoBehaviour {

	// Prefab de un jugador:
	public GameObject playerPrefab;

	// Prefab de un enemigo:
	public GameObject enemyPrefab;

	// Prefab de una granada:
	public GameObject grenadePrefab;

	// Configuración global:
	protected Configuration config;

	// El estado del mundo:
	protected Snapshot snapshot;

	// Cantidad de jugadores actualmente:
	protected int players;

	// Cantidad de granadas actualmente:
	protected int frags;

	protected void Start() {
		Debug.Log("Loading world...");
		config = GameObject.Find("Configuration").GetComponent<Configuration>();
		snapshot = null;
		players = 1;
		frags = 0;
		Debug.Log("World loaded.");
	}

	/**
	* Debe crear o destruir entidades según la snapshot actual, es decir, según
	* se agreguen o destruyan jugadores o granadas.
	*/
	protected void Update() {
		if (snapshot != null) {
			if (players < snapshot.players) {
				Debug.Log("Found new player (old was " + players + "): " + snapshot.players);
				// Se creó un jugador. Siempre es el último en la lista.
				++players;
				Enemy enemy = CreateEnemy(snapshot.positions[players], snapshot.rotations[players]);
				enemy.SetID(snapshot.ids[players]);
				enemy.SetSnapshot(snapshot);
			}
			else if (snapshot.players < players) {
				// Se eliminó un jugador.
				// No debería pasar nunca porque no hay un evento LEAVE.
			}
			else {
				// La cantidad de jugadores no cambió. No se hace nada.
			}
		}
	}

	/** **********************************************************************
	******************************* PUBLIC API ********************************
	 *********************************************************************** */

	/**
	* Instancia un nuevo GameObject, en una posición y con cierta orientación
	* o rotación específica.
	*/
	public GameObject Create(GameObject prefab, Vector3 position, Quaternion rotation) {
		return Instantiate(playerPrefab, position, rotation);
	}

	/**
	* Instancia un nuevo jugador local (controlado por el usuario).
	*/
	public Player CreatePlayer(Vector3 position, Quaternion rotation) {
		return Create(playerPrefab, position, rotation)
			.GetComponent<Player>();
	}

	/**
	* Instancia un enemigo. Se controla automáticamente por snapshot.
	*/
	public Enemy CreateEnemy(Vector3 position, Quaternion rotation) {
		return Create(enemyPrefab, position, rotation)
			.GetComponent<Enemy>();
	}

	/**
	* Crea una granada (es controlada por el servidor).
	*/
	public Grenade CreateGrenade(Vector3 position) {
		return Create(grenadePrefab, position, Quaternion.identity)
			.GetComponent<Grenade>();
	}
}
