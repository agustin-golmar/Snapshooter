
	using System;
	using UnityEngine;

		/*
		* Permite almacenar un flujo de bits dentro de un flujo de bytes,
		* aprovechando el espacio disponible en el último.
		*/

	public class BitBuffer {
		private long _bits = 0;
		private int _currentBitCount = 0;
		private int _length = 0;
		private int _seek = 0;
		private readonly byte [] _buffer;

		public void PutBit(bool value)
		{
			PutBits(value ? 1 : 0, 1);
		}

		private void AddByte()
		{
			while (_currentBitCount >= 8)
			{
				Debug.Log("Corta en: " + Convert.ToString(_bits, 2));
				_buffer[_seek++] = (byte) _bits;
				_length++;
				_currentBitCount -= 8;
				_bits >>= 8;
			}
		}

		public void PutBits(long value, int bitCount)
		{
			long mask = 0;
			for(int i=0; i<bitCount;i++)
			{
				mask <<= 1;
				mask++;
			}

			long val = value & mask;
			_bits |= (val << _currentBitCount);
			_currentBitCount += bitCount;
			AddByte();


		}

		public void PutInt(int value, int min, int max)
		{
			int range = max - min;
			PutBits(value-min,(int)Math.Log(range,2)+1);

		}

		public int GetInt(int min, int max)
		{
			int range = max - min;
			return (int) GetBits((int) Math.Log(range, 2) + 1) + min;
		}

		public void PutFloat(float value, float min, float max, float step)
		{
			int val = (int) ((value - min) / step);
			int maxi = (int) ((max - min) / step);
			PutInt(val,0,maxi);
		}

		public float GetFloat(float min, float max, float step)
		{
			int maxi = (int) ((max - min) / step);
			int val = GetInt(0, maxi);
			return val * step + min;
		}

		private void Flush()
		{
			_length = 0;
			_seek = 0;
			_bits = 0;
			_currentBitCount = 0;

		}

		public bool GetBit()
		{
			return GetBits(1) == 1;

		}

		public long GetBits(int bitcount)
		{
			long mask = 0;
			for (int i = 0; i < bitcount; i++)
			{
				mask <<= 1;
				mask++;
			}
			Debug.Log("Mask: "+ Convert.ToString(mask,2));
			GetByte(bitcount);
			long ret = _bits & mask;
			_currentBitCount -= bitcount;
			_bits >>= bitcount;
			return ret;

		}

		private void GetByte(int bitcount)
		{
			while (_currentBitCount < bitcount)
			{
				//_bits <<= 8;
				// Esta línea tira warning. Falta algún cast para que no lo tire más.
				// El cast a (uint), no estaba. Está bien así?
				_bits |= (uint) _buffer[_seek] << _currentBitCount;
				
				Debug.Log("Temp bits: " + Convert.ToString(_bits, 2));
				Debug.Log("Buffer is: " + Convert.ToString(_buffer[_seek], 2));
				_seek++;
				_currentBitCount += 8;
			}
			Debug.Log("Bits is: " + Convert.ToString(_bits, 2));

		}
		public BitBuffer()
		{
			_buffer = new byte[512];
		}

		public BitBuffer(byte[] payload)
		{
			_buffer = new byte[512];
			for (int i=0;i<payload.Length;i++)
			{
				_buffer[i] = payload[i];
				_length++;
			}
		}

		public byte[] GetPayload()
		{
			byte[] ret = new byte[_length+1];
			for (int i = 0; i < _length; i++)
			{
				
				ret[i] = _buffer[i];
			}
			
			ret[_length] = (byte)_bits;
			Debug.Log("Bits: "+Convert.ToString(_bits,2));
			Flush();
			return ret;
		}
	}
