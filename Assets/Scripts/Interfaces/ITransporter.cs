
		/*
		* Representa un mecanismo de transporte de paquetes.
		*/

	public interface ITransporter {

		/*
		* Recibe un paquete ACK con un subtipo soportado. El transportador puede o
		* no modificar su estado en función del ACK recibido.
		*/
		ITransporter Acknowledge(Packet packet);

		/*
		* Devuelve el tipo de paquetes que este transportador soporta.
		*/
		PacketType GetSupportedPacket();

		/*
		* Devuelve el siguiente paquete, en caso de que uno exista, o <null> en caso
		* contrario.
		*/
		Packet Read();

		/*
		* Agrega un paquete al transportador, para que este decida qué política de
		* envio utilizar.
		*/
		ITransporter Write(Packet packet);
	}
