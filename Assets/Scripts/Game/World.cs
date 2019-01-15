using System.Collections.Generic;
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

	// Prefab de una granada y su sombra:
	public GameObject grenadePrefab;
	public GameObject shallowGrenadePrefab;

	// Configuración global:
	protected Configuration config;

	// El estado del mundo:
	protected Snapshot snapshot;

	// Cantidad de jugadores actualmente:
	protected int players;

	// Jugador local:
	protected Player player;

	// Último enemigo creado:
	protected int lastEnemy;

	// Granadas lanzadas:
	protected HashSet<int> grenades = new HashSet<int>();

	protected void Start() {
		Debug.Log("Loading world...");
		config = GameObject.Find("Configuration").GetComponent<Configuration>();
		snapshot = null;
		player = null;
		players = 1;
		lastEnemy = 0;
		Debug.Log("World loaded.");
	}

	/**
	* Debe crear o destruir entidades según la snapshot actual, es decir, según
	* se agreguen o destruyan jugadores o granadas.
	*/
	protected void Update() {
		if (snapshot != null && player != null) {
			if (players < snapshot.players) {
				if (lastEnemy == player.GetID()) {
					++lastEnemy;
				}
				else {
					CreateEnemy(snapshot.positions[lastEnemy], snapshot.rotations[lastEnemy])
						.SetID(snapshot.ids[lastEnemy])
						.SetSnapshot(snapshot);
					++lastEnemy;
					++players;
				}
			}
			else if (snapshot.players < players) {
				// Se eliminó un jugador.
				// No debería pasar nunca porque no hay un evento LEAVE.
			}
			else {
				// La cantidad de jugadores no cambió. No se hace nada.
			}
			// Si no es server, crear granadas nuevas:
			if (!config.isServer) {
				for (int k = 0; k < snapshot.players; ++k) {
					if (0 < snapshot.gFuses[k] && !grenades.Contains(k)) {
						grenades.Add(k);
						CreateShallowGrenade(snapshot.positions[k], snapshot.rotations[k])
							.SetSnapshot(snapshot)
							.SetID(k);
					}
					if (snapshot.gFuses[k] <= 0) {
						grenades.Remove(k);
					}
				}
			}
		}
	}

	/** **********************************************************************
	******************************* PUBLIC API ********************************
	 *********************************************************************** */

	/**
	* Conecta la snapshot global, para que sea accesible al resto de los
	* objetos.
	*/
	public World LoadSnapshot(Snapshot snapshot) {
		this.snapshot = snapshot;
		return this;
	}

	/**
	* Instancia un nuevo GameObject, en una posición y con cierta orientación
	* o rotación específica.
	*/
	public GameObject Create(GameObject prefab, Vector3 position, Quaternion rotation) {
		return Instantiate(prefab, position, rotation);
	}

	/**
	* Instancia un nuevo jugador local (controlado por el usuario).
	*/
	public Player CreatePlayer(Vector3 position, Quaternion rotation) {
		player = Create(playerPrefab, position, rotation)
			.GetComponent<Player>();
		return player;
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
	public Grenade CreateGrenade(Vector3 position, Quaternion rotation) {
		return Create(grenadePrefab, position, rotation)
			.GetComponent<Grenade>();
	}

	/**
	* Crea una granada superficial (replica el estado del servidor).
	*/
	public ShallowGrenade CreateShallowGrenade(Vector3 position, Quaternion rotation) {
		return Create(shallowGrenadePrefab, position, rotation)
			.GetComponent<ShallowGrenade>();
	}
}
