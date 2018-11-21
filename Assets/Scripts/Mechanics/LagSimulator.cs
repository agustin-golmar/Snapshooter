using System.Collections.Generic;

	/**
	* Simulador de lag. Permite retener paquetes durante cierto tiempo de
	* espera ficticio (lag).
	*/

public class LagSimulator {

	protected Configuration config;
	protected Queue<FuturePacket> packets;

	public LagSimulator(Configuration config) {
		this.config = config;
		packets = new Queue<FuturePacket>();
	}

	/**
	* Intenta extraer un paquete de la cola, cuyo tiempo de retención haya sido
	* superado. En caso contrario, devuelve null.
	*/
	public Packet Read(float timestamp) {
		if (0 < packets.Count) {
			FuturePacket future = packets.Peek();
			if (future.timestamp <= timestamp) {
				packets.Dequeue();
				return future.packet;
			}
		}
		return null;
	}

	/**
	* Agrega un paquete a la cola, junto con el timestamp futuro. El paquete
	* será retenido en la cola hasta que se alcance dicho instante.
	*/
	public LagSimulator Write(Packet packet, float timestamp) {
		float time = timestamp + 1.0E-3f * config.lag;
		packets.Enqueue(new FuturePacket(packet, time));
		return this;
	}

	protected class FuturePacket {

		public Packet packet;
		public float timestamp;

		public FuturePacket(Packet packet, float timestamp) {
			this.packet = packet;
			this.timestamp = timestamp;
		}
	}
}
