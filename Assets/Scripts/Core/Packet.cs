using System;
using System.Text;
using UnityEngine;

	/*
	* Un builder para construir paquetes de forma incremental. El resultado
	* debe ser un flujo de bytes, listo para ser agregado a un Stream. El
	* buffer interno no debe contener bytes adicionales (no usados).
	*
	* Se proveen métodos tanto para serializar como para deserializar el
	* contenido del paquete, por lo cual es ideal que un formato específico
	* se implemente en otras clases, a modo de wrappers.
	*/

public class Packet {

	protected readonly byte [] payload;
	protected int position;

	public Packet(byte [] payload) {
		this.payload = payload;
		position = 0;
	}

	protected Packet(Builder builder) {
		position = 0;
		if (builder.position == builder.payload.Length) {
			payload = builder.payload;
		}
		else {
			payload = new byte [builder.position];
			Array.Copy(builder.payload, payload, builder.position);
		}
	}

	public byte [] GetPayload() {
		return payload;
	}

	public byte GetByte() {
		return payload[position++];
	}

	public Direction GetDirection() {
		return (Direction) GetByte();
	}

	public Endpoint GetEndpoint() {
		return (Endpoint) GetByte();
	}

	public float GetFloat() {
		float data = BitConverter.ToSingle(payload, position);
		position += 4;
		return data;
	}

	public int GetInteger() {
		int data = BitConverter.ToInt32(payload, position);
		position += 4;
		return data;
	}

	public PacketType GetPacketType() {
		return (PacketType) GetByte();
	}

	public Quaternion GetQuaternion() {
		return new Quaternion(GetFloat(), GetFloat(), GetFloat(), GetFloat());
	}

	public Vector3 GetVector() {
		return new Vector3(GetFloat(), GetFloat(), GetFloat());
	}

	public string GetString() {
		byte length = GetByte();
		string data = Encoding.UTF8.GetString(payload, position, length);
		position += length;
		return data;
	}

	public BitBuffer GetBitBuffer() {
		//long data = BitConverter.ToInt64(payload, position);
		//position += 8;
		//int count = BitConverter.ToInt32(payload, position);
		//position += 4;
		byte length = GetByte();
		byte[] buffer = new byte[length];
		//Debug.Log("BUFLEN: " + length);
		for (int i = 0; i < length; ++i) {
			buffer[i] = GetByte();
		}
		return new BitBuffer(buffer);
	}

	public Packet Reset(int position) {
		this.position = position;
		return this;
	}

	public Packet Reset() {
		return Reset(0);
	}

	public class Builder {

		public readonly byte [] payload;
		public int position;

		public Builder(int maxPacketSize) {
			payload = new byte[maxPacketSize];
			position = 0;
		}

		public Builder AddByte(byte data) {
			payload[position++] = data;
			return this;
		}

		public Builder AddDirection(Direction type) {
			return AddByte((byte) type);
		}

		public Builder AddEndpoint(Endpoint type) {
			return AddByte((byte) type);
		}

		public Builder AddFloat(float data) {
			return AddPayload(BitConverter.GetBytes(data));
		}

		public Builder AddInteger(int data) {
			return AddPayload(BitConverter.GetBytes(data));
		}

		public Builder AddPacketType(PacketType type) {
			return AddByte((byte) type);
		}

		public Builder AddPayload(byte [] data) {
			return AddPayload(data, 0, data.Length);
		}

		public Builder AddPayload(byte [] data, int index, int size) {
			Array.Copy(data, index, payload, position, size);
			position += size;
			return this;
		}

		public Builder AddQuaternion(Quaternion quaternion) {
			// Se puede optimizar?
			// ||(x, y, z, w)|| = 1 siempre?
			// En este caso, solo se transmite (x, y, z)
			// y luego w = sqrt(1 - x^2 + y^2 + z^2)
			AddFloat(quaternion.x);
			AddFloat(quaternion.y);
			AddFloat(quaternion.z);
			return AddFloat(quaternion.w);
		}

		public Builder AddString(string data) {
			byte [] stringPayload = Encoding.UTF8.GetBytes(data);
			AddByte((byte) stringPayload.Length);
			return AddPayload(stringPayload);
		}

		public Builder AddVector(Vector3 vector) {
			AddFloat(vector.x);
			AddFloat(vector.y);
			return AddFloat(vector.z);
		}

		public Builder AddBitBuffer(BitBuffer bb) {
			//AddPayload(BitConverter.GetBytes(bb.getData()));
			//AddPayload(BitConverter.GetBytes(bb.getCount()));
			byte[] payload = bb.GetPayload();
			/* foreach (byte b in payload) {
				Debug.Log("Manda: " + Convert.ToString(b,2));
			}*/
			AddByte((byte)payload.Length);
			AddPayload(payload);
			return this;
		}

		public Packet Build() {
			return new Packet(this);
		}
	}
}
