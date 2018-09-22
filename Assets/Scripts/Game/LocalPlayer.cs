
	using System.Threading;
	using UnityEngine;

		/*
		* Representa un jugador local.
		*/

	public class LocalPlayer : Player {

		// Comandos de movimiento:
		private const string KEY_W = "w";
		private const string KEY_A = "a";
		private const string KEY_S = "s";
		private const string KEY_D = "d";

		// N° de secuencia local:
		protected int sequence;

		// Velocidad, en [m/s]:
		public float speed;

		protected new void Start() {
			base.Start();
			sequence = 0;
		}

		protected void Update() {
			Move();
			float currentTime = Time.fixedUnscaledTime;
			if (deltaSnapshot < currentTime - lastSnapshot) {
				lastSnapshot = currentTime;
				Debug.Log("Frame " + Time.frameCount + " -> " + currentTime + " sec.");
				Packet packet = new Packet.Builder(config.maxPacketSize)
					.AddPacketType(PacketType.SNAPSHOT)
					.AddInteger(sequence++)
					.AddFloat(currentTime)
					.AddVector(transform.position)
					.AddQuaternion(transform.rotation)
					.Build();
				output.Write(packet);
			}
		}

		protected void LateUpdate() {
			Camera.main.transform.LookAt(transform);
		}

		public void Move() {
			float delta = speed * Time.deltaTime;
			if (Input.GetKey(KEY_W)) {
				transform.Translate(0, 0, delta);
			}
			if (Input.GetKey(KEY_A)) {
				transform.Rotate(Vector3.down, 30.0f * delta);
			}
			if (Input.GetKey(KEY_S)) {
				transform.Translate(0, 0, -delta);
			}
			if (Input.GetKey(KEY_D)) {
				transform.Rotate(Vector3.up, 30.0f * delta);
			}
		}

		public override bool ShouldBind() {
			return true;
		}

		public override void Replicate() {
			// UDP puede ser lento debido a ARP. Non-blocking?
			while (!config.OnStart()) {
				Thread.Sleep(100);
			}
			while (!config.OnEscape()) {
				// Enviar también ACKs hacia los demás links.
				// Para esto se utilizan los output buffers de los RemotePlayer's.
				// Deberia utilizarse otro thread con round-robin.
				link.Multicast(config.GetLinks(), 1, output);
				Thread.Sleep(config.replicationLag);
			}
		}
	}
