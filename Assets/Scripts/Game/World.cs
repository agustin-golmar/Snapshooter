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
	protected int grenades;

	// Jugador local:
	protected Player player;

	// Último enemigo creado:
	protected int lastEnemy;

	protected void Start() {
		Debug.Log("Loading world...");
		config = GameObject.Find("Configuration").GetComponent<Configuration>();
		snapshot = null;
		player = null;
		players = 1;
		grenades = 0;
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
				Debug.Log("Local Player ID = " + player.GetID());
				Debug.Log("Found new player (old was " + players + "): " + snapshot.players);
				Debug.Log("Creating enemy with ID = " + lastEnemy);
				Debug.Log("  Snapshot: " + snapshot);
				Debug.Log("  Snapshot lengths: " + snapshot.positions.Length + " | " + snapshot.rotations.Length);
				Debug.Log("  Position: " + snapshot.positions[lastEnemy]);
				Debug.Log("  Rotation: " + snapshot.rotations[lastEnemy]);
				Debug.Log("  ID: " + snapshot.ids[lastEnemy]);

				/*
				* players = 1 siempre, porque el jugador local ya fue instanciado.
				* La cantidad de jugadores no es igual a 1 porque había más en la sala.
				* Empiezo en lastEnemy = 0.
				* Si lastEnemy es el jugador local, no hago nada. lastEnemy = 1.
				* Si lastEnemy es diferente del ID local, entonces creo un nuevo enemigo con lastEnemy como ID.
				* Se incrementa players.
				*/
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
				// El primer jugador toma el último
				// [0]
				// El segundo
				// 0 - [1]
			}
			else if (snapshot.players < players) {
				// Se eliminó un jugador.
				// No debería pasar nunca porque no hay un evento LEAVE.
			}
			else {
				// La cantidad de jugadores no cambió. No se hace nada.
			}
			/*if (grenades < snapshot.grenades) {
				Debug.Log("Found new grenade (old was " + grenades + "): " + snapshot.grenades);
				// Se creó una granada. El parámetro 'fuse' debe ser positivo.
				++grenades;
				CreateGrenade(snapshot.gPositions[players], snapshot.gRotations[players])
					.SetID(snapshot.ids[players])
					.SetSnapshot(snapshot);
			}
			else if (snapshot.grenades < grenades) {
				// Se eliminó un jugador.
				// No debería pasar nunca porque no hay un evento LEAVE.
			}
			else {
				// La cantidad de jugadores no cambió. No se hace nada.
			}*/
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
}
