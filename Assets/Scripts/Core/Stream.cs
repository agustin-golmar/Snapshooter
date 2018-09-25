
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

		public Stream Pop() {
			Read();
			return this;
		}

		public Packet Read() {
			lock (streamLock) {
				if (0 < packets.Count) {
					return packets.Dequeue();
				}
				else return null;
			}
		}

		public Queue<Packet> ReadAll() {
			lock (streamLock) {
				Queue<Packet> packets = new Queue<Packet>(this.packets);
				this.packets.Clear();
				return packets;
			}
		}

		public Packet SoftRead() {
			lock (streamLock) {
				if (0 < packets.Count) {
					return packets.Peek();
				}
				else return null;
			}
		}

		public Stream Write(Packet packet) {
			lock (streamLock) {
				packets.Enqueue(packet);
				if (maxPacketsInQueue < packets.Count) {
					Pop();
				}
			}
			return this;
		}
	}
