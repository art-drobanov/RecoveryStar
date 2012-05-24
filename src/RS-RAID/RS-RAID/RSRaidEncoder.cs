/*----------------------------------------------------------------------+
 |  filename:   RSRaidEncoder.cs                                        |
 |----------------------------------------------------------------------|
 |  version:    2.20                                                    |
 |  revision:   23.05.2012 17:33                                        |
 |  authors:    Дробанов Артём Федорович (DrAF),                        |
 |              RUSpectrum (г. Оренбург).                               |
 |  e-mail:     draf@mail.ru                                            |
 |  purpose:    RAID-подобный кодер Рида-Соломона (16 bit, Коши)        |
 +----------------------------------------------------------------------*/

using System;
using System.Threading;

namespace RecoveryStar
{
	/// <summary>
	/// Класс RAID-подобного кодера Рида-Соломона
	/// </summary>
	public class RSRaidEncoder : RSRaidBase
	{
		#region Construction & Destruction

		/// <summary>
		/// Конструктор кодера по-умолчанию
		/// </summary>
		public RSRaidEncoder()
		{
			// Создаем объект класса работы с элементами поля Галуа
			this.eGF16 = new GF16();
		}

		/// <summary>
		/// Базовый конструктор кодера
		/// </summary>
		/// <param name="dataCount">Количество основных томов</param>
		/// <param name="eccCount">Количество томов для восстановления</param>
		public RSRaidEncoder(int dataCount, int eccCount)
		{
			// Установка конфигурации кодера
			SetConfig(dataCount, eccCount, (int)RSType.Cauchy);

			// Создаем объект класса работы с элементами поля Галуа
			this.eGF16 = new GF16();
		}

		/// <summary>
		/// Расширенный конструктор кодера
		/// </summary>
		/// <param name="dataCount">Количество основных томов</param>
		/// <param name="eccCount">Количество томов для восстановления</param>
		/// <param name="codecType">Тип кодека Рида-Соломона (по типу матрицы)</param>
		public RSRaidEncoder(int dataCount, int eccCount, int codecType)
		{
			// Установка конфигурации кодера
			SetConfig(dataCount, eccCount, codecType);

			// Создаем объект класса работы с элементами поля Галуа
			this.eGF16 = new GF16();
		}

		#endregion Construction & Destruction

		#region Public Operations

		/// <summary>
		/// Установка конфигурации кодера
		/// </summary>
		/// <param name="dataCount">Количество основных томов</param>
		/// <param name="eccCount">Количество томов для восстановления</param>
		/// <param name="codecType">Тип кодека Рида-Соломона (по типу матрицы)</param>
		/// <returns>Булевский флаг операции установки конфигурации</returns>
		public bool SetConfig(int dataCount, int eccCount, int codecType)
		{
			int maxVolCount;

			// Устанавливаем константы, соответствующие выбранному режиму
			if(
				(codecType == (int)RSType.Dispersal)
				||
				(codecType == (int)RSType.Cauchy)
				)
			{
				maxVolCount = (int)RSConst.MaxVolCount;
			}
			else
			{
				maxVolCount = (int)RSConst.MaxVolCountAlt;
			}

			// Проверяем конфигурацию на корректность
			if(
				(dataCount > 0)
				&&
				(eccCount > 0)
				&&
				((dataCount + eccCount) <= maxVolCount)
				)
			{
				// Если основная конфигурация изменилась - сообщаем об этом
				if(
					(dataCount != this.n)
					||
					(eccCount != this.m)
					||
					(codecType != this.eRSType)
					)
				{
					this.mainConfigChanged = true;
				}

				// Сохраняем конфигурацию
				this.n = dataCount;
				this.m = eccCount;
				this.eRSType = codecType;

				// Также пересчитываем количество итераций всех стадий подготовки
				double n = this.n;
				double m = this.m;

				// Нормализуем значения для расчета, чтобы избежать переполнения переменных
				NormalizeNM(ref n, ref m);

				// Количество итераций на первой стадии зависит от типа используемой матрицы
				if(
					(this.eRSType == (int)RSType.Alternative)
					||
					(this.eRSType == (int)RSType.Cauchy)
					)
				{
					this.iterOfFirstStage = m;
				}
				else
				{
					this.iterOfFirstStage = ((n * m) * n) + (n * ((n + m) + (n * (n + m))));
				}

				this.iterOfSecondStage = 0; // В кодере нет инвертирования матрицы

				this.configIsOK = true;
			}
			else
			{
				//...указываем на ошибку конфигурации
				this.configIsOK = false;
			}

			return this.configIsOK;
		}

		/// <summary>
		/// Метод умножения матрицы кодирования на входной прологарифмированный вектор
		/// </summary>
		/// <param name="dataLog">Прологарифмированный входной вектор (исходные данные)</param>
		/// <param name="ecc">Выходной вектор (избыточные данные)</param>
		/// <returns>Булевский флаг результата операции</returns>
		public bool Process(int[] dataLog, int[] ecc)
		{
			// Если кодер сконфигурирован некорректно, обработка невозможна!
			if(!this.configIsOK)
			{
				return false;
			}

			// Копируем указатель на массив экспонент для сокращения времени обращения
			int[] GF16Exp = this.eGF16.GFExpTable;

			// Используем параллелизм только в том случае, когда это целесообразно
			if((this.m + this.n) >= (int)RSParallelEdge.Value)
			{
				// Вычисление результата умножения матрицы на вектор
				AForge.Parallel.For(0, this.m, delegate(int i)
				                               	{
				                               		int mulSum = 0; // Сумма произведения строки матрицы на столбец
				                               		int i_n = i * this.n; // Смещение в массиве до элементов i-ой строки

				                               		for(int j = 0; j < this.n; j++)
				                               		{
				                               			mulSum ^= GF16Exp[this.FLog[i_n + j] + dataLog[j]];
				                               		}

				                               		ecc[i] = mulSum;
				                               	});
			}
			else
			{
				// Вычисление результата умножения матрицы на вектор
				for(int i = 0; i < this.m; i++)
				{
					int mulSum = 0; // Сумма произведения строки матрицы на столбец
					int i_n = i * this.n; // Смещение в массиве до элементов i-ой строки

					for(int j = 0; j < this.n; j++)
					{
						mulSum ^= GF16Exp[this.FLog[i_n + j] + dataLog[j]];
					}

					ecc[i] = mulSum;
				}
			}

			return true;
		}

		#endregion Public Operations

		#region Private Operations

		/// <summary>
		/// Заполнение матрицы кодирования данными
		/// </summary>
		protected override void FillFLog()
		{
			// Если основная конфигурация изменилась...
			if(this.mainConfigChanged)
			{
				// Выделяем память под матрицу "FLog"
				this.FLog = new int[this.m * this.n];

				switch(this.eRSType)
				{
					case (int)RSType.Dispersal:
						{
							//...производим формирование дисперсной матрицы "D"
							if(!MakeDispersalMatrix())
							{
								// Указываем, что кодер не сконфигурирован корректно
								this.configIsOK = false;

								// Активируем индикатор актуального состояния переменных-членов
								this.finished = true;

								// Устанавливаем событие завершения обработки
								this.finishedEvent[0].Set();

								return;
							}
							else
							{
								// Заполняем матрицу кодирования
								for(int i = 0; i < this.m; i++)
								{
									// Смещение в массиве до элементов i-ой строки
									int i_n = i * this.n;

									// Формирование строки в матрице кодирования
									for(int j = 0; j < this.n; j++)
									{
										// В матрицу кодирования помещаем логарифмы её исходных элементов
										// (для ускорения умножения матрицы на вектор)
										this.FLog[i_n + j] = this.eGF16.Log(this.D[((this.n + i) * this.n) + j]);
									}

									// В случае, если требуется постановка на паузу, событие "executeEvent"
									// будет сброшено, и будем на паузе вплоть до его появления
									ManualResetEvent.WaitAll(this.executeEvent);

									// Если указано, что требуется выйти из потока - выходим
									if(ManualResetEvent.WaitAll(this.exitEvent, 0, false))
									{
										// Указываем, что кодер не сконфигурирован корректно
										this.configIsOK = false;

										// Активируем индикатор актуального состояния переменных-членов
										this.finished = true;

										// Устанавливаем событие завершения обработки
										this.finishedEvent[0].Set();

										return;
									}
								}
							}

							break;
						}

					case (int)RSType.Alternative:
						{
							//...производим формирование альтернативного заполнения матрицы "A"
							if(!MakeAlternativeMatrix())
							{
								// Указываем, что кодер не сконфигурирован корректно
								this.configIsOK = false;

								// Активируем индикатор актуального состояния переменных-членов
								this.finished = true;

								// Устанавливаем событие завершения обработки
								this.finishedEvent[0].Set();

								return;
							}
							else
							{
								// Заполняем матрицу кодирования
								for(int i = 0; i < this.m; i++)
								{
									// Смещение в массиве до элементов i-ой строки
									int i_n = i * this.n;

									// Формирование строки в матрице кодирования
									for(int j = 0; j < this.n; j++)
									{
										// Просто оптимизация :)
										int idx = i_n + j;

										// В матрицу кодирования помещаем логарифмы её исходных элементов
										// (для ускорения умножения матрицы на вектор)
										this.FLog[idx] = this.eGF16.Log(this.A[idx]);
									}

									// В случае, если требуется постановка на паузу, событие "executeEvent"
									// будет сброшено, и будем на паузе вплоть до его появления
									ManualResetEvent.WaitAll(this.executeEvent);

									// Если указано, что требуется выйти из потока - выходим
									if(ManualResetEvent.WaitAll(this.exitEvent, 0, false))
									{
										// Указываем, что кодер не сконфигурирован корректно
										this.configIsOK = false;

										// Активируем индикатор актуального состояния переменных-членов
										this.finished = true;

										// Устанавливаем событие завершения обработки
										this.finishedEvent[0].Set();

										return;
									}
								}
							}

							break;
						}

					default:
					case (int)RSType.Cauchy:
						{
							//...производим формирование матрицы Коши "C"
							if(!MakeCauchyMatrix())
							{
								// Указываем, что кодер не сконфигурирован корректно
								this.configIsOK = false;

								// Активируем индикатор актуального состояния переменных-членов
								this.finished = true;

								// Устанавливаем событие завершения обработки
								this.finishedEvent[0].Set();

								return;
							}
							else
							{
								// Заполняем матрицу кодирования
								for(int i = 0; i < this.m; i++)
								{
									// Смещение в массиве до элементов i-ой строки
									int i_n = i * this.n;

									// Формирование строки в матрице кодирования
									for(int j = 0; j < this.n; j++)
									{
										// Просто оптимизация :)
										int idx = i_n + j;

										// В матрицу кодирования помещаем логарифмы её исходных элементов
										// (для ускорения умножения матрицы на вектор)
										this.FLog[idx] = this.eGF16.Log(this.C[idx]);
									}

									// В случае, если требуется постановка на паузу, событие "executeEvent"
									// будет сброшено, и будем на паузе вплоть до его появления
									ManualResetEvent.WaitAll(this.executeEvent);

									// Если указано, что требуется выйти из потока - выходим
									if(ManualResetEvent.WaitAll(this.exitEvent, 0, false))
									{
										// Указываем, что кодер не сконфигурирован корректно
										this.configIsOK = false;

										// Активируем индикатор актуального состояния переменных-членов
										this.finished = true;

										// Устанавливаем событие завершения обработки
										this.finishedEvent[0].Set();

										return;
									}
								}
							}

							break;
						}
				}

				// Если есть подписка на делегата завершения...
				if(OnRSMatrixFormingFinish != null)
				{
					//...сообщаем, что экземпляр класса готов к работе
					OnRSMatrixFormingFinish();
				}

				//...и сбрасываем флаг
				this.mainConfigChanged = false;
			}

			// Если есть подписка на делегата завершения...
			if(OnRSMatrixFormingFinish != null)
			{
				//...сообщаем, что экземпляр класса готов к работе
				OnRSMatrixFormingFinish();
			}

			// Активируем индикатор актуального состояния переменных-членов
			this.finished = true;

			// Устанавливаем событие завершения обработки
			this.finishedEvent[0].Set();
		}

		#endregion Private Operations
	}
}