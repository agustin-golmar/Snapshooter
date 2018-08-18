
	using System.Threading;
	using UnityEngine;

		/*
		* Representante de un jugador remoto.
		*/

	public class RemotePlayer : Player {

		void Update() {
			Packet packet = stream.Read();
			if (packet != null) {
				// Si hay nuevos datos, actualizar estado.
				Debug.Log("Packet size: " + packet.GetPayload().Length);
				Debug.Log("Message: " + packet.GetString());
				transform.Translate(packet.GetVector());
			}
		}

		public override bool ShouldBind() {
			return false;
		}

		public override void Replicate() {
			Debug.Log("Enemy deployed: " + config.peerIPs[id] + ":" + config.peerPorts[id]);
			while (!config.OnStart()) {
				Thread.Sleep(100);
			}
			while (!config.OnEscape()) {
				byte [] payload = config.GetLink(0).Receive(link);
				if (payload != null) {
					stream.Write(new Packet(payload));
					Thread.Sleep(config.replicationLag);
				}
			}
		}
	}
