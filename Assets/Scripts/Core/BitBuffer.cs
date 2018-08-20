
	using System;
	using System.IO;
	using UnityEngine;
	using UnityEngine.Assertions;

/*
		* Permite almacenar un flujo de bits dentro de un flujo de bytes,
		* aprovechando el espacio disponible en el último.
		*/

	public class BitBuffer {

		protected long bits;
		protected int currentBitCount;
		protected MemoryStream buffer;

		public void PutBit(bool value)
		{
			long longValue = value? 1L : 0L;
			bits |= longValue << currentBitCount;
			++currentBitCount;
			WriteIfNecessary();
		}

		public long getData()
		{
			return bits;
		}

		public int getCount()
		{
			return currentBitCount;
		}

		public void PutBits(long value, int bitCount)
		{
			long mask = 0;
			for (int i = 0; i < bitCount; i++)
			{
				mask <<= 1;
				mask++;
			}

			long longValue = value & mask;
			bits |= longValue << currentBitCount;
			currentBitCount += bitCount;
			WriteIfNecessary();
		}

		public void PutInt(int value, int min, int max)
		{
			if (value < min || value > max)
			{
				throw new InvalidOperationException("Arguments error");
			}
			int bitCount = 0;
			int counter = max - min;
			while (counter > 0)
			{
				counter >>= 1;
				bitCount++;
			}

			int writeVal = value - min;
			bits |= writeVal << currentBitCount;
			currentBitCount += bitCount;
			WriteIfNecessary();
		}

		public void PutFloat(float value, float min, float max, float step)
		{
			
		}

		public void Flush()
		{
			
		}

		public bool GetBit()
		{
			ReadIfNecessary();
			long ret = bits & (1L<<currentBitCount);
			currentBitCount--;
			if (ret == 0)
				return false;
			return true;
		}
		public BitBuffer()
		{
			buffer = new MemoryStream();
		}

		public BitBuffer(byte[] payload, long bits, int currentBitCount)
		{
			buffer = new MemoryStream();
			for (int i=0;i<payload.Length;i++)
			{
				buffer.WriteByte(payload[i]);
			}

			this.bits = bits;
			this.currentBitCount = currentBitCount;
		}

		public byte[] getPayload()
		{
			return buffer.ToArray();
		}

		private void WriteIfNecessary()
		{
			if (32 <= currentBitCount)
			{
				Debug.Log("Writing");
				if (4 + buffer.Position > buffer.Capacity)
				{
					throw new InvalidOperationException("Write buffer overflow");
				}
				int word = (int) bits;
				byte a = (byte) (word);
				byte b = (byte) (word >> 8);
				byte c = (byte) (word >> 16);
				byte d = (byte) (word >> 24);
				buffer.WriteByte(d);
				buffer.WriteByte(c);
				buffer.WriteByte(b);
				buffer.WriteByte(a);
				bits >>= 32;
				currentBitCount -= 32;
			}
		}

		private void ReadIfNecessary()
		{
			if (32 >= currentBitCount)
			{
				Debug.Log("Reading");
				bits <<= 32;
				int word = (byte)(buffer.ReadByte());
				word |= (byte)(buffer.ReadByte() << 8);
				word |= (byte)(buffer.ReadByte() << 16);
				word |=(byte) (buffer.ReadByte() << 24);
				bits |= word;
				currentBitCount += 32;
			}
		}
	}
