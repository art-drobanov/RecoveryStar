/*----------------------------------------------------------------------+
 |  filename:   RSRaidDecoder.cs                                        |
 |----------------------------------------------------------------------|
 |  version:    2.22                                                    |
 |  revision:   02.04.2013 17:00                                        |
 |  authors:    Дробанов Артём Федорович (DrAF),                        |
 |              RUSpectrum (г. Оренбург).                               |
 |  e-mail:     draf@mail.ru                                            |
 |  purpose:    RAID-подобный декодер Рида-Соломона (16 bit, Коши)      |
 +----------------------------------------------------------------------*/

using System;
using System.Threading;

namespace RecoveryStar
{
	/// <summary>
	/// Класс RAID-подобного декодера Рида-Соломона
	/// </summary>
	public class RSRaidDecoder : RSRaidBase
	{
		#region Data

		/// <summary>
		/// Массив булевских признаков "строка матрицы "FLog" тривиальна?"
		/// </summary>
		private bool[] FLogRowIsTrivial;

		/// <summary>
		/// Список порядковых номеров имеющихся томов (нумерация с нуля)
		/// </summary>
		private int[] volList;

		#endregion Data

		#region Construction & Destruction

		/// <summary>
		/// Конструктор декодера по-умолчанию
		/// </summary>
		public RSRaidDecoder()
		{
			// Создаем объект класса работы с элементами поля Галуа
			this.eGF16 = new GF16();
		}

		/// <summary>
		/// Базовый конструктор декодера
		/// </summary>
		/// <param name="dataCount">Количество основных томов</param>
		/// <param name="eccCount">Количество томов для восстановления</param>
		/// <param name="volList">Список порядковых номеров имеющихся томов</param>
		public RSRaidDecoder(int dataCount, int eccCount, int[] volList)
		{
			// Установка конфигурации кодера
			SetConfig(dataCount, eccCount, volList, (int)RSType.Cauchy);

			// Создаем объект класса работы с элементами поля Галуа
			this.eGF16 = new GF16();
		}

		/// <summary>
		/// Расширенный конструктор декодера
		/// </summary>
		/// <param name="dataCount">Количество основных томов</param>
		/// <param name="eccCount">Количество томов для восстановления</param>
		/// <param name="volList">Список порядковых номеров имеющихся томов</param>
		/// <param name="codecType">Тип кодека Рида-Соломона (по типу матрицы)</param>
		public RSRaidDecoder(int dataCount, int eccCount, int[] volList, int codecType)
		{
			// Установка конфигурации кодера
			SetConfig(dataCount, eccCount, volList, codecType);

			// Создаем объект класса работы с элементами поля Галуа
			this.eGF16 = new GF16();
		}

		#endregion Construction & Destruction

		#region Public Operations

		/// <summary>
		/// Установка конфигурации декодера
		/// </summary>
		/// <param name="dataCount">Количество основных томов</param>
		/// <param name="eccCount">Количество томов для восстановления</param>
		/// <param name="volList">Список порядковых номеров имеющихся томов</param>
		/// <param name="codecType">Тип кодека Рида-Соломона (по типу матрицы)</param>
		/// <returns>Булевский флаг операции установки конфигурации</returns>
		public bool SetConfig(int dataCount, int eccCount, int[] volList, int codecType)
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
				&&
				(volList.Length >= dataCount)
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

				// Количество отслеживаемых прогрессом итераций на первой стадии
				// зависит от типа используемой матрицы
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

				this.iterOfSecondStage = (n * (((n - 1) * (n - 1)) + (n * n)));

				// Выделяем память под массив булевских признаков "строка матрицы "FLog" тривиальна?"
				this.FLogRowIsTrivial = new bool[dataCount];

				// Сохраняем список имеющихся томов
				this.volList = volList;

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
		/// <param name="dataEccLog">Прологарифмированный входной вектор (данные + ecc)</param>
		/// <param name="data">Выходной вектор (восстановленные исходные данные)</param>
		/// <returns>Булевский флаг результата операции</returns>
		public bool Process(int[] dataEccLog, int[] data)
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
				AForge.Parallel.For(0, this.n, delegate(int i)
				                               	{
				                               		// Если текущая строка матрицы не является тривиальной, производим обработку
				                               		if(!this.FLogRowIsTrivial[i])
				                               		{
				                               			int mulSum = 0; // Сумма произведения строки матрицы на столбец
				                               			int i_n = i * this.n; // Смещение в массиве до элементов i-ой строки

				                               			for(int j = 0; j < this.n; j++)
				                               			{
				                               				mulSum ^= GF16Exp[this.FLog[i_n + j] + dataEccLog[j]];
				                               			}

				                               			data[i] = mulSum;
				                               		}
				                               		else
				                               		{
				                               			data[i] = GF16Exp[dataEccLog[i]];
				                               		}
				                               	});
			}
			else
			{
				// Вычисление результата умножения матрицы на вектор
				for(int i = 0; i < this.n; i++)
				{
					// Если текущая строка матрицы не является тривиальной, производим обработку
					if(!this.FLogRowIsTrivial[i])
					{
						int mulSum = 0; // Сумма произведения строки матрицы на столбец
						int i_n = i * this.n; // Смещение в массиве до элементов i-ой строки

						for(int j = 0; j < this.n; j++)
						{
							mulSum ^= GF16Exp[this.FLog[i_n + j] + dataEccLog[j]];
						}

						data[i] = mulSum;
					}
					else
					{
						data[i] = GF16Exp[dataEccLog[i]];
					}
				}
			}

			return true;
		}

		#endregion Public Operations

		#region Private Operations

		/// <summary>
		/// Поиск матрицы, обратной к "FLog", методом Жордановых исключений
		/// (Данная модификация метода может использоваться только в тех случаях,
		/// когда (-a) = (a), т.к. за ненадобностью пропущена стадия изменения элементов),
		/// кроме того, отсутствует поиск ненулевого разрешающего элемента (в случае
		/// работы с матрицей кодирования, наличие нуля на диагонали - сбой кодека,
		/// поэтому ситуация с обнаружением нуля воспринимается исключительно как ошибка
		/// </summary>
		/// <returns>Булевский флаг результата операции</returns>
		private bool FInv()
		{
			// Вычисляем распределение процентов итераций по стадиям для
			// корректной обработки процентов
			double allStageIter = this.iterOfFirstStage + this.iterOfSecondStage;
			int percOfFirstStage = (int)((100.0 * this.iterOfFirstStage) / allStageIter);
			int percOfSecondStage = (int)((100.0 * this.iterOfSecondStage) / allStageIter);

			// Данная стадия должна занимать хотя бы один процент
			// (для корректности расчетов)
			if(percOfSecondStage == 0)
			{
				percOfSecondStage = 1;
			}

			// Вычисляем значение модуля, который позволит выводить процент обработки
			// ровно при единичном приращении для цикла по "k"
			int progressMod1 = this.n / percOfSecondStage;

			// Если модуль равен нулю, то увеличиваем его до значения "1", чтобы
			// прогресс выводился на каждой итерации
			if(progressMod1 == 0)
			{
				progressMod1 = 1;
			}

			// Цикл выбора разрешающего элемента "pivot"
			for(int k = 0; k < this.n; k++)
			{
				// Если данная строка тривиальная - просто переходим на новую итерацию
				if(this.FLogRowIsTrivial[k])
				{
					continue;
				}

				// Смещение в массиве до элементов k-ой строки
				int k_n = k * this.n;

				// Индекс разрешающего элемента
				int pivotIdx = k_n + k;

				// Извлекаем разрешающий элемент...
				int pivot = this.FLog[pivotIdx];

				// Если разрешающий элемент равен нулю - матрица не имеет обратной!
				if(pivot == 0)
				{
					//...указываем на ошибку конфигурации
					this.configIsOK = false;

					// Активируем индикатор актуального состояния переменных-членов
					this.finished = true;

					// Устанавливаем событие завершения обработки
					this.finishedEvent[0].Set();

					return false;
				}

				// После извлечения разрешающего элемента помещаем на его место "1"
				this.FLog[pivotIdx] = 1;

				// Работаем со строками до разрешающей...
				for(int i = 0; i < k; i++)
				{
					// Смещение в массиве до элементов i-ой строки
					int i_n = i * this.n;

					// Сохраняем позицию [i,k]...
					int FLog_i_k = this.FLog[i_n + k];

					// Работаем со столбцами...
					for(int j = 0; j < this.n; j++)
					{
						// Оптимизация :)
						int fIdx = i_n + j;

						// Производим требуемые действия над матрицей: "A[i,j] = A[i,j] * pivot + A[i,k] * A[k,j]"
						this.FLog[fIdx] = this.eGF16.Mul(this.FLog[fIdx], pivot) ^ this.eGF16.Mul(FLog_i_k, this.FLog[k_n + j]);
					}

					// Восстанавливаем позицию [i,k]...
					this.FLog[i_n + k] = FLog_i_k;
				}

				// Работаем со строками после разрешающей...
				for(int i = (k + 1); i < this.n; i++)
				{
					// Смещение в массиве до элементов i-ой строки
					int i_n = i * this.n;

					// Сохраняем позицию [i,k]...
					int FLog_i_k = this.FLog[i_n + k];

					// Работаем со столбцами...
					for(int j = 0; j < this.n; j++)
					{
						// Оптимизация :)
						int fIdx = i_n + j;

						// Производим требуемые действия над матрицей: "A[i,j] = A[i,j] * pivot + A[i,k] * A[k,j]"
						this.FLog[fIdx] = this.eGF16.Mul(this.FLog[fIdx], pivot) ^ this.eGF16.Mul(FLog_i_k, this.FLog[k_n + j]);
					}

					// Восстанавливаем позицию [i,k]...
					this.FLog[i_n + k] = FLog_i_k;
				}

				// Деление матрицы на разрешающий элемент заменяем её умножением на обратный...
				int pivotInv = this.eGF16.Inv(pivot);

				for(int i = 0; i < (this.n * this.n); i++)
				{
					this.FLog[i] = this.eGF16.Mul(this.FLog[i], pivotInv);
				}

				// Если есть подписка на делегата обновления прогресса -...
				if(
					((k % progressMod1) == 0)
					&&
					(OnUpdateRSMatrixFormingProgress != null)
					)
				{
					//...выводим данные
					OnUpdateRSMatrixFormingProgress((((double)(k + 1) / (double)this.n) * percOfSecondStage) + percOfFirstStage);
				}

				// В случае, если требуется постановка на паузу, событие "executeEvent"
				// будет сброшено, и будем на паузе вплоть до его появления
				ManualResetEvent.WaitAll(this.executeEvent);

				// Если указано, что требуется выйти из потока - выходим
				if(ManualResetEvent.WaitAll(this.exitEvent, 0, false))
				{
					// Указываем, что декодер не сконфигурирован корректно
					this.configIsOK = false;

					// Активируем индикатор актуального состояния переменных-членов
					this.finished = true;

					// Устанавливаем событие завершения обработки
					this.finishedEvent[0].Set();

					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Вычисление логарифмов значений инвертированной матрицы
		/// <summary>
		private void LogFCalc()
		{
			for(int i = 0; i < (this.n * this.n); i++)
			{
				this.FLog[i] = this.eGF16.Log(this.FLog[i]);
			}
		}

		/// <summary>
		/// Заполнение матрицы "FLog" (матрицы декодера) данными
		/// </summary>
		protected override void FillFLog()
		{
			// Если длина вектора имеющихся томов меньше количества,
			// требуемого для восстановления...
			if(this.volList.Length < this.n)
			{
				//...указываем на ошибку конфигурации
				this.configIsOK = false;

				// Активируем индикатор актуального состояния переменных-членов
				this.finished = true;

				// Устанавливаем событие завершения обработки
				this.finishedEvent[0].Set();

				return;
			}

			// Выделяем память под матрицу "FLog"
			this.FLog = new int[this.n * this.n];

			// Вектор счетчиков всех томов...
			int[] allVolCount = new int[this.n + this.m];

			//...и вектор ecc-томов для "затыкания" пробелов, созданных
			// утерянными основными томами
			int[] eccVolToFix = new int[this.m];

			// Счетчик количества стертых основных томов
			int dataVolMissCount = this.n;

			// Инициализируем массив счетчиков всех томов
			for(int i = 0; i < (this.n + this.m); i++)
			{
				allVolCount[i] = 0;
			}

			// Проводим анализ состава представленных томов на предмет наличия основных
			for(int i = 0; i < this.n; i++)
			{
				// Вычисляем номер текущего тома
				int currVol = Math.Abs(this.volList[i]);

				// Если номер тома соответствует допустимому диапазону
				if(currVol < (this.n + this.m))
				{
					++allVolCount[currVol];

					// Если текущий том является основным, фиксируем данный факт
					if(currVol < this.n)
					{
						--dataVolMissCount;
					}
				}
				else
				{
					// Указываем на ошибку конфигурации
					this.configIsOK = false;

					// Активируем индикатор актуального состояния переменных-членов
					this.finished = true;

					// Устанавливаем событие завершения обработки
					this.finishedEvent[0].Set();

					return;
				}
			}

			// Проверяем счетчики томов на ошибочное дублирование
			for(int i = 0; i < (this.n + this.m); i++)
			{
				// Если некоторый том был указан более чем один раз...
				if(allVolCount[i] > 1)
				{
					//...указываем на ошибку конфигурации
					this.configIsOK = false;

					// Активируем индикатор актуального состояния переменных-членов
					this.finished = true;

					// Устанавливаем событие завершения обработки
					this.finishedEvent[0].Set();

					return;
				}
			}

			// Если проверка на непротиворечивость не выявила проблем, начинаем
			// формировать матрицу "FLog"

			// Если основная конфигурация изменилась...
			if(this.mainConfigChanged)
			{
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

							break;
						}

					default:
					case (int)RSType.Cauchy:
						{
							//...производим формирование заполнения матрицы Коши "С"
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

							break;
						}
				}

				//...и сбрасываем флаг
				this.mainConfigChanged = false;
			}

			// Для каждого утерянного основного тома ищем том для восстановления
			for(int i = 0, j = 0; i < dataVolMissCount; i++)
			{
				// Движемся по списку томов до тех пор, пока не найдем том для
				// восстановления для затыкания "дырки" (основные тома имеют номера
				// меньше this.n (при нумерации с нуля!))
				while(this.volList[j] < this.n)
				{
					j++;
				}

				// Сохраняем номер тома для замены утерянного основного тома
				eccVolToFix[i] = this.volList[j];

				j++; // j++ позволяет перейти к последующему поиску
			}

			switch(this.eRSType)
			{
				case (int)RSType.Dispersal:
					{
						// Работаем по строкам матрицы (в идеале, все строки должны заполняться
						// строками с единицей на главной диагонали, что соответствует отсутствию
						// повреждений, но allVolCount укажет, как обстоят дела с наличием томов)
						for(int i = 0, e = 0; i < this.n; i++)
						{
							// Индекс строки из дисперсной матрицы, которая будет помещена в матрицу кодирования
							int DRowIdx;

							// Смещение в массиве до элементов i-ой строки
							int i_n = i * this.n;

							// Если основной том отсутствует, формируем строку матрицы Вандермонда
							if(allVolCount[i] == 0)
							{
								// Вычисляем номер строки матрицы Вандермонда, которую нужно вставить
								// на место данной строки формируемой матрицы "FLog"
								DRowIdx = eccVolToFix[e++];

								// Указываем, что данная строка матрицы "FLog" не тривиальна
								this.FLogRowIsTrivial[i] = false;
							}
							else
							{
								// Формируем в матрице "FLog" нулевую строку с единицей на главной диагонали
								// (соответствует имеющемуся основному тому)
								DRowIdx = i;

								// Указываем, что данная строка матрицы "FLog" тривиальна
								this.FLogRowIsTrivial[i] = true;
							}

							// Оптимизация :)
							int bs = DRowIdx * this.n;

							// Формирование строки в матрице кодирования
							// ("тривиальные" строки уже содержатся в матрице "D", они получились
							// "автоматически" на предыдущем этапе обработки MakeDispersal())
							for(int j = 0; j < this.n; j++)
							{
								this.FLog[i_n + j] = this.D[bs + j];
							}

							// В случае, если требуется постановка на паузу, событие "executeEvent"
							// будет сброшено, и будем на паузе вплоть до его появления
							ManualResetEvent.WaitAll(this.executeEvent);

							// Если указано, что требуется выйти из потока - выходим
							if(ManualResetEvent.WaitAll(this.exitEvent, 0, false))
							{
								//...указываем на ошибку конфигурации
								this.configIsOK = false;

								// Активируем индикатор актуального состояния переменных-членов
								this.finished = true;

								// Устанавливаем событие завершения обработки
								this.finishedEvent[0].Set();

								return;
							}
						}

						break;
					}

				case (int)RSType.Alternative:
					{
						// Работаем по строкам матрицы (в идеале, все строки должны заполняться
						// строками с единицей на главной диагонали, что соответствует отсутствию
						// повреждений, но allVolCount укажет, как обстоят дела с наличием томов)
						for(int i = 0, e = 0; i < this.n; i++)
						{
							// Индекс строки из альтернативной матрицы, которая будет помещена в матрицу кодирования
							int ARowIdx;

							// Смещение в массиве до элементов i-ой строки
							int i_n = i * this.n;

							// Если основной том отсутствует, формируем строку матрицы Вандермонда
							if(allVolCount[i] == 0)
							{
								// Вычисляем номер строки альтернативной матрицы, которую нужно вставить
								// на место данной строки формируемой матрицы "FLog"
								ARowIdx = eccVolToFix[e++] - this.n;

								// Указываем, что данная строка матрицы "FLog" не тривиальна
								this.FLogRowIsTrivial[i] = false;
							}
							else
							{
								// Формируем в матрице "FLog" нулевую строку с единицей на главной диагонали
								// (соответствует имеющемуся основному тому)
								ARowIdx = i;

								// Указываем, что данная строка матрицы "FLog" тривиальна
								this.FLogRowIsTrivial[i] = true;
							}

							// Если это требуется - формируем "тривиальную" строку...
							if(this.FLogRowIsTrivial[i])
							{
								for(int j = 0; j < this.n; j++)
								{
									this.FLog[i_n + j] = 0;
								}

								this.FLog[i_n + i] = 1;
							}
							else
							{
								// Оптимизация :)
								int bs = ARowIdx * this.n;

								//...а, иначе, берем строку матрицы Вандермонда
								for(int j = 0; j < this.n; j++)
								{
									this.FLog[i_n + j] = this.A[bs + j];
								}
							}

							// В случае, если требуется постановка на паузу, событие "executeEvent"
							// будет сброшено, и будем на паузе вплоть до его появления
							ManualResetEvent.WaitAll(this.executeEvent);

							// Если указано, что требуется выйти из потока - выходим
							if(ManualResetEvent.WaitAll(this.exitEvent, 0, false))
							{
								//...указываем на ошибку конфигурации
								this.configIsOK = false;

								// Активируем индикатор актуального состояния переменных-членов
								this.finished = true;

								// Устанавливаем событие завершения обработки
								this.finishedEvent[0].Set();

								return;
							}
						}

						break;
					}

				case (int)RSType.Cauchy:
					{
						// Работаем по строкам матрицы (в идеале, все строки должны заполняться
						// строками с единицей на главной диагонали, что соответствует отсутствию
						// повреждений, но allVolCount укажет, как обстоят дела с наличием томов)
						for(int i = 0, e = 0; i < this.n; i++)
						{
							// Индекс строки из матрицы Коши, которая будет помещена в матрицу кодирования
							int CRowIdx;

							// Смещение в массиве до элементов i-ой строки
							int i_n = i * this.n;

							// Если основной том отсутствует, формируем строку матрицы Вандермонда
							if(allVolCount[i] == 0)
							{
								// Вычисляем номер строки матрицы Коши, которую нужно вставить
								// на место данной строки формируемой матрицы "FLog"
								CRowIdx = eccVolToFix[e++] - this.n;

								// Указываем, что данная строка матрицы "FLog" не тривиальна
								this.FLogRowIsTrivial[i] = false;
							}
							else
							{
								// Формируем в матрице "FLog" нулевую строку с единицей на главной диагонали
								// (соответствует имеющемуся основному тому)
								CRowIdx = i;

								// Указываем, что данная строка матрицы "FLog" тривиальна
								this.FLogRowIsTrivial[i] = true;
							}

							// Если это требуется - формируем "тривиальную" строку...
							if(this.FLogRowIsTrivial[i])
							{
								for(int j = 0; j < this.n; j++)
								{
									this.FLog[i_n + j] = 0;
								}

								this.FLog[i_n + i] = 1;
							}
							else
							{
								// Оптимизация :)
								int bs = CRowIdx * this.n;

								//...а, иначе, берем строку матрицы Вандермонда
								for(int j = 0; j < this.n; j++)
								{
									this.FLog[i_n + j] = this.C[bs + j];
								}
							}

							// В случае, если требуется постановка на паузу, событие "executeEvent"
							// будет сброшено, и будем на паузе вплоть до его появления
							ManualResetEvent.WaitAll(this.executeEvent);

							// Если указано, что требуется выйти из потока - выходим
							if(ManualResetEvent.WaitAll(this.exitEvent, 0, false))
							{
								//...указываем на ошибку конфигурации
								this.configIsOK = false;

								// Активируем индикатор актуального состояния переменных-членов
								this.finished = true;

								// Устанавливаем событие завершения обработки
								this.finishedEvent[0].Set();

								return;
							}
						}

						break;
					}
			}

			// Находим обратную матрицу для "FLog"
			if(!FInv())
			{
				// Указываем, что кодер не сконфигурирован корректно
				this.configIsOK = false;

				// Активируем индикатор актуального состояния переменных-членов
				this.finished = true;

				// Устанавливаем событие завершения обработки
				this.finishedEvent[0].Set();

				return;
			}

			// Вычисляем логарифмы элементов инвертированной матрицы
			LogFCalc();

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

		#region Public Properties

		/// <summary>
		/// Список порядковых номеров имеющихся томов
		/// </summary>
		public int[] VolList
		{
			get
			{
				if(!InProcessing)
				{
					return this.volList;
				}
				else
				{
					return null;
				}
			}
		}

		#endregion Public Properties
	}
}