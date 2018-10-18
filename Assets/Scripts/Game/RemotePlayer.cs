
	using System.Collections.Generic;
	using System.Threading;
	using UnityEngine;

		/*
		* Representante de un jugador remoto.
		*/

	public class RemotePlayer : Player {

		protected SnapshotInterpolator interpolator;

		protected void FixedUpdate() {
		}

		protected new void Start() {
			base.Start();
			interpolator = new SnapshotInterpolator(config.windowSize, config.snapshotsPerSecond);
		}

		protected void Update() {
			//Debug.Log("Frame " + Time.frameCount + " -> " + Time.unscaledTime + " sec.");
			Queue<Packet> packets = input.ReadAll(PacketType.SNAPSHOT);
			interpolator.AddPackets(packets);
			Snapshot snapshot = interpolator.GetSnapshot();
			if (snapshot != null) {
				transform.SetPositionAndRotation(snapshot.GetPosition(), snapshot.GetRotation());
				transform.Translate(0.0f, 1.0f, 0.0f); /* hack */
			}
			else {
				//Debug.Log(">>> No se pudo interpolar.");
			}
		}

		public override void Replicate() {
			Debug.Log("Enemy deployed: " + config.peerIPs[id] + ":" + config.peerPorts[id]);
			Thread.Sleep(200);
			Demultiplexer localInput = config.GetPlayer(0).GetInputDemultiplexer();
			Link localLink = config.GetLink(0);
			while (!config.OnEscape()) {
				byte [] payload = localLink.Receive(link);
				if (payload != null) {
					Packet packet = new Packet(payload);
					PacketType type = packet.GetPacketType();
					packet.Reset();
					Debug.Log("Remote receive: " + type);
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
				}
			}
		}

		public override bool ShouldBind() {
			return false;
		}
	}
