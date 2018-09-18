
	using System;

		/*
		* Motor principal de transporte de paquetes.
		*/

	// Podría implementar también la interfaz ITransporter ?
	public class TransportLayer {

		protected ITransporter [] transporters;

		public TransportLayer() {
			transporters = new ITransporter[Enum.GetValues(typeof(PacketType)).Length];
		}

		public TransportLayer SetTransporter(ITransporter transporter) {
			transporters[(int) transporter.GetSupportedPacket()] = transporter;
			return this;
		}

		public Packet Read(PacketType type) {
			return transporters[(int) type].Read();
		}

		public TransportLayer Write(Packet packet) {
			PacketType type = packet.GetPacketType();
			Write(type, packet.Reset());
			return this;
		}

		public TransportLayer Write(PacketType type, Packet packet) {
			transporters[(int) type].Write(packet);
			return this;
		}
	}
