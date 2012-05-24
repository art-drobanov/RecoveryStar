/*----------------------------------------------------------------------+
 |  filename:   DataConverter.cs                                        |
 |----------------------------------------------------------------------|
 |  version:    2.20                                                    |
 |  revision:   23.05.2012 17:33                                        |
 |  authors:    Дробанов Артём Федорович (DrAF),                        |
 |              RUSpectrum (г. Оренбург).                               |
 |  e-mail:     draf@mail.ru                                            |
 |  purpose:    Конвертер данных                                        |
 +----------------------------------------------------------------------*/

using System;

namespace RecoveryStar
{
	/// <summary>
	/// Конвертер различных типов данных
	/// </summary>
	public static class DataConverter
	{
		/// <summary>
		/// Преобразовать UInt64 (ulong) в массив из 8 байт
		/// </summary>
		/// <returns>В массиве обратная форма записи - от младшего разряда (0) к старшему (7)</returns>
		public static byte[] GetBytes(UInt64 value)
		{
			byte[] bytes = new byte[8];

			for(int i = 0; i < 8; i++)
			{
				bytes[i] = (byte)(0x00000000000000FF & (value >> (i << 3)));
			}

			return bytes;
		}

		/// <summary>
		/// Преобразовать Int64 (long) в массив из 8 байт
		/// </summary>
		/// <returns>В массиве обратная форма записи - от младшего разряда (0) к старшему (7)</returns>
		public static byte[] GetBytes(Int64 value)
		{
			byte[] bytes = new byte[8];

			for(int i = 0; i < 8; i++)
			{
				bytes[i] = (byte)(0x00000000000000FF & (value >> (i << 3)));
			}

			return bytes;
		}

		/// <summary>
		/// Преобразовать массив из 8 байт в UInt64 (ulong)
		/// </summary>
		/// <param name="bytes">В массиве обратная форма записи - от младшего разряда (0) к старшему (7)</param>
		public static UInt64 GetUInt64(byte[] bytes)
		{
			UInt64 value = 0x0000000000000000;

			for(int i = 0; i < 8; i++)
			{
				value |= ((UInt64)bytes[i]) << (i << 3);
			}

			return value;
		}

		/// <summary>
		/// Преобразовать массив из 8 байт в Int64 (long)
		/// </summary>
		/// <param name="bytes">В массиве обратная форма записи - от младшего разряда (0) к старшему (7)</param>
		public static Int64 GetInt64(byte[] bytes)
		{
			Int64 value = 0x0000000000000000;

			for(int i = 0; i < 8; i++)
			{
				value |= ((Int64)bytes[i]) << (i << 3);
			}

			return value;
		}
	}
}