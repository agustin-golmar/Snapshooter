
	using System;
	using System.Collections.Generic;

		/*
		* Permite demultiplexar un stream de paquetes en varios streams, uno
		* para cada tipo disponible. En general, es útil para organizar un
		* flujo de entrada.
		*/

	public class Demultiplexer {

		protected readonly Stream [] streams;

		public Demultiplexer(int maxPacketsInQueue) {
			streams = new Stream [Enum.GetValues(typeof(PacketType)).Length];
			for (int i = 0; i < streams.Length; ++i) {
				streams[i] = new Stream(maxPacketsInQueue);
			}
		}

		public Stream GetStream(PacketType type) {
			return streams[(int) type];
		}

		public Packet Read(PacketType type) {
			return GetStream(type).Read();
		}

		public Queue<Packet> ReadAll(PacketType type) {
			return GetStream(type).ReadAll();
		}

		public Packet SoftRead(PacketType type) {
			return GetStream(type).SoftRead();
		}

		public Demultiplexer Write(Packet packet) {
			PacketType type = packet.GetPacketType();
			return Write(type, packet.Reset());
		}

		public Demultiplexer Write(PacketType type, Packet packet) {
			GetStream(type).Write(packet);
			return this;
		}
	}
