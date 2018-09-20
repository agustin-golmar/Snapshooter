
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
				
				//Debug.Log("Data2: "+Convert.ToString(bb.getData(),2));
				//Debug.Log("Count2: "+bb.getCount());
				//Debug.Log("Buffer2 len: "+bb.getBuffer().Length);
				Debug.Log("Valor1: "+Convert.ToString(bb.GetBits(20),16));
				Debug.Log("Valor2: "+Convert.ToString(bb.GetBits(3),16));
				Debug.Log("Valor3: "+Convert.ToString(bb.GetInt(0,10)));
				Debug.Log("Valor4: "+Convert.ToString(bb.GetInt(6,15)));
				Debug.Log("Float: "+Convert.ToString(bb.GetFloat(1.0f,3.0f,0.1f)));
				//Debug.Log("Count2': "+bb.getCount());

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
