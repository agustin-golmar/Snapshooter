
	/**
	* Define el API del servidor. Todos los métodos reciben un paquete, y generan
	* una respuesta en caso de que exista una.
	*/

public interface IAPI {

	Packet Join(Packet request);
	Packet Move(Packet request);
	Packet Shoot(Packet request);
	Packet Frag(Packet request);
}
