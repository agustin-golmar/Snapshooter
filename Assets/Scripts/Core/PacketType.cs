
		/*
		* Tipo de paquete. Cada tipo de paquete debe ser manejado con una
		* semántica diferente por el mecanismo de transporte.
		*/

	public enum PacketType : byte {

		ACK,
		SNAPSHOT,
		EVENT,
		FLOODING
	}
