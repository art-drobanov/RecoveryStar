/*----------------------------------------------------------------------+
 |  filename:   RecoveryStarCore.cs                                     |
 |----------------------------------------------------------------------|
 |  version:    2.20                                                    |
 |  revision:   23.05.2012 17:33                                        |
 |  authors:    Дробанов Артём Федорович (DrAF),                        |
 |              RUSpectrum (г. Оренбург).                               |
 |  e-mail:     draf@mail.ru                                            |
 |  purpose:    Отказоустойчивое кодирование по типу RAID-систем        |
 +----------------------------------------------------------------------*/

using System;
using System.Threading;
using System.IO;

namespace RecoveryStar
{
	/// <summary>
	/// Класс для кодирования файлов в RAID-подобном формате
	/// </summary>
	public class RecoveryStarCore
	{
		#region Delegates

		/// <summary>
		/// Делегат обновления прогресса разбиения (склеивания) файла
		/// </summary>
		public OnUpdateDoubleValueHandler OnUpdateFileSplittingProgress;

		/// <summary>
		/// Делегат завершения процесса разбиения (склеивания) файла
		/// </summary>
		public OnEventHandler OnFileSplittingFinish;

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

		/// <summary>
		/// Делегат обновления процесса контроля целостности файлов
		/// </summary>
		public OnUpdateDoubleValueHandler OnUpdateFileAnalyzeProgress;

		/// <summary>
		/// Делегат завершения процесса контроля целостности файлов
		/// </summary>
		public OnEventHandler OnFileAnalyzeFinish;

		/// <summary>
		/// Делегат получения статистики повреждений многотомного архива
		/// </summary>
		public OnUpdateTwoIntDoubleValueHandler OnGetDamageStat;

		#endregion Delegates

		#region Public Properties & Data

		/// <summary>
		/// Булевское свойство "Файл обрабатывается?"
		/// </summary>
		public bool InProcessing
		{
			get
			{
				if(
					(this.thrRecoveryStarCore != null)
					&&
					(
						(this.thrRecoveryStarCore.ThreadState == ThreadState.Running)
						||
						(this.thrRecoveryStarCore.ThreadState == ThreadState.WaitSleepJoin)
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
		/// Обработка произведена корректно?
		/// </summary>
		private bool processedOK;

		/// <summary>
		/// Криптографическая защита данных
		/// </summary>
		public Security Security
		{
			get
			{
				if(this.eFileSplitter != null)
				{
					return this.eFileSplitter.Security;
				}
				else
				{
					return null;
				}
			}

			set
			{
				if(this.eFileSplitter != null)
				{
					this.eFileSplitter.Security = value;
				}
			}
		}

		/// <summary>
		/// Размер CBC-блока (Кб), используемый при шифровании
		/// </summary>
		public int CBCBlockSize
		{
			get
			{
				if(this.eFileSplitter != null)
				{
					return this.eFileSplitter.CBCBlockSize;
				}
				else
				{
					return -1;
				}
			}

			set
			{
				if(this.eFileSplitter != null)
				{
					this.eFileSplitter.CBCBlockSize = value;
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
					(this.thrRecoveryStarCore != null)
					&&
					(this.thrRecoveryStarCore.IsAlive)
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
					this.thrRecoveryStarCore.Priority = this.threadPriority;

					// Дублируем установку параметра для подконтрольных объектов
					if(this.eFileAnalyzer != null)
					{
						this.eFileAnalyzer.ThreadPriority = value;
					}

					if(this.eFileCodec != null)
					{
						this.eFileCodec.ThreadPriority = value;
					}

					if(this.eFileSplitter != null)
					{
						this.eFileSplitter.ThreadPriority = value;
					}
				}
			}
		}

		/// <summary>
		/// Приоритет процесса обработки файла
		/// </summary>
		private ThreadPriority threadPriority;

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
		private ManualResetEvent[] finishedEvent;

		#endregion Public Properties & Data

		#region Data

		/// <summary>
		/// Модуль для упаковки (распаковки) имени файла в префиксный формат
		/// </summary>
		private FileNamer eFileNamer;

		/// <summary>
		/// Модуль вычисления и контроля сигнатуры целостности файла CRC-64
		/// </summary>
		private FileAnalyzer eFileAnalyzer;

		/// <summary>
		/// RAID-подобный файловый кодер
		/// </summary>
		private FileCodec eFileCodec;

		/// <summary>
		/// Модуль разбиения (склеивания) файлов на тома
		/// </summary>
		private FileSplitter eFileSplitter;

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
		/// Используется быстрое извлечение из томов (без проверки CRC-64)?
		/// </summary>
		private bool fastExtraction;

		/// <summary>
		/// Путь к файлам для обработки
		/// </summary>
		private String path;

		/// <summary>
		/// Имя исходного файла для обработки
		/// </summary>
		private String fileName;

		/// <summary>
		/// Экземляр класса полностью закончил обработку?
		/// </summary>
		private bool finished;

		/// <summary>
		/// Поток кодирования данных
		/// </summary>
		private Thread thrRecoveryStarCore;

		/// <summary>
		/// Событие прекращения обработки файла
		/// </summary>
		private ManualResetEvent[] exitEvent;

		/// <summary>
		/// Событие продолжения обработки файла
		/// </summary>
		private ManualResetEvent[] executeEvent;

		/// <summary>
		/// Событие "пробуждения" цикла ожидания
		/// </summary>
		private ManualResetEvent[] wakeUpEvent;

		#endregion Data

		#region Construction & Destruction

		/// <summary>
		/// Конструктор класса
		/// </summary>
		public RecoveryStarCore()
		{
			// Модуль для упаковки (распаковки) имени файла в префиксный формат
			this.eFileNamer = new FileNamer();

			// Модуль вычисления и контроля сигнатуры целостности файла CRC-64
			this.eFileAnalyzer = new FileAnalyzer();

			// RAID-подобный файловый кодер
			this.eFileCodec = new FileCodec();

			// Модуль разбиения (склеивания) файлов на тома
			this.eFileSplitter = new FileSplitter();

			// Экземляр класса полностью закончил обработку?
			this.finished = true;

			// Обработка произведена корректно?
			this.processedOK = false;

			// По-умолчанию устанавливается фоновый приоритет
			this.threadPriority = 0;

			// Инициализируем событие прекращения обработки файла
			this.exitEvent = new ManualResetEvent[] {new ManualResetEvent(false)};

			// Инициализируем cобытие продолжения обработки файла
			this.executeEvent = new ManualResetEvent[] {new ManualResetEvent(false)};

			// Инициализируем cобытие "пробуждения" цикла ожидания
			this.wakeUpEvent = new ManualResetEvent[] {new ManualResetEvent(false)};

			// Событие, устанавливаемое по завершении обработки
			this.finishedEvent = new ManualResetEvent[] {new ManualResetEvent(true)};
		}

		#endregion Construction & Destruction

		#region Public Operations

		/// <summary>
		/// Отказоустойчивое кодирование файла по типу RAID
		/// </summary>
		/// <param name="fullFileName">Полное имя файла для отказоустойчивого кодирования</param>
		/// <param name="dataCount">Количество основных томов</param>
		/// <param name="eccCount">Количество томов для восстановления</param>
		/// <param name="codecType">Тип кодека Рида-Соломона (по типу матрицы)</param>
		/// <param name="runAsSeparateThread">Запускать в отдельном потоке?</param>
		/// <returns>Булевский флаг операции</returns>
		public bool StartToProtect(String fullFileName, int dataCount, int eccCount, int codecType,
		                           bool runAsSeparateThread)
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

			// Если имя файла не установлено
			if(
				(fullFileName == null)
				||
				(fullFileName == "")
				)
			{
				// Активируем индикатор актуального состояния переменных-членов
				this.finished = true;

				// Устанавливаем событие завершения обработки
				this.finishedEvent[0].Set();

				return false;
			}

			// Производим выделение пути из полного имени файла
			this.path = this.eFileNamer.GetPath(fullFileName);

			// Производим выделение имени из полного имени файла
			this.fileName = this.eFileNamer.GetShortFileName(fullFileName);

			// Если исходный файл не существует, сообщаем об ошибке
			if(!File.Exists(this.path + this.fileName))
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

			// Подписываемся на делегатов
			this.eFileSplitter.OnUpdateFileSplittingProgress = OnUpdateFileSplittingProgress;
			this.eFileSplitter.OnFileSplittingFinish = OnFileSplittingFinish;

			this.eFileCodec.OnUpdateRSMatrixFormingProgress = OnUpdateRSMatrixFormingProgress;
			this.eFileCodec.OnRSMatrixFormingFinish = OnRSMatrixFormingFinish;
			this.eFileCodec.OnUpdateFileStreamsOpeningProgress = OnUpdateFileStreamsOpeningProgress;
			this.eFileCodec.OnFileStreamsOpeningFinish = OnFileStreamsOpeningFinish;
			this.eFileCodec.OnStartedRSCoding = OnStartedRSCoding;
			this.eFileCodec.OnUpdateFileCodingProgress = OnUpdateFileCodingProgress;
			this.eFileCodec.OnFileCodingFinish = OnFileCodingFinish;
			this.eFileCodec.OnUpdateFileStreamsClosingProgress = OnUpdateFileStreamsClosingProgress;
			this.eFileCodec.OnFileStreamsClosingFinish = OnFileStreamsClosingFinish;

			this.eFileAnalyzer.OnUpdateFileAnalyzeProgress = OnUpdateFileAnalyzeProgress;
			this.eFileAnalyzer.OnFileAnalyzeFinish = OnFileAnalyzeFinish;

			// Указываем, что поток должен выполняться
			this.exitEvent[0].Reset();
			this.executeEvent[0].Set();
			this.wakeUpEvent[0].Reset();
			this.finishedEvent[0].Reset();

			// Если указано, что не требуется запуск в отдельном потоке,
			// запускаем в данном
			if(!runAsSeparateThread)
			{
				// Защищаем файл от повреждений (кодируем его)
				Protect();

				// Возвращаем результат обработки
				return this.processedOK;
			}

			// Создаем поток кодирования файлов...
			this.thrRecoveryStarCore = new Thread(new ThreadStart(Protect));

			//...затем даем ему имя...
			this.thrRecoveryStarCore.Name = "RecoveryStarCore.Protect()";

			//...устанавливаем выбранный приоритет задачи...
			this.thrRecoveryStarCore.Priority = this.threadPriority;

			//...и запускаем его
			this.thrRecoveryStarCore.Start();

			// Сообщаем, что все нормально
			return true;
		}

		/// <summary>
		/// Отказоустойчивое декодирование файла
		/// </summary>
		/// <param name="fullFileName">Полное имя файла для восстановления</param>
		/// <param name="fastExtraction">Используется быстрое извлечение из томов (без проверки CRC-64)?</param>
		/// <param name="runAsSeparateThread">Запускать в отдельном потоке?</param>
		/// <returns>Булевский флаг операции</returns>
		public bool StartToRecover(String fullFileName, bool fastExtraction, bool runAsSeparateThread)
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

			// Если имя файла не установлено
			if(
				(fullFileName == null)
				||
				(fullFileName == "")
				)
			{
				// Активируем индикатор актуального состояния переменных-членов
				this.finished = true;

				// Устанавливаем событие завершения обработки
				this.finishedEvent[0].Set();

				return false;
			}

			// Производим выделение пути из полного имени файла
			this.path = this.eFileNamer.GetPath(fullFileName);

			// Производим выделение имени из полного имени файла
			this.fileName = this.eFileNamer.GetShortFileName(fullFileName);

			// Если исходный файл не существует, сообщаем об ошибке
			if(!File.Exists(this.path + this.fileName))
			{
				// Активируем индикатор актуального состояния переменных-членов
				this.finished = true;

				// Устанавливаем событие завершения обработки
				this.finishedEvent[0].Set();

				return false;
			}

			// Распаковываем исходное имя файла из префиксного формата,
			// и в результате получаем параметры "fileName", "dataCount", "eccCount", "codecType"
			if(!this.eFileNamer.Unpack(ref this.fileName, ref this.dataCount, ref this.eccCount, ref this.codecType))
			{
				return false;
			}

			// Используется быстрое извлечение из томов (без проверки CRC-64)?
			this.fastExtraction = fastExtraction;

			// Подписываемся на делегатов
			this.eFileSplitter.OnUpdateFileSplittingProgress = OnUpdateFileSplittingProgress;
			this.eFileSplitter.OnFileSplittingFinish = OnFileSplittingFinish;

			this.eFileCodec.OnUpdateRSMatrixFormingProgress = OnUpdateRSMatrixFormingProgress;
			this.eFileCodec.OnRSMatrixFormingFinish = OnRSMatrixFormingFinish;
			this.eFileCodec.OnUpdateFileStreamsOpeningProgress = OnUpdateFileStreamsOpeningProgress;
			this.eFileCodec.OnFileStreamsOpeningFinish = OnFileStreamsOpeningFinish;
			this.eFileCodec.OnStartedRSCoding = OnStartedRSCoding;
			this.eFileCodec.OnUpdateFileCodingProgress = OnUpdateFileCodingProgress;
			this.eFileCodec.OnFileCodingFinish = OnFileCodingFinish;
			this.eFileCodec.OnUpdateFileStreamsClosingProgress = OnUpdateFileStreamsClosingProgress;
			this.eFileCodec.OnFileStreamsClosingFinish = OnFileStreamsClosingFinish;

			this.eFileAnalyzer.OnUpdateFileAnalyzeProgress = OnUpdateFileAnalyzeProgress;
			this.eFileAnalyzer.OnFileAnalyzeFinish = OnFileAnalyzeFinish;
			this.eFileAnalyzer.OnGetDamageStat = OnGetDamageStat;

			// Указываем, что поток должен выполняться
			this.exitEvent[0].Reset();
			this.executeEvent[0].Set();
			this.wakeUpEvent[0].Reset();
			this.finishedEvent[0].Reset();

			// Если указано, что не требуется запуск в отдельном потоке,
			// запускаем в данном
			if(!runAsSeparateThread)
			{
				// Восстанавливаем файл из многотомного архива с коррекцией ошибок
				Recover();

				// Возвращаем результат обработки
				return this.processedOK;
			}

			// Создаем поток восстановления файлов...
			this.thrRecoveryStarCore = new Thread(new ThreadStart(Recover));

			//...затем даем ему имя...
			this.thrRecoveryStarCore.Name = "RecoveryStarCore.Recover()";

			//...устанавливаем выбранный приоритет задачи...
			this.thrRecoveryStarCore.Priority = this.threadPriority;

			//...и запускаем его
			this.thrRecoveryStarCore.Start();

			// Сообщаем, что все нормально
			return true;
		}

		/// <summary>
		/// Восстановление отказоустойчивого набора данных
		/// </summary>
		/// <param name="fullFileName">Полное имя файла для восстановления</param>
		/// <param name="fastExtraction">Используется быстрое извлечение из томов (без проверки CRC-64)?</param>
		/// <param name="runAsSeparateThread">Запускать в отдельном потоке?</param>
		/// <returns>Булевский флаг операции</returns>
		public bool StartToRepair(String fullFileName, bool fastExtraction, bool runAsSeparateThread)
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

			// Если имя файла не установлено
			if(
				(fullFileName == null)
				||
				(fullFileName == "")
				)
			{
				// Активируем индикатор актуального состояния переменных-членов
				this.finished = true;

				// Устанавливаем событие завершения обработки
				this.finishedEvent[0].Set();

				return false;
			}

			// Производим выделение пути из полного имени файла
			this.path = this.eFileNamer.GetPath(fullFileName);

			// Производим выделение имени из полного имени файла
			this.fileName = this.eFileNamer.GetShortFileName(fullFileName);

			// Если исходный файл не существует, сообщаем об ошибке
			if(!File.Exists(this.path + this.fileName))
			{
				// Активируем индикатор актуального состояния переменных-членов
				this.finished = true;

				// Устанавливаем событие завершения обработки
				this.finishedEvent[0].Set();

				return false;
			}

			// Распаковываем исходное имя файла из префиксного формата,
			// и в результате получаем параметры "fileName", "dataCount", "eccCount", "codecType"
			if(!this.eFileNamer.Unpack(ref this.fileName, ref this.dataCount, ref this.eccCount, ref this.codecType))
			{
				return false;
			}

			// Используется быстрое извлечение из томов (без проверки CRC-64)?
			this.fastExtraction = fastExtraction;

			// Подписываемся на делегатов
			this.eFileCodec.OnUpdateRSMatrixFormingProgress = OnUpdateRSMatrixFormingProgress;
			this.eFileCodec.OnRSMatrixFormingFinish = OnRSMatrixFormingFinish;
			this.eFileCodec.OnUpdateFileStreamsOpeningProgress = OnUpdateFileStreamsOpeningProgress;
			this.eFileCodec.OnFileStreamsOpeningFinish = OnFileStreamsOpeningFinish;
			this.eFileCodec.OnStartedRSCoding = OnStartedRSCoding;
			this.eFileCodec.OnUpdateFileCodingProgress = OnUpdateFileCodingProgress;
			this.eFileCodec.OnFileCodingFinish = OnFileCodingFinish;
			this.eFileCodec.OnUpdateFileStreamsClosingProgress = OnUpdateFileStreamsClosingProgress;
			this.eFileCodec.OnFileStreamsClosingFinish = OnFileStreamsClosingFinish;

			this.eFileAnalyzer.OnUpdateFileAnalyzeProgress = OnUpdateFileAnalyzeProgress;
			this.eFileAnalyzer.OnFileAnalyzeFinish = OnFileAnalyzeFinish;
			this.eFileAnalyzer.OnGetDamageStat = OnGetDamageStat;

			// Указываем, что поток должен выполняться
			this.exitEvent[0].Reset();
			this.executeEvent[0].Set();
			this.wakeUpEvent[0].Reset();
			this.finishedEvent[0].Reset();

			// Если указано, что не требуется запуск в отдельном потоке,
			// запускаем в данном
			if(!runAsSeparateThread)
			{
				// Восстанавливаем файл из многотомного архива с коррекцией ошибок
				Repair();

				// Возвращаем результат обработки
				return this.processedOK;
			}

			// Создаем поток восстановления файлов...
			this.thrRecoveryStarCore = new Thread(new ThreadStart(Repair));

			//...затем даем ему имя...
			this.thrRecoveryStarCore.Name = "RecoveryStarCore.Repair()";

			//...устанавливаем выбранный приоритет задачи...
			this.thrRecoveryStarCore.Priority = this.threadPriority;

			//...и запускаем его
			this.thrRecoveryStarCore.Start();

			// Сообщаем, что все нормально
			return true;
		}

		/// <summary>
		/// Тестирование отказоустойчивого набора данных
		/// </summary>
		/// <param name="fullFileName">Полное имя файла для тестирования</param>
		/// <param name="fastExtraction">Используется быстрое извлечение из томов (без проверки CRC-64)?</param>
		/// <param name="runAsSeparateThread">Запускать в отдельном потоке?</param>
		/// <returns>Булевский флаг операции</returns>
		public bool StartToTest(String fullFileName, bool fastExtraction, bool runAsSeparateThread)
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

			// Если имя файла не установлено
			if(
				(fullFileName == null)
				||
				(fullFileName == "")
				)
			{
				// Активируем индикатор актуального состояния переменных-членов
				this.finished = true;

				// Устанавливаем событие завершения обработки
				this.finishedEvent[0].Set();

				return false;
			}

			// Производим выделение пути из полного имени файла
			this.path = this.eFileNamer.GetPath(fullFileName);

			// Производим выделение имени из полного имени файла
			this.fileName = this.eFileNamer.GetShortFileName(fullFileName);

			// Если исходный файл не существует, сообщаем об ошибке
			if(!File.Exists(this.path + this.fileName))
			{
				// Активируем индикатор актуального состояния переменных-членов
				this.finished = true;

				// Устанавливаем событие завершения обработки
				this.finishedEvent[0].Set();

				return false;
			}

			// Распаковываем исходное имя файла из префиксного формата,
			// и в результате получаем параметры "fileName", "dataCount", "eccCount", "codecType"
			if(!this.eFileNamer.Unpack(ref this.fileName, ref this.dataCount, ref this.eccCount, ref this.codecType))
			{
				return false;
			}

			// Используется быстрое извлечение из томов (без проверки CRC-64)?
			this.fastExtraction = fastExtraction;

			// Подписываемся на делегатов
			this.eFileAnalyzer.OnUpdateFileAnalyzeProgress = OnUpdateFileAnalyzeProgress;
			this.eFileAnalyzer.OnFileAnalyzeFinish = OnFileAnalyzeFinish;
			this.eFileAnalyzer.OnGetDamageStat = OnGetDamageStat;

			// Указываем, что поток должен выполняться
			this.exitEvent[0].Reset();
			this.executeEvent[0].Set();
			this.wakeUpEvent[0].Reset();
			this.finishedEvent[0].Reset();

			// Если указано, что не требуется запуск в отдельном потоке,
			// запускаем в данном
			if(!runAsSeparateThread)
			{
				// Восстанавливаем файл из многотомного архива с коррекцией ошибок
				Test();

				// Возвращаем результат обработки
				return this.processedOK;
			}

			// Создаем поток восстановления файлов...
			this.thrRecoveryStarCore = new Thread(new ThreadStart(Test));

			//...затем даем ему имя...
			this.thrRecoveryStarCore.Name = "RecoveryStarCore.Test()";

			//...устанавливаем выбранный приоритет задачи...
			this.thrRecoveryStarCore.Priority = this.threadPriority;

			//...и запускаем его
			this.thrRecoveryStarCore.Start();

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
		/// Отказоустойчивое кодирование файла по типу RAID
		/// </summary>
		private void Protect()
		{
			// Разбиваем исходный файл на фрагменты
			if(this.eFileSplitter.StartToSplit(this.path, this.fileName, this.dataCount, this.eccCount, this.codecType, true))
			{
				// Цикл ожидания завершения этапа разбиения исходного файла на тома
				while(true)
				{
					// Если не обнаружили установленного события "executeEvent",
					// то пользователь хочет, чтобы мы поставили обработку на паузу -
					if(!ManualResetEvent.WaitAll(this.executeEvent, 0, false))
					{
						//...приостанавливаем работу контролируемого алгоритма...
						this.eFileSplitter.Pause();

						//...и сами засыпаем
						ManualResetEvent.WaitAll(this.executeEvent);

						// А когда проснулись, указываем, что обработка должна продолжаться
						this.eFileSplitter.Continue();
					}

					// Ждем любое из перечисленных событий...
					int eventIdx = ManualResetEvent.WaitAny(new ManualResetEvent[] {this.wakeUpEvent[0], this.exitEvent[0], this.eFileSplitter.FinishedEvent[0]});

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
						///...останавливаем контролируемый алгоритм
						this.eFileSplitter.Stop();

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

			// В связи с закрытием большого количества файловых потоков
			// необходимо дождаться записи изменений, внесенных потоком
			// кодирования в тело класса. Поток уже не работает, но
			// установленное им булевское свойство, возможно, ещё
			// "не проявилось"
			for(int i = 0; i < (int)WaitCount.MaxWaitCount; i++)
			{
				if(!this.eFileSplitter.Finished)
				{
					Thread.Sleep((int)WaitTime.MinWaitTime);
				}
				else
				{
					break;
				}
			}

			// Если циклы ожидания закрытия файловых потоков не привели к желаемому
			// результату - это ошибка
			if(!this.eFileSplitter.ProcessedOK)
			{
				// Указываем на то, что обработка была прервана
				this.processedOK = false;

				// Активируем индикатор актуального состояния переменных-членов
				this.finished = true;

				// Устанавливаем событие завершения обработки
				this.finishedEvent[0].Set();

				return;
			}

			// Создаем тома для восстановления
			if(this.eFileCodec.StartToEncode(this.path, this.fileName, this.dataCount, this.eccCount, this.codecType, true))
			{
				// Цикл ожидания завершения этапа кодирования томов
				while(true)
				{
					// Если не обнаружили установленного события "executeEvent",
					// то пользователь хочет, чтобы мы поставили обработку на паузу -
					if(!ManualResetEvent.WaitAll(this.executeEvent, 0, false))
					{
						//...приостанавливаем работу контролируемого алгоритма...
						this.eFileCodec.Pause();

						//...и сами засыпаем
						ManualResetEvent.WaitAll(this.executeEvent);

						// А когда проснулись, указываем, что обработка должна продолжаться
						this.eFileCodec.Continue();
					}

					// Ждем любое из перечисленных событий...
					int eventIdx = ManualResetEvent.WaitAny(new ManualResetEvent[] {this.wakeUpEvent[0], this.exitEvent[0], this.eFileCodec.FinishedEvent[0]});

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
						///...останавливаем контролируемый алгоритм
						this.eFileCodec.Stop();

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

			// В связи с закрытием большого количества файловых потоков
			// необходимо дождаться записи изменений, внесенных потоком
			// кодирования в тело класса. Поток уже не работает, но
			// установленное им булевское свойство, возможно, ещё
			// "не проявилось"
			for(int i = 0; i < (int)WaitCount.MaxWaitCount; i++)
			{
				if(!this.eFileCodec.Finished)
				{
					Thread.Sleep((int)WaitTime.MinWaitTime);
				}
				else
				{
					break;
				}
			}

			// Если циклы ожидания закрытия файловых потоков не привели к желаемому
			// результату - это ошибка
			if(!this.eFileCodec.ProcessedOK)
			{
				// Указываем на то, что обработка была прервана
				this.processedOK = false;

				// Активируем индикатор актуального состояния переменных-членов
				this.finished = true;

				// Устанавливаем событие завершения обработки
				this.finishedEvent[0].Set();

				return;
			}

			// Осуществляем вычисление сигнатур целостности CRC-64 для всего набора томов
			if(this.eFileAnalyzer.StartToWriteCRC64(this.path, this.fileName, this.dataCount, this.eccCount, this.codecType, true))
			{
				// Цикл ожидания завершения процесса расчета сигнатур целостности томов
				while(true)
				{
					// Если не обнаружили установленного события "executeEvent",
					// то пользователь хочет, чтобы мы поставили обработку на паузу -
					if(!ManualResetEvent.WaitAll(this.executeEvent, 0, false))
					{
						//...приостанавливаем работу контролируемого алгоритма...
						this.eFileAnalyzer.Pause();

						//...и сами засыпаем
						ManualResetEvent.WaitAll(this.executeEvent);

						// А когда проснулись, указываем, что обработка должна продолжаться
						this.eFileAnalyzer.Continue();
					}

					// Ждем любое из перечисленных событий...
					int eventIdx = ManualResetEvent.WaitAny(new ManualResetEvent[] {this.wakeUpEvent[0], this.exitEvent[0], this.eFileAnalyzer.FinishedEvent[0]});

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
						///...останавливаем контролируемый алгоритм
						this.eFileAnalyzer.Stop();

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

			// В связи с закрытием большого количества файловых потоков
			// необходимо дождаться записи изменений, внесенных потоком
			// кодирования в тело класса. Поток уже не работает, но
			// установленное им булевское свойство, возможно, ещё
			// "не проявилось"
			for(int i = 0; i < (int)WaitCount.MaxWaitCount; i++)
			{
				if(!this.eFileAnalyzer.Finished)
				{
					Thread.Sleep((int)WaitTime.MinWaitTime);
				}
				else
				{
					break;
				}
			}

			// Если циклы ожидания закрытия файловых потоков не привели к желаемому
			// результату - это ошибка
			if(!this.eFileAnalyzer.ProcessedOK)
			{
				// Указываем на то, что обработка была прервана
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
		private void Recover()
		{
			// Список томов, используемых для восстановления
			int[] volList;

			// Осуществляем проверку сигнатур целостности CRC-64 для всего набора томов
			if(this.eFileAnalyzer.StartToAnalyzeCRC64(this.path, this.fileName, this.dataCount, this.eccCount, this.codecType, this.fastExtraction, true))
			{
				// Цикл ожидания завершения процесса расчета сигнатур целостности томов
				while(true)
				{
					// Если не обнаружили установленного события "executeEvent",
					// то пользователь хочет, чтобы мы поставили обработку на паузу -
					if(!ManualResetEvent.WaitAll(this.executeEvent, 0, false))
					{
						//...приостанавливаем работу контролируемого алгоритма...
						this.eFileAnalyzer.Pause();

						//...и сами засыпаем
						ManualResetEvent.WaitAll(this.executeEvent);

						// А когда проснулись, указываем, что обработка должна продолжаться
						this.eFileAnalyzer.Continue();
					}

					// Ждем любое из перечисленных событий...
					int eventIdx = ManualResetEvent.WaitAny(new ManualResetEvent[] {this.wakeUpEvent[0], this.exitEvent[0], this.eFileAnalyzer.FinishedEvent[0]});

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
						///...останавливаем контролируемый алгоритм
						this.eFileAnalyzer.Stop();

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

			// В связи с закрытием большого количества файловых потоков
			// необходимо дождаться записи изменений, внесенных потоком
			// кодирования в тело класса. Поток уже не работает, но
			// установленное им булевское свойство, возможно, ещё
			// "не проявилось"
			for(int i = 0; i < (int)WaitCount.MaxWaitCount; i++)
			{
				if(!this.eFileAnalyzer.Finished)
				{
					Thread.Sleep((int)WaitTime.MinWaitTime);
				}
				else
				{
					break;
				}
			}

			// Если циклы ожидания закрытия файловых потоков не привели к желаемому
			// результату - это ошибка
			if(!this.eFileAnalyzer.ProcessedOK)
			{
				// Указываем на то, что обработка была прервана
				this.processedOK = false;

				// Активируем индикатор актуального состояния переменных-членов
				this.finished = true;

				// Устанавливаем событие завершения обработки
				this.finishedEvent[0].Set();

				return;
			}

			// Теперь, когда обработка завершена, необходимо проанализировать
			// полученный вектор "volList"
			volList = this.eFileAnalyzer.VolList;

			// Изначально предполагаем, что восстановление данных не потребуется
			bool needToRecover = false;

			// Проверяем вектор на наличие в нем томов для восстановления
			for(int dataNum = 0; dataNum < this.dataCount; ++dataNum)
			{
				// Если встретился том для восстановления, часть
				// основных томов повреждена и требуется применение "FileCodec"
				if(volList[dataNum] != dataNum)
				{
					needToRecover = true;

					break;
				}
			}

			// Если требуется восстановление основных томов, запускаем его
			if(needToRecover)
			{
				// Восстанавливаем утерянные основные тома
				if(this.eFileCodec.StartToDecode(this.path, this.fileName, this.dataCount, this.eccCount, volList, this.codecType, true))
				{
					// Цикл ожидания завершения этапа декодирования томов
					while(true)
					{
						// Если не обнаружили установленного события "executeEvent",
						// то пользователь хочет, чтобы мы поставили обработку на паузу -
						if(!ManualResetEvent.WaitAll(this.executeEvent, 0, false))
						{
							//...приостанавливаем работу контролируемого алгоритма...
							this.eFileCodec.Pause();

							//...и сами засыпаем
							ManualResetEvent.WaitAll(this.executeEvent);

							// А когда проснулись, указываем, что обработка должна продолжаться
							this.eFileCodec.Continue();
						}

						// Ждем любое из перечисленных событий...
						int eventIdx = ManualResetEvent.WaitAny(new ManualResetEvent[] {this.wakeUpEvent[0], this.exitEvent[0], this.eFileCodec.FinishedEvent[0]});

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
							///...останавливаем контролируемый алгоритм
							this.eFileCodec.Stop();

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

				// В связи с закрытием большого количества файловых потоков
				// необходимо дождаться записи изменений, внесенных потоком
				// кодирования в тело класса. Поток уже не работает, но
				// установленное им булевское свойство, возможно, ещё
				// "не проявилось"
				for(int i = 0; i < (int)WaitCount.MaxWaitCount; i++)
				{
					if(!this.eFileCodec.Finished)
					{
						Thread.Sleep((int)WaitTime.MinWaitTime);
					}
					else
					{
						break;
					}
				}

				// Если циклы ожидания закрытия файловых потоков не привели к желаемому
				// результату - это ошибка
				if(!this.eFileCodec.ProcessedOK)
				{
					// Указываем на то, что обработка была прервана
					this.processedOK = false;

					// Активируем индикатор актуального состояния переменных-членов
					this.finished = true;

					// Устанавливаем событие завершения обработки
					this.finishedEvent[0].Set();

					return;
				}
			}

			// Склеиваем исходный файл из восстановленных основных томов
			if(this.eFileSplitter.StartToGlue(this.path, this.fileName, this.dataCount, this.eccCount, this.codecType, true))
			{
				// Цикл ожидания завершения этапа склеивания исходного файла из томов
				while(true)
				{
					// Если не обнаружили установленного события "executeEvent",
					// то пользователь хочет, чтобы мы поставили обработку на паузу -
					if(!ManualResetEvent.WaitAll(this.executeEvent, 0, false))
					{
						//...приостанавливаем работу контролируемого алгоритма...
						this.eFileSplitter.Pause();

						//...и сами засыпаем
						ManualResetEvent.WaitAll(this.executeEvent);

						// А когда проснулись, указываем, что обработка должна продолжаться
						this.eFileSplitter.Continue();
					}

					// Ждем любое из перечисленных событий...
					int eventIdx = ManualResetEvent.WaitAny(new ManualResetEvent[] {this.wakeUpEvent[0], this.exitEvent[0], this.eFileSplitter.FinishedEvent[0]});

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
						///...останавливаем контролируемый алгоритм
						this.eFileSplitter.Stop();

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

			// В связи с закрытием большого количества файловых потоков
			// необходимо дождаться записи изменений, внесенных потоком
			// кодирования в тело класса. Поток уже не работает, но
			// установленное им булевское свойство, возможно, ещё
			// "не проявилось"
			for(int i = 0; i < (int)WaitCount.MaxWaitCount; i++)
			{
				if(!this.eFileSplitter.Finished)
				{
					Thread.Sleep((int)WaitTime.MinWaitTime);
				}
				else
				{
					break;
				}
			}

			// Если циклы ожидания закрытия файловых потоков не привели к желаемому
			// результату - это ошибка
			if(!this.eFileSplitter.ProcessedOK)
			{
				// Указываем на то, что обработка была прервана
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
		/// "Лечение" набора файлов
		/// </summary>
		private void Repair()
		{
			// Список томов, используемых для восстановления
			int[] volList;

			// Осуществляем проверку сигнатур целостности CRC-64 для всего набора томов
			if(this.eFileAnalyzer.StartToAnalyzeCRC64(this.path, this.fileName, this.dataCount, this.eccCount, this.codecType, this.fastExtraction, true))
			{
				// Цикл ожидания завершения процесса расчета сигнатур целостности томов
				while(true)
				{
					// Если не обнаружили установленного события "executeEvent",
					// то пользователь хочет, чтобы мы поставили обработку на паузу -
					if(!ManualResetEvent.WaitAll(this.executeEvent, 0, false))
					{
						//...приостанавливаем работу контролируемого алгоритма...
						this.eFileAnalyzer.Pause();

						//...и сами засыпаем
						ManualResetEvent.WaitAll(this.executeEvent);

						// А когда проснулись, указываем, что обработка должна продолжаться
						this.eFileAnalyzer.Continue();
					}

					// Ждем любое из перечисленных событий...
					int eventIdx = ManualResetEvent.WaitAny(new ManualResetEvent[] {this.wakeUpEvent[0], this.exitEvent[0], this.eFileAnalyzer.FinishedEvent[0]});

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
						///...останавливаем контролируемый алгоритм
						this.eFileAnalyzer.Stop();

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

			// В связи с закрытием большого количества файловых потоков
			// необходимо дождаться записи изменений, внесенных потоком
			// кодирования в тело класса. Поток уже не работает, но
			// установленное им булевское свойство, возможно, ещё
			// "не проявилось"
			for(int i = 0; i < (int)WaitCount.MaxWaitCount; i++)
			{
				if(!this.eFileAnalyzer.Finished)
				{
					Thread.Sleep((int)WaitTime.MinWaitTime);
				}
				else
				{
					break;
				}
			}

			// Если циклы ожидания закрытия файловых потоков не привели к желаемому
			// результату - это ошибка
			if(!this.eFileAnalyzer.ProcessedOK)
			{
				// Указываем на то, что обработка была прервана
				this.processedOK = false;

				// Активируем индикатор актуального состояния переменных-членов
				this.finished = true;

				// Устанавливаем событие завершения обработки
				this.finishedEvent[0].Set();

				return;
			}

			// Теперь, когда обработка завершена, необходимо проанализировать
			// полученный вектор "volList"
			volList = this.eFileAnalyzer.VolList;

			// Изначально предполагаем, что восстановление данных не потребуется
			bool needToRecover = false;

			// Проверяем вектор на наличие в нем томов для восстановления
			for(int dataNum = 0; dataNum < this.dataCount; ++dataNum)
			{
				// Если встретился том для восстановления, часть
				// основных томов повреждена и требуется применение "FileCodec"
				if(volList[dataNum] != dataNum)
				{
					needToRecover = true;

					break;
				}
			}

			// Если требуется восстановление основных томов, запускаем его
			if(needToRecover)
			{
				// Восстанавливаем утерянные основные тома
				if(this.eFileCodec.StartToDecode(this.path, this.fileName, this.dataCount, this.eccCount, volList, this.codecType, true))
				{
					// Цикл ожидания завершения этапа декодирования томов
					while(true)
					{
						// Если не обнаружили установленного события "executeEvent",
						// то пользователь хочет, чтобы мы поставили обработку на паузу -
						if(!ManualResetEvent.WaitAll(this.executeEvent, 0, false))
						{
							//...приостанавливаем работу контролируемого алгоритма...
							this.eFileCodec.Pause();

							//...и сами засыпаем
							ManualResetEvent.WaitAll(this.executeEvent);

							// А когда проснулись, указываем, что обработка должна продолжаться
							this.eFileCodec.Continue();
						}

						// Ждем любое из перечисленных событий...
						int eventIdx = ManualResetEvent.WaitAny(new ManualResetEvent[] {this.wakeUpEvent[0], this.exitEvent[0], this.eFileCodec.FinishedEvent[0]});

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
							///...останавливаем контролируемый алгоритм
							this.eFileCodec.Stop();

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

				// В связи с закрытием большого количества файловых потоков
				// необходимо дождаться записи изменений, внесенных потоком
				// кодирования в тело класса. Поток уже не работает, но
				// установленное им булевское свойство, возможно, ещё
				// "не проявилось"
				for(int i = 0; i < (int)WaitCount.MaxWaitCount; i++)
				{
					if(!this.eFileCodec.Finished)
					{
						Thread.Sleep((int)WaitTime.MinWaitTime);
					}
					else
					{
						break;
					}
				}

				// Если циклы ожидания закрытия файловых потоков не привели к желаемому
				// результату - это ошибка
				if(!this.eFileCodec.ProcessedOK)
				{
					// Указываем на то, что обработка была прервана
					this.processedOK = false;

					// Активируем индикатор актуального состояния переменных-членов
					this.finished = true;

					// Устанавливаем событие завершения обработки
					this.finishedEvent[0].Set();

					return;
				}
			}

			// Файловый поток (необходим для того, чтобы укоротить каждый из файлов набора)
			FileStream eFileStream = null;

			try
			{
				// Имя файла для обработки
				String fileName;

				// Обрабатываем все файлы
				for(int i = 0; i < (this.dataCount + this.eccCount); i++)
				{
					// Считываем первоначальное имя файла,...
					fileName = this.fileName;

					//...упаковываем его в префиксный формат...
					this.eFileNamer.Pack(ref fileName, i, this.dataCount, this.eccCount, this.codecType);

					//...формируем полное имя файла...
					fileName = this.path + fileName;

					//...производим тест на наличие файла...
					if(File.Exists(fileName))
					{
						//...если таковой имеется, ставим на него атрибуты
						// по-умолчанию
						File.SetAttributes(fileName, FileAttributes.Normal);

						//...открываем файловый поток на запись...
						eFileStream = new FileStream(fileName, FileMode.Open, System.IO.FileAccess.Write);

						if(eFileStream != null)
						{
							//...укорачиваем его ровно на 8 байт (убирая CRC-64)...
							eFileStream.SetLength(eFileStream.Length - 8);

							//...сливаем файловый буфер...
							eFileStream.Flush();

							//...и закрываем файл
							eFileStream.Close();

							// Если закрыли поток - присваиваем ему null, чтобы в случае
							// исключительной ситуации корректно распознавать неоткрытые потоки
							eFileStream = null;
						}
					}
				}
			}

				// Если было хотя бы одно исключение - закрываем файловый поток и
				// сообщаем об ошибке
			catch
			{
				// Закрываем файловый поток
				if(eFileStream != null)
				{
					eFileStream.Close();
					eFileStream = null;
				}

				// Указываем на то, что процесс "лечения" набора файлов прошел некорректно
				this.processedOK = false;

				// Устанавливаем индикатор актуального состояния переменных-членов
				this.finished = true;

				return;
			}

			// Если в результате анализа набора томов было установлено, что
			// все тома для восстановления являются неповрежденными,
			// нет потребности в их повторном создании
			if(!this.eFileAnalyzer.AllEccVolsOK)
			{
				// Создаем тома для восстановления
				if(this.eFileCodec.StartToEncode(this.path, this.fileName, this.dataCount, this.eccCount, this.codecType, true))
				{
					// Цикл ожидания завершения этапа кодирования томов
					while(true)
					{
						// Если не обнаружили установленного события "executeEvent",
						// то пользователь хочет, чтобы мы поставили обработку на паузу -
						if(!ManualResetEvent.WaitAll(this.executeEvent, 0, false))
						{
							//...приостанавливаем работу контролируемого алгоритма...
							this.eFileCodec.Pause();

							//...и сами засыпаем
							ManualResetEvent.WaitAll(this.executeEvent);

							// А когда проснулись, указываем, что обработка должна продолжаться
							this.eFileCodec.Continue();
						}

						// Ждем любое из перечисленных событий...
						int eventIdx = ManualResetEvent.WaitAny(new ManualResetEvent[] {this.wakeUpEvent[0], this.exitEvent[0], this.eFileCodec.FinishedEvent[0]});

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
							///...останавливаем контролируемый алгоритм
							this.eFileCodec.Stop();

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

				// В связи с закрытием большого количества файловых потоков
				// необходимо дождаться записи изменений, внесенных потоком
				// кодирования в тело класса. Поток уже не работает, но
				// установленное им булевское свойство, возможно, ещё
				// "не проявилось"
				for(int i = 0; i < (int)WaitCount.MaxWaitCount; i++)
				{
					if(!this.eFileCodec.Finished)
					{
						Thread.Sleep((int)WaitTime.MinWaitTime);
					}
					else
					{
						break;
					}
				}

				// Если циклы ожидания закрытия файловых потоков не привели к желаемому
				// результату - это ошибка
				if(!this.eFileCodec.ProcessedOK)
				{
					// Указываем на то, что обработка была прервана
					this.processedOK = false;

					// Активируем индикатор актуального состояния переменных-членов
					this.finished = true;

					// Устанавливаем событие завершения обработки
					this.finishedEvent[0].Set();

					return;
				}
			}

			// Осуществляем вычисление сигнатур целостности CRC-64 для всего набора томов
			if(this.eFileAnalyzer.StartToWriteCRC64(this.path, this.fileName, this.dataCount, this.eccCount, this.codecType, true))
			{
				// Цикл ожидания завершения процесса расчета сигнатур целостности томов
				while(true)
				{
					// Если не обнаружили установленного события "executeEvent",
					// то пользователь хочет, чтобы мы поставили обработку на паузу -
					if(!ManualResetEvent.WaitAll(this.executeEvent, 0, false))
					{
						//...приостанавливаем работу контролируемого алгоритма...
						this.eFileAnalyzer.Pause();

						//...и сами засыпаем
						ManualResetEvent.WaitAll(this.executeEvent);

						// А когда проснулись, указываем, что обработка должна продолжаться
						this.eFileAnalyzer.Continue();
					}

					// Ждем любое из перечисленных событий...
					int eventIdx = ManualResetEvent.WaitAny(new ManualResetEvent[] {this.wakeUpEvent[0], this.exitEvent[0], this.eFileAnalyzer.FinishedEvent[0]});

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
						///...останавливаем контролируемый алгоритм
						this.eFileAnalyzer.Stop();

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

			// В связи с закрытием большого количества файловых потоков
			// необходимо дождаться записи изменений, внесенных потоком
			// кодирования в тело класса. Поток уже не работает, но
			// установленное им булевское свойство, возможно, ещё
			// "не проявилось"
			for(int i = 0; i < (int)WaitCount.MaxWaitCount; i++)
			{
				if(!this.eFileAnalyzer.Finished)
				{
					Thread.Sleep((int)WaitTime.MinWaitTime);
				}
				else
				{
					break;
				}
			}

			// Если циклы ожидания закрытия файловых потоков не привели к желаемому
			// результату - это ошибка
			if(!this.eFileAnalyzer.ProcessedOK)
			{
				// Указываем на то, что обработка была прервана
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
		/// Тестирование набора файлов
		/// </summary>
		private void Test()
		{
			// Осуществляем проверку сигнатур целостности CRC-64 для всего набора томов
			if(this.eFileAnalyzer.StartToAnalyzeCRC64(this.path, this.fileName, this.dataCount, this.eccCount, this.codecType, this.fastExtraction, true))
			{
				// Цикл ожидания завершения процесса расчета сигнатур целостности томов
				while(true)
				{
					// Если не обнаружили установленного события "executeEvent",
					// то пользователь хочет, чтобы мы поставили обработку на паузу -
					if(!ManualResetEvent.WaitAll(this.executeEvent, 0, false))
					{
						//...приостанавливаем работу контролируемого алгоритма...
						this.eFileAnalyzer.Pause();

						//...и сами засыпаем
						ManualResetEvent.WaitAll(this.executeEvent);

						// А когда проснулись, указываем, что обработка должна продолжаться
						this.eFileAnalyzer.Continue();
					}

					// Ждем любое из перечисленных событий...
					int eventIdx = ManualResetEvent.WaitAny(new ManualResetEvent[] {this.wakeUpEvent[0], this.exitEvent[0], this.eFileAnalyzer.FinishedEvent[0]});

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
						///...останавливаем контролируемый алгоритм
						this.eFileAnalyzer.Stop();

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

			// В связи с закрытием большого количества файловых потоков
			// необходимо дождаться записи изменений, внесенных потоком
			// кодирования в тело класса. Поток уже не работает, но
			// установленное им булевское свойство, возможно, ещё
			// "не проявилось"
			for(int i = 0; i < (int)WaitCount.MaxWaitCount; i++)
			{
				if(!this.eFileAnalyzer.Finished)
				{
					Thread.Sleep((int)WaitTime.MinWaitTime);
				}
				else
				{
					break;
				}
			}

			// Если циклы ожидания закрытия файловых потоков не привели к желаемому
			// результату - это ошибка
			if(!this.eFileAnalyzer.ProcessedOK)
			{
				// Указываем на то, что обработка была прервана
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