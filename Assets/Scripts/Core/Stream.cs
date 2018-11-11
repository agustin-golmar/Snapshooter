using System.Collections.Generic;

	/*
	* Permite almacenar un flujo de paquetes por orden de llegada (FIFO), y
	* ofrece mecanismos para acceder a ellos. Los accesos son thread-safe.
	*/

public class Stream {

	protected readonly object streamLock = new object();
	protected readonly Queue<Packet> packets;
	protected readonly int maxPacketsInQueue;

	public Stream(int maxPacketsInQueue) {
		this.maxPacketsInQueue = maxPacketsInQueue;
		packets = new Queue<Packet>();
	}

	/**
	* Elimina el elemento más viejo del Stream, sin leerlo.
	*/
	public Stream Pop() {
		Read();
		return this;
	}

	/**
	* Devuelve el elemento más viejo del Stream, o null si el Stream estaba
	* vacío.
	*/
	public Packet Read() {
		lock (streamLock) {
			if (0 < packets.Count) {
				return packets.Dequeue();
			}
			else return null;
		}
	}

	/**
	* Devuelve todos los paquetes en el Stream, en el orden de llegada,
	* efectivamente vaciando el Stream por completo. Al leer de una sola vez
	* se reducen la cantidad de locks adquiridos, disminuyendo la contención.
	*/
	public Queue<Packet> ReadAll() {
		lock (streamLock) {
			Queue<Packet> packets = new Queue<Packet>(this.packets);
			this.packets.Clear();
			return packets;
		}
	}

	/**
	* Devuelve el paquete más antiguo, pero no lo elimina del Stream.
	*/
	public Packet SoftRead() {
		lock (streamLock) {
			if (0 < packets.Count) {
				return packets.Peek();
			}
			else return null;
		}
	}

	/**
	* Agrega un paquete al Stream. El paquete se inserta al final de la cola.
	* Si se supera el límite de paquetes en la cola, se eliminan los más
	* viejos.
	*/
	public Stream Write(Packet packet) {
		lock (streamLock) {
			packets.Enqueue(packet);
			if (maxPacketsInQueue < packets.Count) {
				Pop();
			}
		}
		return this;
	}

	/**
	* Al igual que ReadAll, efectúa una escritura de paquetes adquiriendo el
	* candado por única vez, reduciendo la contención. Si se supera el límite
	* aceptado en el Stream, se eliminan los más viejos.
	*/
	public Stream WriteAll(IEnumerable<Packet> packets) {
		lock (streamLock) {
			foreach (Packet packet in packets) {
				this.packets.Enqueue(packet);
			}
			while (maxPacketsInQueue < this.packets.Count) {
				Pop();
			}
		}
		return this;
	}
}
