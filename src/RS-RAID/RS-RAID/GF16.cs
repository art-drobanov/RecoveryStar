/*----------------------------------------------------------------------+
 |  filename:   GF16.cs                                                 |
 |----------------------------------------------------------------------|
 |  version:    2.21                                                    |
 |  revision:   24.08.2012 15:52                                        |
 |  authors:    Дробанов Артём Федорович (DrAF),                        |
 |              RUSpectrum (г. Оренбург).                               |
 |  e-mail:     draf@mail.ru                                            |
 |  purpose:    Класс арифметики поля Галуа (16 bit)                    |
 +----------------------------------------------------------------------*/

using System;

namespace RecoveryStar
{
	/// <summary>
	/// Класс поля Галуа
	/// </summary>
	public class GF16
	{
		#region Constants

		/// <summary>
		/// Несократимый порождающий полином GF(2^16)
		/// </summary>
		private const int RSPrimPoly = 0x1100B;

		/// <summary>
		/// Степень поля Галуа
		/// </summary>
		private const int GFPower = 16;

		/// <summary>
		/// Размер поля Галуа
		/// </summary>
		private const int GFSize = ((1 << GFPower) - 1);

		#endregion Constants

		#region Public Properties & Data

		/// <summary>
		/// Таблица дискретного логарифмирования GF(2^16)
		/// </summary>
		public int[] GFLogTable
		{
			get { return this.GFLog; }
		}

		/// <summary>
		/// Таблица дискретного потенцирования GF(2^16)
		/// </summary>
		public int[] GFExpTable
		{
			get { return this.GFExp; }
		}

		#endregion Public Properties & Data

		#region Data

		/// <summary>
		/// Таблица "логарифмирования"
		/// </summary>
		private int[] GFLog;

		/// <summary>
		/// Таблица "потенцирования"
		/// </summary>
		private int[] GFExp;

		#endregion Data

		#region Construction & Destruction

		public GF16()
		{
			// Инициализируем таблицы "логарифмирования" и "потенцирования"
			GFInit();
		}

		#endregion Construction & Destruction

		#region Public Operations

		/// <summary>
		/// Сложение элементов поля Галуа
		/// </summary>
		public int Add(int a, int b)
		{
			return a ^ b;
		}

		/// <summary>
		/// Вычитание элементов поля Галуа
		/// </summary>
		public int Sub(int a, int b)
		{
			return a ^ b;
		}

		/// <summary>
		/// Оптимизированное умножение элементов поля Галуа (без проверок аргументов на ноль)
		/// </summary>
		public int Mul(int a, int b)
		{
			return this.GFExp[this.GFLog[a] + this.GFLog[b]];
		}

		/// <summary>
		/// Оптимизированное деление элементов поля Галуа (без проверки первого аргумента на ноль)
		/// </summary>
		public int Div(int a, int b)
		{
			// На ноль делить нельзя!
			if(b == 0)
			{
				return -1;
			}

			// Выражение "+this.GFSize" гарантирует неотрицательное значение индекса
			return this.GFExp[this.GFLog[a] - this.GFLog[b] + GFSize];
		}

		/// <summary>
		/// Возведение в степень элемента поля Галуа
		/// </summary>
		public int Pow(int a, int p)
		{
			// Если показатель степени равен "0", то результат - "1"
			if(p == 0)
			{
				return 1;
			}

			// Если основание степени равно "0", то результат - "0"
			if(a == 0)
			{
				return 0;
			}

			// Степень числа может быть представлена как произведение
			// логарифма основания и показателя степени (с последующим потенцированием)
			int pow = this.GFLog[a] * p;

			// Приводим результат к размерам поля (старшие байты складываем с младшими)
			// и возвращаем значение экспоненты
			return this.GFExp[((pow >> GFPower) & GFSize) + (pow & GFSize)];
		}

		/// <summary>
		/// Вычисление обратного элемента поля Галуа
		/// </summary>
		public int Inv(int a)
		{
			// На ноль делить нельзя!
			if(a == 0)
			{
				return -1;
			}

			return this.GFExp[GFSize - this.GFLog[a]];
		}

		/// <summary>
		/// Вычисление логарифма элемента поля Галуа
		/// </summary>
		public int Log(int a)
		{
			return this.GFLog[a];
		}

		/// <summary>
		/// Вычисление экспоненты элемента поля Галуа
		/// </summary>
		public int Exp(int a)
		{
			return this.GFExp[a];
		}

		#endregion Public Operations

		#region Private Operations

		/// <summary>
		/// Инициализация таблиц "логарифмирования" и "потенцирования"
		/// </summary>
		private void GFInit()
		{
			// Таблица "логарифмирования"
			this.GFLog = new int[GFSize + 1];

			// Таблица "потенцирования"
			this.GFExp = new int[(4 * GFSize) + 1];

			// Логарифм нуля сделан таким, чтобы выносить результат за пределы
			// рабочей области таблицы экспонент, туда, где расположены нули ((0 * 0) = 0)
			this.GFLog[0] = (2 * GFSize);

			// Заполняем таблицы логарифмирования и потенцирования
			for(int log = 0, b = 1; log < GFSize; ++log)
			{
				this.GFLog[b] = log;
				this.GFExp[log] = b;
				this.GFExp[log + GFSize] = b; // Дополнительная часть таблицы позволяет
				// избежать приведения к размеру поля после
				// суммирования результатов логарифмирования

				// Удваиваем значение элемента поля, для которого строятся таблицы
				b <<= 1;

				// Если вышли за размеры поля GF(2^16), приводим значение к нему
				if(b > GFSize)
				{
					b ^= RSPrimPoly;
				}
			}

			// Заполняем вторую часть таблицы нулями (нужно для оптимизации
			// умножения, логарифм нуля сделан таким, чтобы выносить результат за пределы
			// рабочей области таблицы экспонент, туда, где расположены нули ((0 * 0) = 0)
			for(int i = (2 * GFSize); i < ((4 * GFSize) + 1); i++)
			{
				this.GFExp[i] = 0;
			}
		}

		#endregion Private Operations
	}
}