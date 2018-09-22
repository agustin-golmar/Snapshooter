
	using System.Threading;
	using UnityEngine;

		/*
		* Representante de un jugador remoto.
		*/

	public class RemotePlayer : Player {

		protected SnapshotInterpolator interpolator;

		public RemotePlayer() {
			// Interpolar de a 2 snapshots (no puede ser menos de 2), a 10 SPS:
			interpolator = new SnapshotInterpolator(2, 10);
		}

		protected void Update() {
			// Obtener todas las snapshots disponibles:
			while (true) {
				Packet packet = input.Read(PacketType.SNAPSHOT);
				if (packet != null) {
					interpolator.AddPacket(packet);
					Debug.Log("Nuevo paquete de snapshot agregado.");
				}
				else break;
			}
			// Se puede interpolar, sólo si hay snapshots:
			Snapshot snapshot = interpolator.GetSnapshot();
			if (snapshot != null) {
				transform.SetPositionAndRotation(snapshot.GetPosition(), snapshot.GetRotation());
				/* hack */transform.Translate(0.0f, 1.0f, 0.0f);
			}
			else {
				Debug.Log("\tNo se puede interpolar, la snapshot es nula.");
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
			Demultiplexer localInput = config.GetPlayer(0).GetInputDemultiplexer();
			Link localLink = config.GetLink(0);
			while (!config.OnEscape()) {
				byte [] payload = localLink.Receive(link);
				if (payload != null) {
					Packet packet = new Packet(payload);
					PacketType type = packet.GetPacketType();
					packet.Reset();
					switch (type) {
						case PacketType.ACK : {
							localInput.Write(type, packet);
							break;
						}
						default : {
							// Se debe registrar y se debe generar un ACK de respuesta
							// en caso de que sea EVENT o FLOODING.
							input.Write(packet);
							break;
						}
					}
					Thread.Sleep(config.replicationLag);
				}
			}
		}
	}
