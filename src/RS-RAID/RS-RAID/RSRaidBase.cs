/*----------------------------------------------------------------------+
 |  filename:   RSRaidBase.cs                                           |
 |----------------------------------------------------------------------|
 |  version:    2.21                                                    |
 |  revision:   24.08.2012 15:52                                        |
 |  authors:    Дробанов Артём Федорович (DrAF),                        |
 |              RUSpectrum (г. Оренбург).                               |
 |  e-mail:     draf@mail.ru                                            |
 |  purpose:    Базовый класс кодека Рида-Соломона (16 bit, Коши)       |
 +----------------------------------------------------------------------*/

using System;
using System.Threading;

namespace RecoveryStar
{
	/// <summary>
	/// Класс базовой части RAID-подобного кодера Рида-Соломона
	/// </summary>
	public abstract class RSRaidBase
	{
		#region Delegates

		/// <summary>
		/// Делегат обновления процесса формирования матрицы "FLog"
		/// </summary>
		public OnUpdateDoubleValueHandler OnUpdateRSMatrixFormingProgress;

		/// <summary>
		/// Делегат завершения процесса формирования матрицы "FLog"
		/// </summary>
		public OnEventHandler OnRSMatrixFormingFinish;

		#endregion Delegates

		#region Public Properties & Data

		/// <summary>
		/// Экземпляр класса занят обработкой?
		/// </summary>
		public bool InProcessing
		{
			get
			{
				if(
					(this.thrRSMatrixForming != null)
					&&
					(
						(this.thrRSMatrixForming.ThreadState == ThreadState.Running)
						||
						(this.thrRSMatrixForming.ThreadState == ThreadState.WaitSleepJoin)
					)
					)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
		}

		/// <summary>
		/// Экземпляр класса сконфигурирован корректно?
		/// </summary>
		public bool ConfigIsOK
		{
			get
			{
				if(!InProcessing)
				{
					return this.configIsOK;
				}
				else
				{
					return false;
				}
			}
		}

		/// <summary>
		/// Экземпляр класса инициализирован корректно (пригоден к работе)?
		/// </summary>
		protected bool configIsOK;

		/// <summary>
		/// Булевское свойство "Экземпляр класса закончил обработку
		/// (имеет актуальное состояние переменных-членов)?"
		/// </summary>
		public bool Finished
		{
			get
			{
				// Если класс не занят обработкой - возвращаем значение
				if(!InProcessing)
				{
					return this.finished;
				}
				else
				{
					return false;
				}
			}
		}

		/// <summary>
		/// Экземляр класса полностью закончил обработку?
		/// </summary>
		protected bool finished;

		/// <summary>
		/// Количество основных томов
		/// </summary>
		public int DataCount
		{
			get
			{
				if(!InProcessing)
				{
					return this.n;
				}
				else
				{
					return -1;
				}
			}
		}

		/// <summary>
		/// Количество основных томов
		/// </summary>
		protected int n;

		/// <summary>
		/// Количество томов для восстановления
		/// </summary>
		public int EccCount
		{
			get
			{
				if(!InProcessing)
				{
					return this.m;
				}
				else
				{
					return -1;
				}
			}
		}

		/// <summary>
		/// Количество томов для восстановления
		/// </summary>
		protected int m;

		/// <summary>
		/// Тип кодека (по типу используемой матрицы)
		/// </summary>
		public int CodecType
		{
			get
			{
				if(!InProcessing)
				{
					return this.eRSType;
				}
				else
				{
					return -1;
				}
			}
		}

		/// <summary>
		/// Тип кодека Рида-Соломона (по типу используемой матрицы кодирования)
		/// </summary>
		protected int eRSType;

		/// <summary>
		/// Приоритет процесса
		/// </summary>
		public int ThreadPriority
		{
			get { return (int)this.threadPriority; }

			set
			{
				if(
					(this.thrRSMatrixForming != null)
					&&
					(this.thrRSMatrixForming.IsAlive)
					)
				{
					switch(value)
					{
						default:
						case 0:
							{
								this.threadPriority = System.Threading.ThreadPriority.Lowest;

								break;
							}

						case 1:
							{
								this.threadPriority = System.Threading.ThreadPriority.BelowNormal;

								break;
							}

						case 2:
							{
								this.threadPriority = System.Threading.ThreadPriority.Normal;

								break;
							}

						case 3:
							{
								this.threadPriority = System.Threading.ThreadPriority.AboveNormal;

								break;
							}

						case 4:
							{
								this.threadPriority = System.Threading.ThreadPriority.Highest;

								break;
							}
					}

					// Устанавливаем выбранный приоритет процесса
					this.thrRSMatrixForming.Priority = this.threadPriority;
				}
			}
		}

		/// <summary>
		/// Приоритет процесса подготовки матрицы кодирования
		/// </summary>
		protected ThreadPriority threadPriority;

		/// <summary>
		/// Событие, устанавливаемое по завершении обработки
		/// </summary>
		public ManualResetEvent[] FinishedEvent
		{
			get { return this.finishedEvent; }
		}

		/// <summary>
		/// Событие, устанавливаемое по завершении обработки
		/// </summary>
		protected ManualResetEvent[] finishedEvent;

		#endregion Public Properties & Data

		#region Data

		/// <summary>
		/// Объект класса работы с элементами поля Галуа
		/// </summary>
		protected GF16 eGF16;

		/// <summary>
		/// Матрица RAID-подобного кодера Рида-Соломона
		/// </summary>
		protected int[] FLog;

		/// <summary>
		/// Дисперсная матрица
		/// </summary>
		protected int[] D;

		/// <summary>
		/// "Альтернативная" матрица
		/// </summary>
		protected int[] A;

		/// <summary>
		/// Матрица Коши
		/// </summary>
		protected int[] C;

		/// <summary>
		/// Основная конфигурация сменилась?
		/// </summary>
		protected bool mainConfigChanged;

		/// <summary>
		/// Количество итераций первой стадии подготовки матрицы кодирования
		/// </summary>
		protected double iterOfFirstStage;

		/// <summary>
		/// Количество итераций второй стадии подготовки матрицы кодирования
		/// </summary>
		protected double iterOfSecondStage;

		/// <summary>
		/// Поток заполнения матрицы "FLog" перед выполнением кодирования / декодирования
		/// </summary>
		protected Thread thrRSMatrixForming;

		/// <summary>
		/// Событие прекращения подготовки матрицы кодирования
		/// </summary>
		protected ManualResetEvent[] exitEvent;

		/// <summary>
		/// Событие продолжения подготовки матрицы кодирования
		/// </summary>
		protected ManualResetEvent[] executeEvent;

		#endregion Data

		#region Construction & Destruction

		/// <summary>
		/// Конструктор базового класса сущности "RAID-подобный кодек Рида-Соломона"
		/// </summary>
		public RSRaidBase()
		{
			// Создаем экземпляр класса для работы с арифметикой поля Галуа (2^16)
			this.eGF16 = new GF16();

			// Экземляр класса полностью закончил обработку?
			this.finished = true;

			// Основная конфигурация сменилась?
			this.mainConfigChanged = true;

			// Экземпляр класса инициализирован корректно (пригоден к работе)?
			this.configIsOK = false;

			// По-умолчанию устанавливается фоновый приоритет
			this.threadPriority = 0;

			// Инициализируем событие прекращения обработки файла
			this.exitEvent = new ManualResetEvent[] {new ManualResetEvent(false)};

			// Инициализируем cобытие продолжения обработки файла
			this.executeEvent = new ManualResetEvent[] {new ManualResetEvent(false)};

			// Событие, устанавливаемое по завершении обработки
			this.finishedEvent = new ManualResetEvent[] {new ManualResetEvent(true)};
		}

		#endregion Construction & Destruction

		#region Public Operations

		/// <summary>
		/// Запуск процесса заполнения матрицы "FLog" данными
		/// </summary>
		/// <param name="runAsSeparateThread">Запускать в отдельном потоке?</param>
		/// <returns>Булевский флаг операции</returns>
		public bool Prepare(bool runAsSeparateThread)
		{
			// Если поток формирования матрицы "FLog" работает - не позволяем повторный запуск
			if(InProcessing)
			{
				return false;
			}

			// Если конфигурация установлена некорректно - выходим
			if(!this.configIsOK)
			{
				return false;
			}

			// Сбрасываем индикатор актуального состояния переменных-членов
			this.finished = false;

			// Сбрасываем событие завершения обработки
			this.finishedEvent[0].Reset();

			// Указываем, что поток должен выполняться
			this.exitEvent[0].Reset();
			this.executeEvent[0].Set();

			// Если указано, что не требуется запуск в отдельном потоке,
			// запускаем в данном
			if(!runAsSeparateThread)
			{
				// Заполняем матрицу кодирования
				FillFLog();

				// Возвращаем результат обработки
				return this.configIsOK;
			}

			// Создаем поток формирования матрицы "FLog"...
			this.thrRSMatrixForming = new Thread(new ThreadStart(FillFLog));

			//...затем даем ему имя...
			this.thrRSMatrixForming.Name = "RSRaid.FillFLog()";

			//...устанавливаем выбранный приоритет задачи...
			this.thrRSMatrixForming.Priority = this.threadPriority;

			//...и запускаем
			this.thrRSMatrixForming.Start();

			return true;
		}

		/// <summary>
		/// Метод остановки потока
		/// </summary>
		public void Stop()
		{
			// Указываем, что поток обработки больше не должен выполняться
			this.exitEvent[0].Set();

			// Принудительно снимаем с паузы
			this.executeEvent[0].Set();
		}

		/// <summary>
		/// Постановка потока обработки на паузу
		/// </summary>
		public void Pause()
		{
			// Ставим на паузу
			this.executeEvent[0].Reset();
		}

		/// <summary>
		/// Снятие потока обработки с паузы
		/// </summary>
		public void Continue()
		{
			// Снимаем обработку c паузы
			this.executeEvent[0].Set();
		}

		#endregion Public Operations

		#region Protected Operations

		/// <summary>
		/// Нормализация значений "n" и "m" c целью предотвращения переполнения переменных,
		/// хранящих общее количество итераций
		/// </summary>
		protected void NormalizeNM(ref double n, ref double m)
		{
			double maxVal = 0;

			if(n > m)
			{
				maxVal = n;
			}
			else
			{
				maxVal = m;
			}

			double divider = maxVal / 100.0;

			if(divider > 1)
			{
				n /= divider;
				m /= divider;
			}
		}

		/// <summary>
		/// Метод поиска индекса строки,
		/// </summary>
		/// <param name="rowNum">Номер строки</param>
		/// <returns>Индекс строки, пригодной для замены</returns>
		protected int FindSwapRow(int rowNum)
		{
			// Пробегаем по всем имеющимся строкам матрицы
			// в указанном столбце
			for(int i = rowNum; i < (this.n + this.m); i++)
			{
				if(this.D[(i * this.n) + rowNum] != 0)
				{
					return i;
				}
			}

			return -1;
		}

		/// <summary>
		/// Метод перестановки двух строк местами
		/// </summary>
		/// <param name="rowNum1">Индекс первой строки</param>
		/// <param name="rowNum2">Индекс второй строки</param>
		protected void SwapRows(int rowNum1, int rowNum2)
		{
			// Вычисляем смещение до элементов i-ой строки
			int rowNum1this_n = rowNum1 * this.n;
			int rowNum2this_n = rowNum2 * this.n;

			for(int j = 0; j < this.n; j++)
			{
				int dIdx1 = rowNum1this_n + j;
				int dIdx2 = rowNum2this_n + j;

				int tmp = this.D[dIdx1];
				this.D[dIdx1] = this.D[dIdx2];
				this.D[dIdx2] = tmp;
			}
		}

		/// <summary>
		/// Метод получения дисперсной матрицы "D"
		/// </summary>
		/// <returns>Булевский флаг результата операции</returns>
		protected bool MakeDispersalMatrix()
		{
			// Выделяем память под матрицу "FLog"
			this.D = new int[(this.n + this.m) * this.n];

			// Заполняем матрицу данными (формируем матрицу Вандермонда)
			for(int i = 0; i < (this.n + this.m); i++)
			{
				// Смещение в массиве до элементов i-ой строки
				int i_n = i * this.n;

				// Вычисление строки матрицы Вандермонда (этот блок вычислений
				// может быть реализован и без использования функции возведения
				// элемента в степень, но текущая реализация предполагает большую
				// гибкость и понятность)
				for(int j = 0; j < this.n; j++)
				{
					this.D[i_n + j] = this.eGF16.Pow(i, j);
				}
			}

			// Вычисляем распределение процентов итераций по стадиям для
			// корректной обработки процентов
			double allStageIter = this.iterOfFirstStage + this.iterOfSecondStage;
			int percOfFirstStage = (int)((100.0 * this.iterOfFirstStage) / allStageIter);

			// Данная стадия должна занимать хотя бы один процент
			// (для корректности расчетов)
			if(percOfFirstStage == 0)
			{
				percOfFirstStage = 1;
			}

			// Вычисляем значение модуля, который позволит выводить процент обработки
			// ровно при единичном приращении для цикла по "i"
			int progressMod1 = this.n / percOfFirstStage;

			// Если модуль равен нулю, то увеличиваем его до значения "1", чтобы
			// прогресс выводился на каждой итерации
			if(progressMod1 == 0)
			{
				progressMod1 = 1;
			}

			// Цикл выбора диагонального элемента
			for(int k = 1; k < this.n; k++)
			{
				// Ищем строку, в которой элемент на главной
				// диагонали мог бы быть ненулевым
				int swapIdx = FindSwapRow(k);

				// Если подходящая строка не может быть найдена -
				// это ошибка - ...
				if(swapIdx == -1)
				{
					//...указываем на ошибку конфигурации
					this.configIsOK = false;

					// Активируем индикатор актуального состояния переменных-членов
					this.finished = true;

					// Устанавливаем событие завершения обработки
					this.finishedEvent[0].Set();

					return false;
				}

				// Если была найдена строка, отличная от текущей...
				if(swapIdx != k)
				{
					//...меняем строки местами
					SwapRows(swapIdx, k);
				}

				int k_n = k * this.n;

				// Извлекаем диагональный элемент
				int diagElem = this.D[k_n + k];

				// Если диагональный элемент не равен "1", умножаем весь столбец
				// на обратный ему элемент, превращая диагональный в "1"
				if(diagElem != 1)
				{
					// Вычисляем обратный элемент для "diagElem"
					int diagElemInv = this.eGF16.Inv(diagElem);

					// Производим требуемую обработку элементов столбца -
					// умножаем его на элемент, обратный "diagElem"
					for(int i = k; i < (this.n + this.m); i++)
					{
						int dIdx = (i * this.n) + k;

						this.D[dIdx] = this.eGF16.Mul(this.D[dIdx], diagElemInv);
					}
				}

				// Для всех столбцов...
				for(int j = 0; j < this.n; j++)
				{
					// Извлекаем множитель текущего столбца
					int colMult = this.D[k_n + j];

					//...не являющихся столбцами разрешающего элемента...
					if(
						(j != k)
						&&
						(colMult != 0)
						)
					{
						for(int i = k; i < (this.n + this.m); i++)
						{
							int i_n = i * this.n;
							int dIdx = i_n + j;

							//...производим замену Cj = Cj - Dk,j * Ck
							this.D[dIdx] = this.D[dIdx] ^ this.eGF16.Mul(colMult, this.D[i_n + k]);
						}
					}
				}

				// Если есть подписка на делегата обновления прогресса -...
				if(
					((k % progressMod1) == 0)
					&&
					(OnUpdateRSMatrixFormingProgress != null)
					)
				{
					//...выводим данные
					OnUpdateRSMatrixFormingProgress(((double)(k + 1) / (double)this.n) * percOfFirstStage);
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
		/// Метод получения "альтернативной" матрицы "A" : в нем для заполнения матрицы кодирования
		/// из 65535 констант выбираются 32768, таких, чтобы логарифм каждой из них был взаимно
		/// простым со значением "65535", т.е. чтобы их НОД (наибольший общий делитель) был равен
		/// "1". Нарушение этого условия приводит к невозможности обращения матрицы кодирования,
		/// и, соответственно, к невозможности восстановления данных)
		/// </summary>
		/// <returns>Булевский флаг результата операции</returns>
		protected bool MakeAlternativeMatrix()
		{
			// Перебираемое значение логарифма, с целью дальнейшего получения константы
			// для занесения в матрицу путем его потенцирования
			int logBase = 0;

			// Восстанавливаемое по "logBase" основание степени для формирования строки
			// матрицы Вандермонда
			int powBase = 0;

			// Выделяем память под матрицу "FLog"
			this.A = new int[this.m * this.n];

			// Вычисляем распределение процентов итераций по стадиям для
			// корректной обработки процентов
			double allStageIter = this.iterOfFirstStage + this.iterOfSecondStage;
			int percOfFirstStage = (int)((100.0 * this.iterOfFirstStage) / allStageIter);

			// Данная стадия должна занимать хотя бы один процент
			// (для корректности расчетов)
			if(percOfFirstStage == 0)
			{
				percOfFirstStage = 1;
			}

			// Вычисляем значение модуля, который позволит выводить процент обработки
			// ровно при единичном приращении для цикла по "i"
			int progressMod1 = this.m / percOfFirstStage;

			// Если модуль равен нулю, то увеличиваем его до значения "1", чтобы
			// прогресс выводился на каждой итерации
			if(progressMod1 == 0)
			{
				progressMod1 = 1;
			}

			// Заполняем матрицу данными (формируем матрицу Вандермонда)
			for(int i = 0; i < this.m; i++)
			{
				// Пока "logBase" не взаимно просто с "65535"...
				while(
					((logBase % 3) == 0)
					||
					((logBase % 5) == 0)
					||
					((logBase % 17) == 0)
					||
					((logBase % 257) == 0)
					)
				{
					++logBase;
				}

				//...затем, восстанавливаем его значение...
				powBase = this.eGF16.Exp(logBase++);

				// Смещение в массиве до элементов i-ой строки
				int i_n = i * this.n;

				for(int j = 0; j < this.n; j++)
				{
					//...и используем для формирования строки матрицы Вандермонда
					this.A[i_n + j] = this.eGF16.Pow(powBase, j);
				}

				// Если есть подписка на делегата обновления прогресса -...
				if(
					((i % progressMod1) == 0)
					&&
					(OnUpdateRSMatrixFormingProgress != null)
					)
				{
					//...выводим данные
					OnUpdateRSMatrixFormingProgress(((double)(i + 1) / (double)this.m) * percOfFirstStage);
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
		/// Метод получения матрицы Коши
		/// </summary>
		/// <returns>Булевский флаг результата операции</returns>
		protected bool MakeCauchyMatrix()
		{
			// Выделяем память под матрицу "FLog"
			this.C = new int[this.m * this.n];

			// Вычисляем распределение процентов итераций по стадиям для
			// корректной обработки процентов
			double allStageIter = this.iterOfFirstStage + this.iterOfSecondStage;
			int percOfFirstStage = (int)((100.0 * this.iterOfFirstStage) / allStageIter);

			// Данная стадия должна занимать хотя бы один процент
			// (для корректности расчетов)
			if(percOfFirstStage == 0)
			{
				percOfFirstStage = 1;
			}

			// Вычисляем значение модуля, который позволит выводить процент обработки
			// ровно при единичном приращении для цикла по "i"
			int progressMod1 = this.m / percOfFirstStage;

			// Если модуль равен нулю, то увеличиваем его до значения "1", чтобы
			// прогресс выводился на каждой итерации
			if(progressMod1 == 0)
			{
				progressMod1 = 1;
			}

			// Заполняем матрицу данными (формируем матрицу Коши)
			for(int i = 0; i < this.m; i++)
			{
				// Смещение в массиве до элементов i-ой строки
				int i_n = i * this.n;

				// Оптимизация :)
				int i_pl_n = i + this.n;

				for(int j = 0; j < this.n; j++)
				{
					// Формируем строку матрицы Коши...
					this.C[i_n + j] = this.eGF16.Inv(this.eGF16.Exp(i_pl_n) ^ this.eGF16.Exp(j));
				}

				// Если есть подписка на делегата обновления прогресса -...
				if(
					((i % progressMod1) == 0)
					&&
					(OnUpdateRSMatrixFormingProgress != null)
					)
				{
					//...выводим данные
					OnUpdateRSMatrixFormingProgress(((double)(i + 1) / (double)this.m) * percOfFirstStage);
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
		/// Заполнение матрицы "FLog" данными
		/// </summary>
		protected virtual void FillFLog()
		{
		}

		#endregion Protected Operations
	}
}