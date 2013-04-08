/*----------------------------------------------------------------------+
 |  filename:   CRC-64.cs                                               |
 |----------------------------------------------------------------------|
 |  version:    2.22                                                    |
 |  revision:   02.04.2013 17:00                                        |
 |  authors:    Дробанов Артём Федорович (DrAF),                        |
 |              RUSpectrum (г. Оренбург).                               |
 |  e-mail:     draf@mail.ru                                            |
 |  purpose:    Контроль целостности данных с использованием CRC-64     |
 +----------------------------------------------------------------------*/

using System;

namespace RecoveryStar
{
	/// <summary>
	/// Класс расчета CRC-64
	/// </summary>
	public class CRC64
	{
		#region Constants

		/// <summary>
		/// Генераторный полином для CRC64
		/// </summary>
		private const UInt64 crc64GenPoly = 0xC96C5795D7870F42;

		/// <summary>
		/// Размер таблицы для расчета CRC64
		/// </summary>
		private const UInt64 crc64TableSize = 256;

		/// <summary>
		/// Начальное значение CRC64
		/// </summary>
		private const UInt64 crc64Init = 0xFFFFFFFFFFFFFFFF;

		#endregion Constants

		#region Public Properties & Data

		/// <summary>
		/// Таблица для расчета CRC64
		/// </summary>
		private readonly UInt64[] crc64Table = new UInt64[crc64TableSize];

		/// <summary>
		/// Значение CRC64
		/// </summary>
		private UInt64 crc64value = crc64Init;

		/// <summary>
		/// Значение CRC64
		/// </summary>
		public UInt64 Value
		{
			get { return this.crc64value; }
		}

		/// <summary>
		/// Количество рассчитанных байт
		/// </summary>
		private Int64 length = 0;

		/// <summary>
		/// Количество рассчитанных байт
		/// </summary>
		public Int64 Length
		{
			get { return this.length; }
		}

		#endregion Public Properties & Data

		#region Construction

		/// <summary>
		/// Конструктор класса
		/// </summary>
		public CRC64()
		{
			// Заполнение таблицы CRC64
			for(UInt64 i = 0; i < crc64TableSize; i++)
			{
				UInt64 c = i;

				for(int j = 0; j < 8; j++)
				{
					c = ((c & 0x0000000000000001) == 0) ? (c >> 1) : (c >> 1) ^ crc64GenPoly;
				}

				// Пишем рассчитанное значение в массив
				this.crc64Table[i] = c;
			}
		}

		#endregion Construction

		#region Public Operations

		/// <summary>
		/// Вычислить CRC64
		/// </summary>
		/// <param name="buffer">Массив исходных данных</param>
		/// <param name="length">Длина участка для вычисления CRC64</param>
		public void Calculate(byte[] buffer, int length)
		{
			UInt64 c = this.crc64value; // !

			for(int i = 0; i < length; i++)
			{
				c = (c >> 8) ^ this.crc64Table[(0x00000000000000FF & c) ^ buffer[i]];
			}

			this.crc64value = c;
			this.length += length;
		}

		/// <summary>
		/// Вычислить CRC64
		/// </summary>
		/// <param name="buffer">Массив исходных данных</param>
		/// <param name="offset">Смещение в массиве исходных данных</param>
		/// <param name="length">Длина участка для вычисления CRC64</param>
		public void Calculate(byte[] buffer, int offset, int length)
		{
			UInt64 c = this.crc64value; // !

			for(int i = offset; i < (offset + length); i++)
			{
				c = (c >> 8) ^ this.crc64Table[(0x00000000000000FF & c) ^ buffer[i]];
			}

			this.crc64value = c;
			this.length += length;
		}

		/// <summary>
		/// Сброс в начальное значение
		/// </summary>
		public void Reset()
		{
			this.crc64value = crc64Init;
			this.length = 0;
		}

		/// <summary>
		/// Считать значение CRC64 (UInt64 - ulong) как байтовый массив (8 байт)
		/// </summary>
		/// <returns>В массиве обратная форма записи - от младшего разряда (0) к старшему (7)</returns>
		public byte[] GetCRC64Bytes()
		{
			return DataConverter.GetBytes(this.crc64value);
		}

		#endregion Public Operations
	}
}