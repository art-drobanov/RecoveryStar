/*----------------------------------------------------------------------+
 |  filename:   FileCodec.cs                                            |
 |----------------------------------------------------------------------|
 |  version:    2.22                                                    |
 |  revision:   02.04.2013 17:00                                        |
 |  authors:    Дробанов Артём Федорович (DrAF),                        |
 |              RUSpectrum (г. Оренбург).                               |
 |  e-mail:     draf@mail.ru                                            |
 |  purpose:    Кодирование множества файлов в RAID-подобной схеме      |
 +----------------------------------------------------------------------*/

using System;
using System.Threading;
using System.IO;

namespace RecoveryStar
{
	/// <summary>
	/// Класс для кодирования файлов в RAID-подобной схеме
	/// </summary>
	public class FileCodec
	{
		#region Delegates

		/// <summary>
		/// Делегат обновления процесса формирования матрицы "F"
		/// </summary>
		public OnUpdateDoubleValueHandler OnUpdateRSMatrixFormingProgress;

		/// <summary>
		/// Делегат завершения процесса формирования матрицы "F"
		/// </summary>
		public OnEventHandler OnRSMatrixFormingFinish;

		/// <summary>
		/// Делегат обновления прогресса открытия файловых потоков
		/// </summary>
		public OnUpdateDoubleValueHandler OnUpdateFileStreamsOpeningProgress;

		/// <summary>
		/// Делегат завершения процесса открытия файловых потоков
		/// </summary>
		public OnEventHandler OnFileStreamsOpeningFinish;

		/// <summary>
		/// Делегат начала кодирования Рида-Соломона
		/// </summary>
		public OnEventHandler OnStartedRSCoding;

		/// <summary>
		/// Делегат обновления прогресса кодирования файлов
		/// </summary>
		public OnUpdateDoubleValueHandler OnUpdateFileCodingProgress;

		/// <summary>
		/// Делегат завершения процесса кодирования файлов
		/// </summary>
		public OnEventHandler OnFileCodingFinish;

		/// <summary>
		/// Делегат обновления прогресса закрытия файловых потоков
		/// </summary>
		public OnUpdateDoubleValueHandler OnUpdateFileStreamsClosingProgress;

		/// <summary>
		/// Делегат завершения процесса закрытия файловых потоков
		/// </summary>
		public OnEventHandler OnFileStreamsClosingFinish;

		#endregion Delegates

		#region Constants

		/// <summary>
		/// Минимальный общий размер буферизации - 64 Мб
		/// </summary>
		private const int minTotalBufferSize = 1 << 26;

		#endregion Constants

		#region Public Properties & Data

		/// <summary>
		/// Булевское свойство "Файл обрабатывается?"
		/// </summary>
		public bool InProcessing
		{
			get
			{
				if(
					(this.thrFileCodec != null)
					&&
					(
						(this.thrFileCodec.ThreadState == ThreadState.Running)
						||
						(this.thrFileCodec.ThreadState == ThreadState.WaitSleepJoin)
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
		/// Обработка файлов произведена корректно?"
		/// </summary>
		public bool ProcessedOK
		{
			get
			{
				// Если класс не занят обработкой - возвращаем значение
				if(!InProcessing)
				{
					return this.processedOK;
				}
				else
				{
					return false;
				}
			}
		}

		/// <summary>
		/// Размер общего Файлового буфера (распределяемого между потоками)
		/// </summary>
		public int TotalBufferSize
		{
			get
			{
				// Если класс не занят обработкой - возвращаем значение...
				if(!InProcessing)
				{
					return this.maxTotalBufferSize;
				}
				else
				{
					//...а иначе сообщаем об обратном
					return -1;
				}
			}

			set
			{
				// Если класс не занят обработкой - устанавливаем значение...
				if(!InProcessing)
				{
					//... но только если оно не нарушает минимальный размер буфера - 64 Мб
					if(value > minTotalBufferSize)
					{
						this.maxTotalBufferSize = value;
					}
					else
					{
						this.maxTotalBufferSize = minTotalBufferSize;
					}
				}
			}
		}

		/// <summary>
		/// Используется автовыбор размера буфера?
		/// </summary>
		public bool AutoBuffering
		{
			get
			{
				// Если класс не занят обработкой - возвращаем значение...
				if(!InProcessing)
				{
					return this.autoBuffering;
				}
				else
				{
					//...а иначе сообщаем об обратном
					return false;
				}
			}

			set
			{
				// Если класс не занят обработкой - устанавливаем значение...
				if(!InProcessing)
				{
					this.autoBuffering = value;
				}
			}
		}

		/// <summary>
		/// Коэффициент поглощения памяти при автобуферизации
		/// </summary>
		public double MemConsumeCoeff
		{
			get
			{
				// Если класс не занят обработкой - возвращаем значение...
				if(!InProcessing)
				{
					return this.memConsumeCoeff;
				}
				else
				{
					//...а иначе сообщаем об обратном
					return -1;
				}
			}

			set
			{
				// Если класс не занят обработкой - устанавливаем значение...
				if(!InProcessing)
				{
					if(
						(value >= 0.1)
						&&
						(value <= 1)
						)
					{
						this.memConsumeCoeff = value;
					}
				}
			}
		}

		/// <summary>
		/// Приоритет процесса
		/// </summary>
		public int ThreadPriority
		{
			get { return (int)this.threadPriority; }

			set
			{
				if(
					(this.thrFileCodec != null)
					&&
					(this.thrFileCodec.IsAlive)
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
					this.thrFileCodec.Priority = this.threadPriority;

					if(this.eRSRaidEncoder != null)
					{
						this.eRSRaidEncoder.ThreadPriority = value;
					}

					if(this.eRSRaidDecoder != null)
					{
						this.eRSRaidDecoder.ThreadPriority = value;
					}
				}
			}
		}

		/// <summary>
		/// Событие, устанавливаемое по завершении обработки
		/// </summary>
		public ManualResetEvent[] FinishedEvent
		{
			get { return this.finishedEvent; }
		}

		#endregion Public Properties & Data

		#region Data

		/// <summary>
		/// Объект для упаковки (распаковки) имени в префиксный формат
		/// </summary>
		private FileNamer eFileNamer;

		/// <summary>
		/// RAID-подобный кодер Рида-Соломона
		/// </summary>
		private RSRaidEncoder eRSRaidEncoder;

		/// <summary>
		/// RAID-подобный декодер Рида-Соломона
		/// </summary>
		private RSRaidDecoder eRSRaidDecoder;

		/// <summary>
		/// Арифметика поля Галуа
		/// </summary>
		private GF16 eGF16;

		/// <summary>
		/// Размер общего файлового буфера (на все потоки)
		/// </summary>
		private int maxTotalBufferSize;

		/// <summary>
		/// Используется автовыбор размера буфера?
		/// </summary>
		private bool autoBuffering;

		/// <summary>
		/// Коэффициент поглощения памяти при автобуферизации
		/// </summary>
		private double memConsumeCoeff;

		/// <summary>
		/// Путь к файлам для обработки
		/// </summary>
		private String path;

		/// <summary>
		/// Имя файла, которому принадлежит множество томов
		/// </summary>
		private String fileName;

		/// <summary>
		/// Количество основных томов
		/// </summary>
		private int dataCount;

		/// <summary>
		/// Количество томов для восстановления
		/// </summary>
		private int eccCount;

		/// <summary>
		/// Тип кодека Рида-Соломона (по типу используемой матрицы кодирования)
		/// </summary>
		private int codecType;

		/// <summary>
		/// Вектор, указывающий на состав томов
		/// </summary>
		private int[] volList;

		/// <summary>
		/// Экземляр класса полностью закончил обработку?
		/// </summary>
		private bool finished;

		/// <summary>
		/// Обработка произведена корректно?
		/// </summary>
		private bool processedOK;

		/// <summary>
		/// Поток кодирования данных
		/// </summary>
		private Thread thrFileCodec;

		/// <summary>
		/// Приоритет процесса разбиения (склеивания) файла
		/// </summary>
		private ThreadPriority threadPriority;

		/// <summary>
		/// Событие прекращения обработки файлов
		/// </summary>
		private ManualResetEvent[] exitEvent;

		/// <summary>
		/// Событие продолжения обработки файлов
		/// </summary>
		private ManualResetEvent[] executeEvent;

		/// <summary>
		/// Событие "пробуждения" цикла ожидания
		/// </summary>
		private ManualResetEvent[] wakeUpEvent;

		/// <summary>
		/// Событие, устанавливаемое по завершении обработки
		/// </summary>
		private ManualResetEvent[] finishedEvent;

		#endregion Data

		#region Construction & Destruction

		/// <summary>
		/// Конструктор класса
		/// </summary>
		public FileCodec()
		{
			// Инициализируем экземпляр класса для упаковки (распаковки) имени файла
			// в префиксный формат
			this.eFileNamer = new FileNamer();

			// Путь к файлам для обработки по-умолчанию пустой
			this.path = "";

			// Инициализируем имя файла по-умолчанию
			this.fileName = "NONAME";

			// Создаем экземпляр для получения системной информации
			SystemInfo eSystemInfo = new SystemInfo();

			// Создаем класс арифметики поля Галуа
			this.eGF16 = new GF16();

			// Размер файлового буфера для всех потоков при неавтоматическом
			// выборе по-умолчанию составляет 1 / 8 физического объема памяти
			TotalBufferSize = (int)(eSystemInfo.TotalPhysicalMemory / 8);
			TotalBufferSize = (TotalBufferSize < 0) ? int.MaxValue : TotalBufferSize;

			// По-умолчанию устанавливаем автоматическую буферизацию на потоки
			this.autoBuffering = true;

			// Коэффициент поглощения памяти при автобуферизации по-умолчанию составляет 0.5
			this.memConsumeCoeff = 0.5;

			// Экземляр класса полностью закончил обработку?
			this.finished = true;

			// Обработка произведена корректно?
			this.processedOK = false;

			// По-умолчанию устанавливается фоновый приоритет
			this.threadPriority = 0;

			// Инициализируем событие прекращения обработки файлов
			this.exitEvent = new ManualResetEvent[] {new ManualResetEvent(false)};

			// Инициализируем cобытие продолжения обработки файлов
			this.executeEvent = new ManualResetEvent[] {new ManualResetEvent(false)};

			// Инициализируем cобытие "пробуждения" цикла ожидания
			this.wakeUpEvent = new ManualResetEvent[] {new ManualResetEvent(false)};

			// Событие, устанавливаемое по завершении обработки
			this.finishedEvent = new ManualResetEvent[] {new ManualResetEvent(true)};
		}

		#endregion Construction & Destruction

		#region Public Operations

		/// <summary>
		/// Вычисление информации для восстановления основных томов
		/// </summary>
		/// <param name="path">Путь к файлам для обработки</param>
		/// <param name="fileName">Имя файла, которому принадлежит множество томов</param>
		/// <param name="dataCount">Количество основных томов</param>
		/// <param name="eccCount">Количество томов для восстановления</param>
		/// <param name="codecType">Тип кодека Рида-Соломона (по типу матрицы)</param>
		/// <param name="runAsSeparateThread">Запускать в отдельном потоке?</param>
		/// <returns>Булевский флаг операции</returns>
		public bool StartToEncode(String path, String fileName, int dataCount, int eccCount, int codecType, bool runAsSeparateThread)
		{
			// Если поток кодирования файла работает - не позволяем повторный запуск
			if(InProcessing)
			{
				return false;
			}

			// Сбрасываем флаг корректности результата перед запуском потока
			this.processedOK = false;

			// Сбрасываем индикатор актуального состояния переменных-членов
			this.finished = false;

			// Сохраняем путь к файлам для обработки
			if(path == null)
			{
				this.path = "";
			}
			else
			{
				// Производим выделение пути из "path" в случае,
				// если туда было записано полное имя
				this.path = this.eFileNamer.GetPath(path);
			}

			if(fileName == null)
			{
				// Активируем индикатор актуального состояния переменных-членов
				this.finished = true;

				// Устанавливаем событие завершения обработки
				this.finishedEvent[0].Set();

				return false;
			}

			// Производим выделение короткого имени файла из "fileName" в случае,
			// если туда было записано полное имя
			this.fileName = this.eFileNamer.GetShortFileName(fileName);

			// Проверяем на некорректную конфигурацию
			if(
				(dataCount <= 0)
				||
				(eccCount <= 0)
				||
				((dataCount + eccCount) > (int)RSConst.MaxVolCountAlt)
				)
			{
				// Активируем индикатор актуального состояния переменных-членов
				this.finished = true;

				// Устанавливаем событие завершения обработки
				this.finishedEvent[0].Set();

				return false;
			}

			// Сохраняем количество основных томов
			this.dataCount = dataCount;

			// Сохраняем количество томов для восстановления
			this.eccCount = eccCount;

			// Сохраняем тип кодека Рида-Соломона
			this.codecType = codecType;

			// Указываем, что поток должен выполняться
			this.exitEvent[0].Reset();
			this.executeEvent[0].Set();
			this.wakeUpEvent[0].Reset();
			this.finishedEvent[0].Reset();

			// Если указано, что не требуется запуск в отдельном потоке,
			// запускаем в данном
			if(!runAsSeparateThread)
			{
				// Кодируем набор томов с получением томов для восстановления
				Encode();

				// Возвращаем результат обработки
				return this.processedOK;
			}

			// Создаем поток кодирования файлов...
			this.thrFileCodec = new Thread(new ThreadStart(Encode));

			//...затем даем ему имя...
			this.thrFileCodec.Name = "FileCodec.Encode()";

			//...устанавливаем выбранный приоритет задачи...
			this.thrFileCodec.Priority = this.threadPriority;

			//...и запускаем его
			this.thrFileCodec.Start();

			// Сообщаем, что все нормально
			return true;
		}

		/// <summary>
		/// Восстановление утерянных основных томов
		/// </summary>
		/// <param name="path">Путь к файлам для обработки</param>
		/// <param name="fileName">Имя файла, которому принадлежит множество томов</param>
		/// <param name="dataCount">Количество основных томов</param>
		/// <param name="eccCount">Количество томов для восстановления</param>
		/// <param name="volList">Список номеров имеющихся томов</param>
		/// <param name="codecType">Тип кодека Рида-Соломона (по типу матрицы)</param>
		/// <param name="runAsSeparateThread">Запускать в отдельном потоке?</param>
		/// <returns>Булевский флаг операции</returns>
		public bool StartToDecode(String path, String fileName, int dataCount, int eccCount, int[] volList, int codecType, bool runAsSeparateThread)
		{
			// Если поток декодирования файла работает - не позволяем повторный запуск
			if(InProcessing)
			{
				return false;
			}

			// Сбрасываем флаг корректности результата перед запуском потока
			this.processedOK = false;

			// Сбрасываем индикатор актуального состояния переменных-членов
			this.finished = false;

			// Сохраняем путь к файлам для обработки
			if(path == null)
			{
				this.path = "";
			}
			else
			{
				// Производим выделение пути из "path" в случае,
				// если туда было записано полное имя
				this.path = this.eFileNamer.GetPath(path);
			}

			if(fileName == null)
			{
				// Активируем индикатор актуального состояния переменных-членов
				this.finished = true;

				// Устанавливаем событие завершения обработки
				this.finishedEvent[0].Set();

				return false;
			}

			// Производим выделение короткого имени файла из "fileName" в случае,
			// если туда был записано полное имя
			this.fileName = this.eFileNamer.GetShortFileName(fileName);

			// Проверяем на некорректную конфигурацию
			if(
				(dataCount <= 0)
				||
				(eccCount <= 0)
				||
				((dataCount + eccCount) > (int)RSConst.MaxVolCountAlt)
				)
			{
				// Активируем индикатор актуального состояния переменных-членов
				this.finished = true;

				// Устанавливаем событие завершения обработки
				this.finishedEvent[0].Set();

				return false;
			}

			// Сохраняем количество основных томов
			this.dataCount = dataCount;

			// Сохраняем количество томов для восстановления
			this.eccCount = eccCount;

			// Сохраняем список номеров имеющихся томов
			this.volList = volList;

			// Сохраняем тип кодека Рида-Соломона
			this.codecType = codecType;

			// Указываем, что поток должен выполняться
			this.exitEvent[0].Reset();
			this.executeEvent[0].Set();
			this.wakeUpEvent[0].Reset();
			this.finishedEvent[0].Reset();

			// Если указано, что не требуется запуск в отдельном потоке,
			// запускаем в данном
			if(!runAsSeparateThread)
			{
				// Декодируем последовательность файлов с восстановлением основных томов
				Decode();

				// Возвращаем результат обработки
				return this.processedOK;
			}

			// Создаем поток восстановления основных томов...
			this.thrFileCodec = new Thread(new ThreadStart(Decode));

			//...затем даем ему имя...
			this.thrFileCodec.Name = "FileCodec.Decode()";

			//...устанавливаем выбранный приоритет задачи...
			this.thrFileCodec.Priority = this.threadPriority;

			//...и запускаем его
			this.thrFileCodec.Start();

			// Сообщаем, что все нормально
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

			// Снимаем с ожидания в цикле
			this.wakeUpEvent[0].Set();
		}

		/// <summary>
		/// Постановка потока обработки на паузу
		/// </summary>
		public void Pause()
		{
			// Ставим на паузу
			this.executeEvent[0].Reset();

			// Снимаем с ожидания в цикле
			this.wakeUpEvent[0].Set();
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

		#region Private Operations

		/// <summary>
		/// Кодирование последовательности файлов
		/// </summary>
		private void Encode()
		{
			// Создаем RAID-подобный кодер Рида-Соломона
			if(this.eRSRaidEncoder == null)
			{
				this.eRSRaidEncoder = new RSRaidEncoder(this.dataCount, this.eccCount, this.codecType);
			}
			else
			{
				this.eRSRaidEncoder.SetConfig(this.dataCount, this.eccCount, this.codecType);
			}

			// Подписываемся на делегатов
			this.eRSRaidEncoder.OnUpdateRSMatrixFormingProgress = OnUpdateRSMatrixFormingProgress;
			this.eRSRaidEncoder.OnRSMatrixFormingFinish = OnRSMatrixFormingFinish;

			// Запускаем подготовку RAID-подобного кодера Рида-Соломона
			if(this.eRSRaidEncoder.Prepare(true))
			{
				// Цикл ожидания завершения подготовки кодера Рида-Соломона к работе
				while(true)
				{
					// Если не обнаружили установленного события "executeEvent",
					// то пользователь хочет, чтобы мы поставили обработку на паузу -
					if(!ManualResetEvent.WaitAll(this.executeEvent, 0, false))
					{
						//...приостанавливаем работу контролируемого алгоритма...
						this.eRSRaidEncoder.Pause();

						//...и сами засыпаем
						ManualResetEvent.WaitAll(this.executeEvent);

						// А когда проснулись, указываем, что обработка должна продолжаться
						this.eRSRaidEncoder.Continue();
					}

					// Ждем любое из перечисленных событий...
					int eventIdx = ManualResetEvent.WaitAny(new ManualResetEvent[] {this.wakeUpEvent[0], this.exitEvent[0], this.eRSRaidEncoder.FinishedEvent[0]});

					//...если получили сигнал к тому, чтобы проснуться -
					// переходим на новую итерацию, т.к. просыпаемся
					// перед постановкой на паузу...
					if(eventIdx == 0)
					{
						//...предварительно сбросив событие, заставившее нас проснуться
						this.wakeUpEvent[0].Reset();

						continue;
					}

					//...если получили сигнал к выходу из обработки...
					if(eventIdx == 1)
					{
						//...останавливаем контролируемый алгоритм
						this.eRSRaidEncoder.Stop();

						// Указываем на то, что обработка была прервана
						this.processedOK = false;

						// Активируем индикатор актуального состояния переменных-членов
						this.finished = true;

						// Устанавливаем событие завершения обработки
						this.finishedEvent[0].Set();

						return;
					}

					//...если получили сигнал о завершении обработки вложенным алгоритмом...
					if(eventIdx == 2)
					{
						//...выходим из цикла ожидания завершения (этого и ждали в while(true)!)
						break;
					}
				} // while(true)
			}
			else
			{
				// Сбрасываем флаг корректности результата
				this.processedOK = false;

				// Активируем индикатор актуального состояния переменных-членов
				this.finished = true;

				// Устанавливаем событие завершения обработки
				this.finishedEvent[0].Set();

				return;
			}

			// Когда поток уже не работает, установленное им булевское свойство,
			// возможно, ещё "не проявилось"
			for(int i = 0; i < (int)WaitCount.MaxWaitCount; i++)
			{
				if(!this.eRSRaidEncoder.Finished)
				{
					Thread.Sleep((int)WaitTime.MinWaitTime);
				}
				else
				{
					break;
				}
			}

			// Если кодер не сконфигурировался корректно - выходим...
			if(!this.eRSRaidEncoder.ConfigIsOK)
			{
				//...указывая на ошибку
				this.processedOK = false;

				// Активируем индикатор актуального состояния переменных-членов
				this.finished = true;

				// Устанавливаем событие завершения обработки
				this.finishedEvent[0].Set();

				return;
			}

			// Выделяем память под массивы входных и выходных данных кодера
			int[] sourceLog = new int[this.dataCount];
			int[] target = new int[this.eccCount];

			// Буфер для работы с парами байт
			byte[] wordBuff = new byte[2];

			// Выделяем память под массивы файловых потоков
			BufferedStream[] fileStreamSourceArr = new BufferedStream[this.dataCount];
			BufferedStream[] fileStreamTargetArr = new BufferedStream[this.eccCount];

			// Текущий размер буфера (далее уточняется значением по-умолчанию, либо
			// производится автовыбор, исходя из доступного объема физической памяти)
			int currentTotalBufferSize = -1;

			try
			{
				// Вычисляем значение модуля, который позволит выводить процент обработки
				// ровно при единичном приращении для цикла по "volNum"
				int progressMod1 = (this.dataCount + this.eccCount) / 100;

				// Если модуль равен нулю, то увеличиваем его до значения "1", чтобы
				// прогресс выводился на каждой итерации (файл очень маленький)
				if(progressMod1 == 0)
				{
					progressMod1 = 1;
				}

				// Имя файла для обработки
				String fileName;

				// Номер текущего тома
				int volNum;

				// Создаем экземпляр для получения системной информации
				SystemInfo eSystemInfo = new SystemInfo();

				// Уточняем максимальный размер буфера для файловых потоков...
				if(this.autoBuffering)
				{
					//...берем часть доступной физической памяти...
					currentTotalBufferSize = (int)(this.memConsumeCoeff * (double)eSystemInfo.FreePhysicalMemory);
					currentTotalBufferSize = (currentTotalBufferSize < 0) ? int.MaxValue : currentTotalBufferSize;
				}
				else
				{
					//...либо производим жесткий выбор значения
					currentTotalBufferSize = this.TotalBufferSize;
				}

				// Теперь важно уточнить, нужен ли такой большой буфер для работы с
				// данным набором томов. Для этого откроем любой из томов, и узнав его размер
				// вычислим общий требуемый объем на буфера

				// Считываем первоначальное имя файла,...
				fileName = this.fileName;

				//...упаковываем его в префиксный формат...
				this.eFileNamer.Pack(ref fileName, 0, this.dataCount, this.eccCount, this.codecType);

				//...формируем полное имя файла...
				fileName = this.path + fileName;

				//...а затем открываем файловый поток, чтобы узнать размер первого тома
				FileStream eFileStream = new FileStream(fileName, FileMode.Open, System.IO.FileAccess.Read);

				// Узнаем требуемый максимальный общий размер буфера
				long totalBufferSizeNeeded = eFileStream.Length * (this.dataCount + this.eccCount);

				// Закрываем файловый поток
				eFileStream.Close();

				// Если требуется меньше, чем предполагалось, корректируем данную величину
				if(totalBufferSizeNeeded < currentTotalBufferSize)
				{
					currentTotalBufferSize = (int)totalBufferSizeNeeded;
				}

				// Вычисляем размер буфера на том
				int currentVolumeBufferSize = currentTotalBufferSize / (this.dataCount + this.eccCount);

				// Инициализируем массивы файловых потоков основных томов
				for(volNum = 0; volNum < this.dataCount; volNum++)
				{
					// Считываем первоначальное имя файла,...
					fileName = this.fileName;

					//...упаковываем его в префиксный формат...
					this.eFileNamer.Pack(ref fileName, volNum, this.dataCount, this.eccCount, this.codecType);

					//...формируем полное имя файла...
					fileName = this.path + fileName;

					//...и открываем на его основе входной файловый поток
					fileStreamSourceArr[volNum] = new BufferedStream(new FileStream(fileName, FileMode.Open, System.IO.FileAccess.Read), currentVolumeBufferSize);

					// Если есть подписка на делегата обновления прогресса -...
					if(
						((volNum % progressMod1) == 0)
						&&
						(OnUpdateFileStreamsOpeningProgress != null)
						)
					{
						//...выводим данные
						OnUpdateFileStreamsOpeningProgress(((double)(volNum + 1) / (double)(this.dataCount + this.eccCount)) * 100);
					}

					// В случае, если требуется постановка на паузу, событие "executeEvent"
					// будет сброшено, и будем на паузе вплоть до его появления
					ManualResetEvent.WaitAll(this.executeEvent);

					// Если указано, что требуется выйти из потока - выходим
					if(ManualResetEvent.WaitAll(this.exitEvent, 0, false))
					{
						// Указываем, что обработка произведена некорректно
						this.processedOK = false;

						// Активируем индикатор актуального состояния переменных-членов
						this.finished = true;

						// Устанавливаем событие завершения обработки
						this.finishedEvent[0].Set();

						return;
					}
				}

				// Инициализируем массивы файловых потоков томов для восстановления
				for(int eccNum = 0; volNum < (this.dataCount + this.eccCount); volNum++, eccNum++)
				{
					// Считываем первоначальное имя файла...
					fileName = this.fileName;

					//...упаковываем его в префиксный формат...
					this.eFileNamer.Pack(ref fileName, volNum, this.dataCount, this.eccCount, this.codecType);

					//...формируем полное имя файла...
					fileName = this.path + fileName;

					// ...затем производим тест на наличие файла...
					if(File.Exists(fileName))
					{
						//...если таковой имеется, ставим на него атрибуты
						// по-умолчанию...
						File.SetAttributes(fileName, FileAttributes.Normal);
					}

					//...и открываем на его основе выходной файловый поток
					fileStreamTargetArr[eccNum] = new BufferedStream(new FileStream(fileName, FileMode.Create, System.IO.FileAccess.Write), (currentTotalBufferSize / (this.dataCount + this.eccCount)));

					// Если есть подписка на делегата обновления прогресса -...
					if(
						((volNum % progressMod1) == 0)
						&&
						(OnUpdateFileStreamsOpeningProgress != null)
						)
					{
						//...выводим данные
						OnUpdateFileStreamsOpeningProgress(((double)(volNum + 1) / (double)(this.dataCount + this.eccCount)) * 100);
					}

					// В случае, если требуется постановка на паузу, событие "executeEvent"
					// будет сброшено, и будем на паузе вплоть до его появления
					ManualResetEvent.WaitAll(this.executeEvent);

					// Если указано, что требуется выйти из потока - выходим
					if(ManualResetEvent.WaitAll(this.exitEvent, 0, false))
					{
						// Указываем, что обработка произведена некорректно
						this.processedOK = false;

						// Активируем индикатор актуального состояния переменных-членов
						this.finished = true;

						// Устанавливаем событие завершения обработки
						this.finishedEvent[0].Set();

						return;
					}
				}

				// Указываем теперь, что все файловые потоки открыты
				if(OnFileStreamsOpeningFinish != null)
				{
					OnFileStreamsOpeningFinish();
				}

				// Вычисляем значение модуля, который позволит выводить процент обработки
				// ровно при единичном приращении
				int progressMod2 = (int)(fileStreamSourceArr[0].Length / (2 * 100));

				// Если модуль равен нулю, то увеличиваем его до значения "1", чтобы
				// прогресс выводился на каждой итерации (файл очень маленький)
				if(progressMod2 == 0)
				{
					progressMod2 = 1;
				}

				// Указываем пользователю на то, что кодирование уже началось
				if(OnStartedRSCoding != null)
				{
					OnStartedRSCoding();
				}

				// Работаем со всеми срезами пар байт в исходных потоках
				for(int i = 0; i < (fileStreamSourceArr[0].Length / 2); i++)
				{
					// Заполняем вектор исходных данных кодера данными текущего среза
					for(int j = 0; j < this.dataCount; j++)
					{
						// Читаем пару байт из входного потока
                        int dataLen = 2;
						int readed = 0;
						int toRead = 0;
                        while((toRead = dataLen - (readed += fileStreamSourceArr[j].Read(wordBuff, readed, toRead))) != 0) ;

						// Производим слияние двух значений byte в int
						sourceLog[j] = this.eGF16.Log((int)(((uint)(wordBuff[0] << 0) & 0x00FF)
						                                    |
						                                    ((uint)(wordBuff[1] << 8) & 0xFF00)));
					}

					// Кодируем данные (получаем тома для восстановления)
					this.eRSRaidEncoder.Process(sourceLog, target);

					// Выводим в файлы вектор избыточных данных (ecc)
					for(int j = 0; j < this.eccCount; j++)
					{
						// Производим разделение одного значения на два (int16 на два byte)
						wordBuff[0] = (byte)((target[j] >> 0) & 0x00FF);
						wordBuff[1] = (byte)((target[j] >> 8) & 0x00FF);

						// Теперь пишем пару байт в выходной поток
						fileStreamTargetArr[j].Write(wordBuff, 0, 2);
					}

					// Выводим прогресс обработки через каждый процент
					if(
						(i != 0)
						&&
						((i % progressMod2) == 0)
						&&
						(OnUpdateFileCodingProgress != null)
						)
					{
						OnUpdateFileCodingProgress(((double)(i + 1) / (double)fileStreamSourceArr[0].Length) * 200.0);
					}

					// В случае, если требуется постановка на паузу, событие "executeEvent"
					// будет сброшено, и будем на паузе вплоть до его появления
					ManualResetEvent.WaitAll(this.executeEvent);

					// Если указано, что требуется выйти из потока - выходим
					if(ManualResetEvent.WaitAll(this.exitEvent, 0, false))
					{
						// Закрываем входные файловые потоки
						for(int j = 0; j < this.dataCount; j++)
						{
							if(fileStreamSourceArr[j] != null)
							{
								fileStreamSourceArr[j].Close();
								fileStreamSourceArr[j] = null;
							}
						}

						// Закрываем выходные файловые потоки
						for(int j = 0; j < this.eccCount; j++)
						{
							if(fileStreamTargetArr[j] != null)
							{
								fileStreamTargetArr[j].Close();
								fileStreamTargetArr[j] = null;
							}
						}

						// Указываем на то, что обработка была прервана
						this.processedOK = false;

						// Активируем индикатор актуального состояния переменных-членов
						this.finished = true;

						// Устанавливаем событие завершения обработки
						this.finishedEvent[0].Set();

						return;
					}
				}

				// Сообщаем, что обработка файлов закончена
				if(OnFileCodingFinish != null)
				{
					OnFileCodingFinish();
				}

				// Сбрасываем номер тома
				volNum = -1;

				// Закрываем входные файловые потоки
				for(int i = 0; i < this.dataCount; i++)
				{
					if(fileStreamSourceArr[i] != null)
					{
						fileStreamSourceArr[i].Close();
						fileStreamSourceArr[i] = null;
					}

					// Если есть подписка на делегата обновления прогресса -...
					if(
						((++volNum % progressMod1) == 0)
						&&
						(OnUpdateFileStreamsClosingProgress != null)
						)
					{
						//...выводим данные
						OnUpdateFileStreamsClosingProgress(((double)(volNum + 1) / (double)(this.dataCount + this.eccCount)) * 100);
					}
				}

				// Закрываем выходные файловые потоки
				for(int i = 0; i < this.eccCount; i++)
				{
					if(fileStreamTargetArr[i] != null)
					{
						fileStreamTargetArr[i].Flush();
						fileStreamTargetArr[i].Close();
						fileStreamTargetArr[i] = null;
					}

					// Если есть подписка на делегата обновления прогресса -...
					if(
						((++volNum % progressMod1) == 0)
						&&
						(OnUpdateFileStreamsClosingProgress != null)
						)
					{
						//...выводим данные
						OnUpdateFileStreamsClosingProgress(((double)(volNum + 1) / (double)(this.dataCount + this.eccCount)) * 100);
					}
				}

				// Сообщаем, что закрытие файловых потоков закончилось
				if(OnFileStreamsClosingFinish != null)
				{
					OnFileStreamsClosingFinish();
				}
			}

			// Если было хотя бы одно исключение - закрываем файловый поток и
			// сообщаем об ошибке
			catch
			{
				// Закрываем входные файловые потоки
				for(int i = 0; i < this.dataCount; i++)
				{
					if(fileStreamSourceArr[i] != null)
					{
						fileStreamSourceArr[i].Close();
						fileStreamSourceArr[i] = null;
					}
				}

				// Закрываем выходные файловые потоки
				for(int i = 0; i < this.eccCount; i++)
				{
					if(fileStreamTargetArr[i] != null)
					{
						fileStreamTargetArr[i].Close();
						fileStreamTargetArr[i] = null;
					}
				}

				// Указываем на то, что произошла ошибка работы с файлами
				this.processedOK = false;

				// Активируем индикатор актуального состояния переменных-членов
				this.finished = true;

				// Устанавливаем событие завершения обработки
				this.finishedEvent[0].Set();

				return;
			}

			// Указываем на то, что обработка была произведена корректно
			this.processedOK = true;

			// Активируем индикатор актуального состояния переменных-членов
			this.finished = true;

			// Устанавливаем событие завершения обработки
			this.finishedEvent[0].Set();
		}

		/// <summary>
		/// Декодирование последовательности файлов
		/// </summary>
		private void Decode()
		{
			// Список поврежденных основных томов
			int[] damagedVolList = new int[this.dataCount];

			// Счетчик количества поврежденных томов
			int damagedVolCount = 0;

			// Создаем RAID-подобный декодер Рида-Соломона
			if(this.eRSRaidDecoder == null)
			{
				this.eRSRaidDecoder = new RSRaidDecoder(this.dataCount, this.eccCount, this.volList, this.codecType);
			}
			else
			{
				this.eRSRaidDecoder.SetConfig(this.dataCount, this.eccCount, this.volList, this.codecType);
			}

			// Подписываемся на делегатов
			this.eRSRaidDecoder.OnUpdateRSMatrixFormingProgress = OnUpdateRSMatrixFormingProgress;
			this.eRSRaidDecoder.OnRSMatrixFormingFinish = OnRSMatrixFormingFinish;

			// Запускаем подготовку RAID-подобного декодера Рида-Соломона
			if(this.eRSRaidDecoder.Prepare(true))
			{
				// Цикл ожидания завершения подготовки декодера Рида-Соломона к работе
				while(true)
				{
					// Если не обнаружили установленного события "executeEvent",
					// то пользователь хочет, чтобы мы поставили обработку на паузу -
					if(!ManualResetEvent.WaitAll(this.executeEvent, 0, false))
					{
						//...приостанавливаем работу контролируемого алгоритма...
						this.eRSRaidDecoder.Pause();

						//...и сами засыпаем
						ManualResetEvent.WaitAll(this.executeEvent);

						// А когда проснулись, указываем, что обработка должна продолжаться
						this.eRSRaidDecoder.Continue();
					}

					// Ждем любое из перечисленных событий...
					int eventIdx = ManualResetEvent.WaitAny(new ManualResetEvent[] {this.wakeUpEvent[0], this.exitEvent[0], this.eRSRaidDecoder.FinishedEvent[0]});

					//...если получили сигнал к тому, чтобы проснуться -
					// переходим на новую итерацию, т.к. просыпаемся
					// перед постановкой на паузу...
					if(eventIdx == 0)
					{
						//...предварительно сбросив событие, заставившее нас проснуться
						this.wakeUpEvent[0].Reset();

						continue;
					}

					//...если получили сигнал к выходу из обработки...
					if(eventIdx == 1)
					{
						//...останавливаем контролируемый алгоритм
						this.eRSRaidDecoder.Stop();

						// Указываем на то, что обработка была прервана
						this.processedOK = false;

						// Активируем индикатор актуального состояния переменных-членов
						this.finished = true;

						// Устанавливаем событие завершения обработки
						this.finishedEvent[0].Set();

						return;
					}

					//...если получили сигнал о завершении обработки вложенным алгоритмом...
					if(eventIdx == 2)
					{
						//...выходим из цикла ожидания завершения (этого и ждали в while(true)!)
						break;
					}
				} // while(true)
			}
			else
			{
				// Сбрасываем флаг корректности результата
				this.processedOK = false;

				// Активируем индикатор актуального состояния переменных-членов
				this.finished = true;

				return;
			}

			// Когда поток уже не работает, установленное им булевское свойство,
			// возможно, ещё "не проявилось"
			for(int i = 0; i < (int)WaitCount.MaxWaitCount; i++)
			{
				if(!this.eRSRaidDecoder.Finished)
				{
					Thread.Sleep((int)WaitTime.MinWaitTime);
				}
				else
				{
					break;
				}
			}

			// Выделяем память под массивы входных и выходных данных кодера
			int[] sourceLog = new int[this.dataCount];
			int[] target = new int[this.dataCount];

			// Буфер для работы с парами байт
			byte[] wordBuff = new byte[2];

			// Выделяем память под массивы файловых потоков
			BufferedStream[] fileStreamSourceArr = new BufferedStream[this.dataCount];
			BufferedStream[] fileStreamTargetArr = new BufferedStream[this.dataCount];

			// Текущий размер буфера (далее уточняется значением по-умолчанию, либо
			// производится автовыбор, исходя из доступного объема физической памяти)
			int currentTotalBufferSize = -1;

			try
			{
				// Определяем, какие из основных томов (по данным "volList") повреждены,
				// а какие - нет
				for(int i = 0; i < this.volList.Length; i++)
				{
					// Вычисляем номер текущего тома
					int currVol = Math.Abs(this.volList[i]);

					// Если данный том не является основным...
					if(currVol >= this.dataCount)
					{
						//...указываем, на данный факт
						damagedVolList[damagedVolCount++] = i;
					}
				}

				// Вычисляем значение модуля, который позволит выводить процент обработки
				// ровно при единичном приращении для цикла по "volCount"
				int progressMod1 = (this.dataCount + damagedVolCount) / 100;

				// Если модуль равен нулю, то увеличиваем его до значения "1", чтобы
				// прогресс выводился на каждой итерации (файл очень маленький)
				if(progressMod1 == 0)
				{
					progressMod1 = 1;
				}

				// Счетчик открытых файловых потоков
				int volCount = -1;

				// Имя файла для обработки
				String fileName;

				// Создаем экземпляр для получения системной информации
				SystemInfo eSystemInfo = new SystemInfo();

				// Уточняем максимальный размер буфера для файловых потоков...
				if(this.autoBuffering)
				{
					//...берем часть доступной физической памяти...
					currentTotalBufferSize = (int)(this.memConsumeCoeff * (double)eSystemInfo.FreePhysicalMemory);
					currentTotalBufferSize = (currentTotalBufferSize < 0) ? int.MaxValue : currentTotalBufferSize;
				}
				else
				{
					//...либо производим жесткий выбор значения
					currentTotalBufferSize = this.TotalBufferSize;
				}

				// Теперь важно уточнить, нужен ли такой большой буфер для работы с
				// данным набором томов. Для этого откроем любой из томов, и узнав его размер
				// вычислим общий требуемый объем на буфера

				// Считываем первоначальное имя файла,...
				fileName = this.fileName;

				//...упаковываем его в префиксный формат...
				this.eFileNamer.Pack(ref fileName, this.volList[0], this.dataCount, this.eccCount, this.codecType);

				//...формируем полное имя файла...
				fileName = this.path + fileName;

				//...а затем открываем файловый поток, чтобы узнать размер первого тома
				FileStream eFileStream = new FileStream(fileName, FileMode.Open, System.IO.FileAccess.Read);

				// Узнаем требуемый максимальный общий размер буфера
				long totalBufferSizeNeeded = eFileStream.Length * (this.dataCount + damagedVolCount);

				// Закрываем файловый поток
				eFileStream.Close();

				// Если требуется меньше, чем предполагалось, корректируем данную величину
				if(totalBufferSizeNeeded < currentTotalBufferSize)
				{
					currentTotalBufferSize = (int)totalBufferSizeNeeded;
				}

				// Вычисляем размер буфера на том
				int currentVolumeBufferSize = currentTotalBufferSize / (this.dataCount + damagedVolCount);

				// Открываем входные файловые потоки
				for(int i = 0; i < this.dataCount; i++)
				{
					// Считываем первоначальное имя файла,...
					fileName = this.fileName;

					//...упаковываем его в префиксный формат...
					this.eFileNamer.Pack(ref fileName, this.volList[i], this.dataCount, this.eccCount, this.codecType);

					//...формируем полное имя файла...
					fileName = this.path + fileName;

					//...производим тест на наличие файла...
					if(!File.Exists(fileName))
					{
						// Указываем на то, что произошла ошибка работы с файлами
						this.processedOK = false;

						// Активируем индикатор актуального состояния переменных-членов
						this.finished = true;

						// Устанавливаем событие завершения обработки
						this.finishedEvent[0].Set();

						return;
					}

					//...и открываем на его основе входной файловый поток
					fileStreamSourceArr[i] = new BufferedStream(new FileStream(fileName, FileMode.Open, System.IO.FileAccess.Read), currentVolumeBufferSize);

					// Если есть подписка на делегата обновления прогресса -...
					if(
						((++volCount % progressMod1) == 0)
						&&
						(OnUpdateFileStreamsOpeningProgress != null)
						)
					{
						//...выводим данные
						OnUpdateFileStreamsOpeningProgress(((double)(volCount + 1) / (double)(this.dataCount + damagedVolCount)) * 100);
					}

					// В случае, если требуется постановка на паузу, событие "executeEvent"
					// будет сброшено, и будем на паузе вплоть до его появления
					ManualResetEvent.WaitAll(this.executeEvent);

					// Если указано, что требуется выйти из потока - выходим
					if(ManualResetEvent.WaitAll(this.exitEvent, 0, false))
					{
						// Указываем, что обработка произведена некорректно
						this.processedOK = false;

						// Активируем индикатор актуального состояния переменных-членов
						this.finished = true;

						// Устанавливаем событие завершения обработки
						this.finishedEvent[0].Set();

						return;
					}
				}

				// Открываем выходные файловые потоки для поврежденных файлов
				for(int i = 0; i < damagedVolCount; i++)
				{
					// Считываем первоначальное имя файла,...
					fileName = this.fileName;

					//...упаковываем его в префиксный формат...
					this.eFileNamer.Pack(ref fileName, damagedVolList[i], this.dataCount, this.eccCount, this.codecType);

					//...формируем полное имя файла...
					fileName = this.path + fileName;

					// ...затем производим тест на наличие файла...
					if(File.Exists(fileName))
					{
						//...если таковой имеется, ставим на него атрибуты
						// по-умолчанию...
						File.SetAttributes(fileName, FileAttributes.Normal);
					}

					//...и открываем на его основе выходной файловый поток
					fileStreamTargetArr[damagedVolList[i]] = new BufferedStream(new FileStream(fileName, FileMode.Create, System.IO.FileAccess.Write), (currentTotalBufferSize / (this.dataCount + damagedVolCount)));

					// Если есть подписка на делегата обновления прогресса -...
					if(
						((++volCount % progressMod1) == 0)
						&&
						(OnUpdateFileStreamsOpeningProgress != null)
						)
					{
						//...выводим данные
						OnUpdateFileStreamsOpeningProgress(((double)(volCount + 1) / (double)(this.dataCount + damagedVolCount)) * 100);
					}

					// В случае, если требуется постановка на паузу, событие "executeEvent"
					// будет сброшено, и будем на паузе вплоть до его появления
					ManualResetEvent.WaitAll(this.executeEvent);

					// Если указано, что требуется выйти из потока - выходим
					if(ManualResetEvent.WaitAll(this.exitEvent, 0, false))
					{
						// Указываем, что обработка произведена некорректно
						this.processedOK = false;

						// Активируем индикатор актуального состояния переменных-членов
						this.finished = true;

						// Устанавливаем событие завершения обработки
						this.finishedEvent[0].Set();

						return;
					}
				}

				// Указываем теперь, что все файловые потоки открыты
				if(OnFileStreamsOpeningFinish != null)
				{
					OnFileStreamsOpeningFinish();
				}

				// Вычисляем значение модуля, который позволит выводить процент обработки
				// ровно при единичном приращении
				int progressMod2 = (int)(fileStreamSourceArr[0].Length / (2 * 100));

				// Если модуль равен нулю, то увеличиваем его до значения "1", чтобы
				// прогресс выводился на каждой итерации (файл очень маленький)
				if(progressMod2 == 0)
				{
					progressMod2 = 1;
				}

				// Указываем пользователю на то, что кодирование уже началось
				if(OnStartedRSCoding != null)
				{
					OnStartedRSCoding();
				}

				// Работаем со всеми срезами пар байт в исходных потоках
				for(int i = 0; i < ((fileStreamSourceArr[0].Length - 8) / 2); i++)
				{
					// Заполняем вектор исходных данных кодера данными текущего среза
					for(int j = 0; j < this.dataCount; j++)
					{
						// Читаем пару байт из входного потока
						int dataLen = 2;
						int readed = 0;
						int toRead = 0;
						while((toRead = dataLen - (readed += fileStreamSourceArr[j].Read(wordBuff, readed, toRead))) != 0) ;

						// Производим слияние двух значений byte в int
						sourceLog[j] = this.eGF16.Log((int)(((uint)(wordBuff[0] << 0) & 0x00FF)
						                                    |
						                                    ((uint)(wordBuff[1] << 8) & 0xFF00)));
					}

					// Декодируем данные (получаем полный корректный вектор основных томов)
					this.eRSRaidDecoder.Process(sourceLog, target);

					// Выводим уникальные элементы вектора выходных данных
					for(int j = 0; j < damagedVolCount; j++)
					{
						// Производим разделение одного значения на два (int16 на два byte)
						wordBuff[0] = (byte)((target[damagedVolList[j]] >> 0) & 0x00FF);
						wordBuff[1] = (byte)((target[damagedVolList[j]] >> 8) & 0x00FF);

						// Теперь пишем пару байт в выходной поток
						fileStreamTargetArr[damagedVolList[j]].Write(wordBuff, 0, 2);
					}

					// Выводим прогресс обработки через каждый процент
					if(
						(i != 0)
						&&
						((i % progressMod2) == 0)
						&&
						(OnUpdateFileCodingProgress != null)
						)
					{
						OnUpdateFileCodingProgress(((double)(i + 1) / (double)fileStreamSourceArr[0].Length) * 200.0);
					}

					// В случае, если требуется постановка на паузу, событие "executeEvent"
					// будет сброшено, и будем на паузе вплоть до его появления
					ManualResetEvent.WaitAll(this.executeEvent);

					// Если указано, что требуется выйти из потока - выходим
					if(ManualResetEvent.WaitAll(this.exitEvent, 0, false))
					{
						// Закрываем входные файловые потоки
						for(int j = 0; j < this.dataCount; j++)
						{
							if(fileStreamSourceArr[j] != null)
							{
								fileStreamSourceArr[j].Close();
								fileStreamSourceArr[j] = null;
							}
						}

						// Закрываем выходные файловые потоки
						for(int j = 0; j < this.eccCount; j++)
						{
							if(fileStreamTargetArr[j] != null)
							{
								fileStreamTargetArr[j].Close();
								fileStreamTargetArr[j] = null;
							}
						}

						// Указываем на то, что обработка была прервана
						this.processedOK = false;

						// Активируем индикатор актуального состояния переменных-членов
						this.finished = true;

						// Устанавливаем событие завершения обработки
						this.finishedEvent[0].Set();

						return;
					}
				}

				// Сообщаем, что обработка файлов закончена
				if(OnFileCodingFinish != null)
				{
					OnFileCodingFinish();
				}

				// Сбрасываем номер тома
				int volNum = -1;

				// Закрываем входные файловые потоки
				for(int i = 0; i < this.dataCount; i++)
				{
					if(fileStreamSourceArr[i] != null)
					{
						fileStreamSourceArr[i].Close();
						fileStreamSourceArr[i] = null;
					}

					// Если есть подписка на делегата обновления прогресса -...
					if(
						((++volNum % progressMod1) == 0)
						&&
						(OnUpdateFileStreamsClosingProgress != null)
						)
					{
						//...выводим данные
						OnUpdateFileStreamsClosingProgress(((double)(volNum + 1) / (double)(this.dataCount + this.eccCount)) * 100);
					}
				}

				// Закрываем выходные файловые потоки так:
				for(int i = 0; i < damagedVolCount; i++)
				{
					// Сначала пишем фиктивные 8 байт вместо реальной CRC-64,
					// а, затем, закрываем файл.
					if(fileStreamTargetArr[damagedVolList[i]] != null)
					{
						fileStreamTargetArr[damagedVolList[i]].Write(new byte[8], 0, 8);
						fileStreamTargetArr[damagedVolList[i]].Flush();
						fileStreamTargetArr[damagedVolList[i]].Close();
						fileStreamTargetArr[damagedVolList[i]] = null;
					}

					// Если есть подписка на делегата обновления прогресса -...
					if(
						((++volNum % progressMod1) == 0)
						&&
						(OnUpdateFileStreamsClosingProgress != null)
						)
					{
						//...выводим данные
						OnUpdateFileStreamsClosingProgress(((double)(volNum + 1) / (double)(this.dataCount + this.eccCount)) * 100);
					}
				}

				// Сообщаем, что закрытие файловых потоков закончилось
				if(OnFileStreamsClosingFinish != null)
				{
					OnFileStreamsClosingFinish();
				}
			}

			// Если было хотя бы одно исключение - закрываем файловый поток и
			// сообщаем об ошибке
			catch
			{
				// Закрываем входные файловые потоки
				for(int i = 0; i < this.dataCount; i++)
				{
					if(fileStreamSourceArr[i] != null)
					{
						fileStreamSourceArr[i].Close();
						fileStreamSourceArr[i] = null;
					}
				}

				// Закрываем выходные файловые потоки
				for(int i = 0; i < damagedVolCount; i++)
				{
					if(fileStreamTargetArr[damagedVolList[i]] != null)
					{
						fileStreamTargetArr[damagedVolList[i]].Flush();
						fileStreamTargetArr[damagedVolList[i]].Close();
						fileStreamTargetArr[damagedVolList[i]] = null;
					}
				}

				// Указываем на то, что произошла ошибка работы с файлами
				this.processedOK = false;

				// Активируем индикатор актуального состояния переменных-членов
				this.finished = true;

				// Устанавливаем событие завершения обработки
				this.finishedEvent[0].Set();

				return;
			}

			// Указываем на то, что обработка была произведена корректно
			this.processedOK = true;

			// Активируем индикатор актуального состояния переменных-членов
			this.finished = true;

			// Устанавливаем событие завершения обработки
			this.finishedEvent[0].Set();
		}

		#endregion Private Operations
	}
}