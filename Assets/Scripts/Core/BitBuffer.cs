
	using System;
	using System.IO;
	using UnityEngine;
	using UnityEngine.Assertions;

/*
		* Permite almacenar un flujo de bits dentro de un flujo de bytes,
		* aprovechando el espacio disponible en el último.
		*/

	public class BitBuffer {

		protected int bits = 0;
		protected int currentBitCount = 0;
		protected int length = 0;
		protected int seek = 0;
		protected byte [] buffer;

		public void PutBit(bool value)
		{
			int val = value ? 1 : 0;
			bits |= val << currentBitCount;
			currentBitCount++;
			if (currentBitCount >= 8)
			{
				Debug.Log("Corta en: "+Convert.ToString(bits,2));
				buffer[seek++] = (byte)bits;
				length++;
				currentBitCount = 0;
				bits = 0;
			}
		}

		public void PutBits(long value, int bitCount)
		{
		
		}

		public void PutInt(int value, int min, int max)
		{
			
		}

		public void PutFloat(float value, float min, float max, float step)
		{
			
		}

		private void Flush()
		{
			length = 0;
			seek = 0;
			bits = 0;
			currentBitCount = 0;

		}

		public bool GetBit()
		{
			if (currentBitCount == 0)
			{
				bits = buffer[seek];
				Debug.Log("Buffer is: "+Convert.ToString(buffer[seek],2));
				seek++;
				currentBitCount = 8;
			}

			int mask = 1 << (8-currentBitCount);
			bool ret = ((bits & mask) != 0);
			currentBitCount--;
			return ret;

		}
		public BitBuffer()
		{
			buffer = new byte[512];
		}

		public BitBuffer(byte[] payload)
		{
			buffer = new byte[512];
			for (int i=0;i<payload.Length;i++)
			{
				buffer[i] = payload[i];
				length++;
			}
		}

		public byte[] getPayload()
		{
			byte[] ret = new byte[length+1];
			for (int i = 0; i < length; i++)
			{
				
				ret[i] = buffer[i];
			}
			
			ret[length] = (byte)bits;
			Debug.Log("Bits: "+Convert.ToString(bits,2));
			Flush();
			return ret;
		}
	}
