
	using System.Threading;
	using UnityEngine;

		/*
		* Representante de un jugador remoto.
		*/

	public class RemotePlayer : Player {

		protected SnapshotInterpolator interpolator;

		public RemotePlayer() {
			// Interpolar de a 2 snapshots a 10 SPS:
			interpolator = new SnapshotInterpolator(2, 10);
		}

		protected void Update() {
			Packet packet = input.SoftRead();
			if (packet != null) {
				Debug.Log("Packet size: " + packet.GetPayload().Length);
				PacketType type = packet.GetPacketType();
				packet.Reset();
				switch (type) {
					case PacketType.SNAPSHOT : {
						interpolator.AddPacket(packet);
						input.Pop();
						break;
					}
					case PacketType.EVENT : {
						// Manejar evento...
						break;
					}
					case PacketType.FLOODING : {
						// Manejar flood...
						break;
					}
				}
				Debug.Log("PacketType: " + packet.GetPacketType());
			}
			// La 1er snapshot puede ser null, porque no existe...
			Snapshot snapshot = interpolator.GetSnapshot();
			if (snapshot != null) {
				transform.SetPositionAndRotation(snapshot.GetPosition(), snapshot.GetRotation());
				/* hack */transform.Translate(0, 1, 0);
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
			Stream localInput = config.GetPlayer(0).GetInputStream();
			Link localLink = config.GetLink(0);
			while (!config.OnEscape()) {
				byte [] payload = localLink.Receive(link);
				if (payload != null) {
					Packet packet = new Packet(payload);
					PacketType type = packet.GetPacketType();
					packet.Reset();
					switch (type) {
						case PacketType.ACK : {
							localInput.Write(packet);
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
