
	using System.Threading;
	using UnityEngine;

		/*
		* Configuración global interna de la escena.
		*/

	public class Configuration : MonoBehaviour {

		// Configuración:
		public GameObject remotePlayerPrefab;
		public Vector3 [] respawns;
		public string [] peerIPs;
		public int [] peerPorts;
		public int maxPacketSize;
		public int maxPacketsInQueue;
		public int receiveTimeout;
		public int sendTimeout;
		public int snapshotsPerSecond;
		public int windowSize;
		public int lag;
		public float packetLost;

		// Servicios globales:
		protected Threading threading;
		protected Player [] players;
		protected Link [] links;

		// Estado global:
		protected bool onEscape;

		protected void Awake() {
		}

		protected void OnApplicationQuit() {
			Debug.Log("Exiting...");
			onEscape = true;
			threading.Shutdown();
			Thread.Sleep(100);
			Debug.Log("Finish.");
		}

		protected void Start() {
			Debug.Log("Starting scene...");
			threading = new Threading(peerIPs.Length);
			links = new Link [peerIPs.Length];
			players = new Player [peerIPs.Length];
			onEscape = false;
			LoadPlayers().Deploy();
			Debug.Log("Scene loaded.");
		}

		protected void Update() {
		}

		public Link GetLink(int index) {
			return links[index];
		}

		public Link [] GetLinks() {
			return links;
		}

		public Player GetPlayer(int index) {
			return players[index];
		}

		public bool OnEscape() {
			return onEscape;
		}

		protected Configuration Deploy() {
			for (int i = 0; i < players.Length; ++i) {
				Player player = players[i];
				player.SetID(i);
				links[i] = new Link.Builder()
					.IP(peerIPs[i])
					.Port(peerPorts[i])
					.Bind(player.ShouldBind())
					.ReceiveTimeout(receiveTimeout)
					.SendTimeout(sendTimeout)
					.Build();
				threading.Submit(i, new ThreadStart(player.Replicate));
			}
			return this;
		}

		protected Configuration LoadPlayers() {
			players[0] = GameObject.Find("LocalPlayer")
				.GetComponent<LocalPlayer>();
			for (int i = 1; i < players.Length; ++i) {
				players[i] = Instantiate(
					remotePlayerPrefab,
					respawns[i],
					Quaternion.identity)
						.GetComponent<RemotePlayer>();
			}
			return this;
		}
	}
