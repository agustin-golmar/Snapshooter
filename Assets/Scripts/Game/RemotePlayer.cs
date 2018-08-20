
	using System;
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
				BitBuffer bb = packet.GetBitBuffer();
				
				Debug.Log("Data2: "+Convert.ToString(bb.getData(),2));
				Debug.Log("Count2: "+bb.getCount());
				Debug.Log("Buffer2: "+bb.getPayload());
				Debug.Log("V1: "+bb.GetBit());
				Debug.Log("V2: "+bb.GetBit());
				Debug.Log("V3: "+bb.GetBit());
				Debug.Log("V4: "+bb.GetBit());
				Debug.Log("V5: "+bb.GetBit());
				Debug.Log("V6: "+bb.GetBit());
				Debug.Log("V7: "+bb.GetBit());
				Debug.Log("V8: "+bb.GetBit());
				Debug.Log("Data2': "+Convert.ToString(bb.getData(),2));
				Debug.Log("Count2': "+bb.getCount());
				
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
