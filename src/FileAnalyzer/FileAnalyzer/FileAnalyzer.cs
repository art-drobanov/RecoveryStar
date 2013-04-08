/*----------------------------------------------------------------------+
 |  filename:   FileAnalyzer.cs                                         |
 |----------------------------------------------------------------------|
 |  version:    2.22                                                    |
 |  revision:   02.04.2013 17:00                                        |
 |  authors:    Дробанов Артём Федорович (DrAF),                        |
 |              RUSpectrum (г. Оренбург).                               |
 |  e-mail:     draf@mail.ru                                            |
 |  purpose:    Контроль целостности файлов в RAID-подобной схеме       |
 +----------------------------------------------------------------------*/

using System;
using System.Threading;
using System.IO;

namespace RecoveryStar
{
	/// <summary>
	/// Класс контроля целостности набора файлов-томов
	/// </summary>
	public class FileAnalyzer
	{
		#region Delegates

		/// <summary>
		/// Делегат обновления прогресса контроля целостности файлов
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
					(this.thrFileAnalyzer != null)
					&&
					(
						(this.thrFileAnalyzer.ThreadState == ThreadState.Running)
						||
						(this.thrFileAnalyzer.ThreadState == ThreadState.WaitSleepJoin)
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
		/// Экземляр класса полностью закончил обработку?
		/// </summary>
		private bool finished;

		/// <summary>
		/// Булевское свойство "Множество файлов обработано корректно?"
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
		/// Обработка набора файлов произведена корректно?
		/// </summary>
		private bool processedOK;

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

		/// <summary>
		/// Вектор, указывающий на состав томов
		/// </summary>
		private int[] volList;

		/// <summary>
		/// Все тома для восстановления корректны?
		/// </summary>
		public bool AllEccVolsOK
		{
			get
			{
				if(!InProcessing)
				{
					return this.allEccVolsOK;
				}
				else
				{
					return false;
				}
			}
		}

		/// <summary>
		/// Все тома для восстановления корректны?
		/// </summary>
		private bool allEccVolsOK;

		/// <summary>
		/// Приоритет процесса
		/// </summary>
		public int ThreadPriority
		{
			get { return (int)this.threadPriority; }

			set
			{
				if(
					(this.thrFileAnalyzer != null)
					&&
					(this.thrFileAnalyzer.IsAlive)
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
					this.thrFileAnalyzer.Priority = this.threadPriority;

					// Дублируем установку параметра для подконтрольного объекта
					if(this.eFileIntegrityCheck != null)
					{
						this.eFileIntegrityCheck.ThreadPriority = value;
					}
				}
			}
		}

		/// <summary>
		/// Приоритет процесса контроля целостности файлов
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
		/// Экземпляр класса контроля целостности набора файлов
		/// </summary>
		private FileIntegrityCheck eFileIntegrityCheck;

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
		/// Используется быстрое извлечение из томов (без проверки CRC-64)?
		/// </summary>
		private bool fastExtraction;

		/// <summary>
		/// Поток контроля целостности файла
		/// </summary>
		private Thread thrFileAnalyzer;

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

		#endregion Data

		#region Construction & Destruction

		/// <summary>
		/// Конструктор класса
		/// </summary>
		public FileAnalyzer()
		{
			// Модуль для упаковки (распаковки) имени файла в префиксный формат
			this.eFileNamer = new FileNamer();

			// Создаем экземпляр класса контроля целостности набора файлов
			this.eFileIntegrityCheck = new FileIntegrityCheck();

			// Путь к файлам для обработки по-умолчанию пустой
			this.path = "";

			// Инициализируем имя файла по-умолчанию
			this.fileName = "NONAME";

			// Изначально все тома для восстановления считаем поврежденными
			this.allEccVolsOK = false;

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
		/// Метод запуска потока обработки вычисления и записи CRC64 в конец файлов
		/// </summary>
		/// <param name="path">Путь к файлам для обработки</param>
		/// <param name="fileName">Имя файла для обработки</param>
		/// <param name="dataCount">Конфигурация количества основных томов</param>
		/// <param name="eccCount">Конфигурация количества томов для восстановления</param>
		/// <param name="codecType">Тип кодека Рида-Соломона (по типу матрицы)</param>
		/// <param name="runAsSeparateThread">Запускать в отдельном потоке?</param>
		/// <returns>Булевский флаг операции</returns>
		public bool StartToWriteCRC64(String path, String fileName, int dataCount, int eccCount, int codecType, bool runAsSeparateThread)
		{
			// Если поток вычисления CRC-64 работает - не позволяем повторный запуск
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

			// Сохраняем тип кодека Рида-Соломона (по типу используемой матрицы кодирования)
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
				// Вычисляем CRC-64 для каждого из файлов набора
				WriteCRC64();

				// Возвращаем результат обработки
				return this.processedOK;
			}

			// Создаем поток вычисления и записи CRC-64...
			this.thrFileAnalyzer = new Thread(new ThreadStart(WriteCRC64));

			//...затем даем ему имя...
			this.thrFileAnalyzer.Name = "FileAnalyzer.WriteCRC64()";

			//...устанавливаем выбранный приоритет задачи...
			this.thrFileAnalyzer.Priority = this.threadPriority;

			//...и запускаем его
			this.thrFileAnalyzer.Start();

			// Сообщаем, что все нормально
			return true;
		}

		/// <summary>
		/// Метод запуска потока обработки проверки CRC64, записанного в конец
		/// каждого из файлов набора, с генерированием списка имеющихся томов "volList",
		/// который будет использован декодером для восстановления данных
		/// </summary>
		/// <param name="path">Путь к файлам для обработки</param>
		/// <param name="fileName">Имя файла для обработки</param>
		/// <param name="dataCount">Конфигурация количества основных томов</param>
		/// <param name="eccCount">Конфигурация количества томов для восстановления</param>
		/// <param name="codecType">Тип кодека Рида-Соломона (по типу матрицы)</param>
		/// <param name="fastExtraction">Используется быстрое извлечение из томов (без проверки CRC-64)?</param>
		/// <param name="runAsSeparateThread">Запускать в отдельном потоке?</param>
		/// <returns>Булевский флаг операции</returns>
		public bool StartToAnalyzeCRC64(String path, String fileName, int dataCount, int eccCount, int codecType, bool fastExtraction, bool runAsSeparateThread)
		{
			// Если поток вычисления CRC-64 работает - не позволяем повторный запуск
			if(InProcessing)
			{
				return false;
			}

			// Изначально все тома для восстановления считаем поврежденными
			this.allEccVolsOK = false;

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

			// Сохраняем тип кодека Рида-Соломона (по типу используемой матрицы кодирования)
			this.codecType = codecType;

			// Используется быстрое извлечение из томов (без проверки CRC-64)?
			this.fastExtraction = fastExtraction;

			// Указываем, что поток должен выполняться
			this.exitEvent[0].Reset();
			this.executeEvent[0].Set();
			this.wakeUpEvent[0].Reset();
			this.finishedEvent[0].Reset();

			// Если указано, что не требуется запуск в отдельном потоке,
			// запускаем в данном
			if(!runAsSeparateThread)
			{
				// Вычисляем и проверяем CRC-64 для каждого из файлов набора с заполнением
				// свойства VolList
				AnalyzeCRC64();

				// Возвращаем результат обработки
				return this.processedOK;
			}

			// Создаем поток вычисления и проверки CRC-64...
			this.thrFileAnalyzer = new Thread(new ThreadStart(AnalyzeCRC64));

			//...затем даем ему имя...
			this.thrFileAnalyzer.Name = "FileAnalyzer.AnalyzeCRC64()";

			//...устанавливаем выбранный приоритет задачи...
			this.thrFileAnalyzer.Priority = this.threadPriority;

			//...и запускаем его
			this.thrFileAnalyzer.Start();

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
		/// Вычисление и запись в конец файлов значения CRC-64
		/// </summary>
		private void WriteCRC64()
		{
			// Вычисляем значение модуля, который позволит выводить процент обработки
			// ровно при единичном приращении для цикла по "i"
			int progressMod1 = (this.dataCount + this.eccCount) / 100;

			// Если модуль равен нулю, то увеличиваем его до значения "1", чтобы
			// прогресс выводился на каждой итерации (файл очень маленький)
			if(progressMod1 == 0)
			{
				progressMod1 = 1;
			}

			// Подвергаем обработке все тома
			for(int volNum = 0; volNum < (this.dataCount + this.eccCount); volNum++)
			{
				// Считываем первоначальное имя файла
				String fileName = this.fileName;

				// Получаем имя исходного файла в префиксной форме
				this.eFileNamer.Pack(ref fileName, volNum, this.dataCount, this.eccCount, this.codecType);

				// Формируем полное имя файла
				fileName = this.path + fileName;

				// Производим вычисление CRC-64 для каждого файла
				if(this.eFileIntegrityCheck.StartToWriteCRC64(fileName, true))
				{
					// Цикл ожидания завершения обработки файла
					while(true)
					{
						// Если не обнаружили установленного события "executeEvent",
						// то пользователь хочет, чтобы мы поставили обработку на паузу -
						if(!ManualResetEvent.WaitAll(this.executeEvent, 0, false))
						{
							//...приостанавливаем работу контролируемого алгоритма...
							this.eFileIntegrityCheck.Pause();

							//...и сами засыпаем
							ManualResetEvent.WaitAll(this.executeEvent);

							// А когда проснулись, указываем, что обработка должна продолжаться
							this.eFileIntegrityCheck.Continue();
						}

						// Ждем любое из перечисленных событий...
						int eventIdx = ManualResetEvent.WaitAny(new ManualResetEvent[] {this.wakeUpEvent[0], this.exitEvent[0], this.eFileIntegrityCheck.FinishedEvent[0]});

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
							this.eFileIntegrityCheck.Stop();

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
					if(!this.eFileIntegrityCheck.Finished)
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
				if(!this.eFileIntegrityCheck.ProcessedOK)
				{
					// Указываем на то, что обработка не была завершена корректно
					this.processedOK = false;

					// Активируем индикатор актуального состояния переменных-членов
					this.finished = true;

					// Устанавливаем событие завершения обработки
					this.finishedEvent[0].Set();

					return;
				}

				// Выводим прогресс обработки
				if(
					((volNum % progressMod1) == 0)
					&&
					(OnUpdateFileAnalyzeProgress != null)
					)
				{
					OnUpdateFileAnalyzeProgress(((double)(volNum + 1) / (double)(this.dataCount + this.eccCount)) * 100.0);
				}

				// В случае, если требуется постановка на паузу, событие "executeEvent"
				// будет сброшено, и будем на паузе вплоть до его появления
				ManualResetEvent.WaitAll(this.executeEvent);

				// Если указано, что требуется выйти из потока - выходим
				if(ManualResetEvent.WaitAll(this.exitEvent, 0, false))
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

			// Сообщаем об окончании процесса обработки
			if(OnFileAnalyzeFinish != null)
			{
				OnFileAnalyzeFinish();
			}

			// Сообщаем, что обработка прошла корректно
			this.processedOK = true;

			// Активируем индикатор актуального состояния переменных-членов
			this.finished = true;

			// Устанавливаем событие завершения обработки
			this.finishedEvent[0].Set();
		}

		/// <summary>
		/// Вычисление и проверка значения CRC-64, записанного в конце файла
		/// </summary>
		private void AnalyzeCRC64()
		{
			// Вычисляем значение модуля, который позволит выводить процент обработки
			// ровно при единичном приращении для цикла по "i"
			int progressMod1 = (this.dataCount + this.eccCount) / 100;

			// Если модуль равен нулю, то увеличиваем его до значения "1", чтобы
			// прогресс выводился на каждой итерации (файл очень маленький)
			if(progressMod1 == 0)
			{
				progressMod1 = 1;
			}

			// Выделяем память под "volList"
			this.volList = new int[this.dataCount];

			// Выделяем память под "eccList"
			int[] eccList = new int[this.eccCount];

			// Индекс в массиве томов
			int volListIdx = 0;

			// Индекс в массиве томов для восстановления
			int eccListIdx = 0;

			// Счетчик количества поврежденных основных томов
			int dataVolMissCount = 0;

			// Счетчик количества найденных томов для восстановления
			int eccVolPresentCount = 0;

			// Имя файла для обработки
			String fileName;

			// Подвергаем проверке все основные тома
			for(int dataNum = 0; dataNum < this.dataCount; dataNum++)
			{
				// Изначально предполагаем, что текущий том поврежден
				bool dataVolIsOK = false;

				// Считываем первоначальное имя файла
				fileName = this.fileName;

				// Получаем имя исходного файла в префиксной форме
				this.eFileNamer.Pack(ref fileName, dataNum, this.dataCount, this.eccCount, this.codecType);

				// Формируем полное имя файла
				fileName = this.path + fileName;

				// Если исходный файл существует...
				if(File.Exists(fileName))
				{
					// Если не используется быстрое извлечение - проверяем на целостность
					// CRC-64, иначе полагаем, что всё корректно (целостность тома берем
					// по факту его наличия)
					if(!this.fastExtraction)
					{
						//...- производим его проверку
						if(this.eFileIntegrityCheck.StartToCheckCRC64(fileName, true))
						{
							// Цикл ожидания завершения обработки файла
							while(true)
							{
								// Если не обнаружили установленного события "executeEvent",
								// то пользователь хочет, чтобы мы поставили обработку на паузу -
								if(!ManualResetEvent.WaitAll(this.executeEvent, 0, false))
								{
									//...приостанавливаем работу контролируемого алгоритма...
									this.eFileIntegrityCheck.Pause();

									//...и сами засыпаем
									ManualResetEvent.WaitAll(this.executeEvent);

									// А когда проснулись, указываем, что обработка должна продолжаться
									this.eFileIntegrityCheck.Continue();
								}

								// Ждем любое из перечисленных событий...
								int eventIdx = ManualResetEvent.WaitAny(new ManualResetEvent[] {this.wakeUpEvent[0], this.exitEvent[0], this.eFileIntegrityCheck.FinishedEvent[0]});

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
									this.eFileIntegrityCheck.Stop();

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
							if(!this.eFileIntegrityCheck.Finished)
							{
								Thread.Sleep((int)WaitTime.MinWaitTime);
							}
							else
							{
								break;
							}
						}

						// Указываем, что основной том корректен
						if(this.eFileIntegrityCheck.ProcessedOK)
						{
							dataVolIsOK = true;
						}
					}
					else
					{
						// Указываем, что основной том корректен
						dataVolIsOK = true;
					}

					// Выводим прогресс обработки
					if(
						((dataNum % progressMod1) == 0)
						&&
						(OnUpdateFileAnalyzeProgress != null)
						)
					{
						OnUpdateFileAnalyzeProgress(((double)(dataNum + 1) / (double)(this.dataCount + this.eccCount)) * 100.0);
					}

					// В случае, если требуется постановка на паузу, событие "executeEvent"
					// будет сброшено, и будем на паузе вплоть до его появления
					ManualResetEvent.WaitAll(this.executeEvent);

					// Если указано, что требуется выйти из потока - выходим
					if(ManualResetEvent.WaitAll(this.exitEvent, 0, false))
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

				// Если данный основной том не поврежден, записываем его в "volList",
				// а иначе увеличиваем счетчик поврежденных томов и ставим на место
				// номера тома значение "-1", которое укажет на необходимость подстановки
				// тома для восстановления
				if(dataVolIsOK)
				{
					this.volList[volListIdx++] = dataNum;
				}
				else
				{
					this.volList[volListIdx++] = -1;

					// Увеличиваем счетчик количества поврежденных основных томов
					dataVolMissCount++;
				}
			}

			// Теперь, когда знаем количество поврежденных основных томов,
			// нужно просканировать все файлы для восстановления, и определить
			// требуемую их часть в список томов, а "избыток" поместить в
			// список альтренативных томов для восстановления
			for(int eccNum = this.dataCount; eccNum < (this.dataCount + this.eccCount); eccNum++)
			{
				// Изначально предполагаем, что текущий том поврежден
				bool eccVolIsOK = false;

				// Считываем первоначальное имя файла
				fileName = this.fileName;

				// Получаем имя исходного файла в префиксной форме
				this.eFileNamer.Pack(ref fileName, eccNum, this.dataCount, this.eccCount, this.codecType);

				// Формируем полное имя файла
				fileName = this.path + fileName;

				// Если исходный файл существует...
				if(File.Exists(fileName))
				{
					// Если не используется быстрое извлечение - проверяем на целостность
					// CRC-64, иначе полагаем, что всё корректно (целостность тома берем
					// по факту его наличия)
					if(!this.fastExtraction)
					{
						//...- производим его проверку
						if(this.eFileIntegrityCheck.StartToCheckCRC64(fileName, true))
						{
							// Цикл ожидания завершения обработки файла
							while(true)
							{
								// Если не обнаружили установленного события "executeEvent",
								// то пользователь хочет, чтобы мы поставили обработку на паузу -
								if(!ManualResetEvent.WaitAll(this.executeEvent, 0, false))
								{
									//...приостанавливаем работу контролируемого алгоритма...
									this.eFileIntegrityCheck.Pause();

									//...и сами засыпаем
									ManualResetEvent.WaitAll(this.executeEvent);

									// А когда проснулись, указываем, что обработка должна продолжаться
									this.eFileIntegrityCheck.Continue();
								}

								// Ждем любое из перечисленных событий...
								int eventIdx = ManualResetEvent.WaitAny(new ManualResetEvent[] {this.wakeUpEvent[0], this.exitEvent[0], this.eFileIntegrityCheck.FinishedEvent[0]});

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
									this.eFileIntegrityCheck.Stop();

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
							if(!this.eFileIntegrityCheck.Finished)
							{
								Thread.Sleep((int)WaitTime.MinWaitTime);
							}
							else
							{
								break;
							}
						}

						// Указываем, что том для восстановления корректен
						if(this.eFileIntegrityCheck.ProcessedOK)
						{
							eccVolIsOK = true;
						}
					}
					else
					{
						// Указываем, что том для восстановления корректен
						eccVolIsOK = true;
					}

					// Выводим прогресс обработки
					if(
						((eccNum % progressMod1) == 0)
						&&
						(OnUpdateFileAnalyzeProgress != null)
						)
					{
						OnUpdateFileAnalyzeProgress(((double)(eccNum + 1) / (double)(this.dataCount + this.eccCount)) * 100.0);
					}

					// В случае, если требуется постановка на паузу, событие "executeEvent"
					// будет сброшено, и будем на паузе вплоть до его появления
					ManualResetEvent.WaitAll(this.executeEvent);

					// Если указано, что требуется выйти из потока - выходим
					if(ManualResetEvent.WaitAll(this.exitEvent, 0, false))
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

				// Если том для восстановления хороший...
				if(eccVolIsOK)
				{
					//...- добавляем его в список
					eccList[eccListIdx++] = eccNum;

					// Увеличиваем счетчик количества томов для восстановления
					eccVolPresentCount++;
				}
				else
				{
					//...а иначе указываем, что том поврежден
					eccList[eccListIdx++] = -1;
				}
			}

			// Если значение счетчика количества корректных томов для восстановления совпадает
			// со значением счетчика томов для восстановления конфигурации - все тома для
			// восстановления являются неповрежденными
			if(eccVolPresentCount == this.eccCount)
			{
				this.allEccVolsOK = true;
			}

			// Выводим статистику повреждений
			if(OnGetDamageStat != null)
			{
				// Вычисляем общий процент повреждений (сумму повреждений основных томов и томов для восстановления
				// делим на общее количество томов)
				double percOfDamage = ((double)(dataVolMissCount + (this.eccCount - eccVolPresentCount)) / (double)(this.dataCount + this.eccCount)) * 100;

				// Вычисляем процент "выживших" альтернативных томов для восстановления
				// Альтернативные тома - это изначально те тома, которые не планируется использовать для восстановления
				double percOfAltEcc = ((double)(eccVolPresentCount - dataVolMissCount) / (double)this.eccCount) * 100;

				// Выводим статистику повреждений
				OnGetDamageStat(dataVolMissCount + (this.eccCount - eccVolPresentCount), (eccVolPresentCount - dataVolMissCount), percOfDamage, percOfAltEcc);
			}

			// Если нет поврежденных основных томов, просто выходим
			if(dataVolMissCount == 0)
			{
				// Сообщаем об окончании процесса обработки
				if(OnFileAnalyzeFinish != null)
				{
					OnFileAnalyzeFinish();
				}

				// Указываем на то, что данные не повреждены
				this.processedOK = true;

				// Активируем индикатор актуального состояния переменных-членов
				this.finished = true;

				// Устанавливаем событие завершения обработки
				this.finishedEvent[0].Set();

				return;
			}

			// Если мы не сможем восстановить повреждения...
			if(eccVolPresentCount < dataVolMissCount)
			{
				// Сообщаем об окончании процесса обработки
				if(OnFileAnalyzeFinish != null)
				{
					OnFileAnalyzeFinish();
				}

				//...указываем на то, что данные не могут быть восстановлены
				this.processedOK = false;

				// Активируем индикатор актуального состояния переменных-членов
				this.finished = true;

				// Устанавливаем событие завершения обработки
				this.finishedEvent[0].Set();

				return;
			}

			// Перемещаемся на начало списка томов для восстановления
			eccListIdx = 0;

			// Теперь пробегаемся по вектору "volList", и вместо каждого из значений "-1"
			// подставляем очередное значение из найденного диапазона
			for(int i = 0; i < this.dataCount; i++)
			{
				if(this.volList[i] == -1)
				{
					// Пробегаемся по вектору томов для восстановления,
					// останавливаясь на корректном томе для восстановления
					while(eccList[eccListIdx] == -1)
					{
						eccListIdx++;
					}

					// Подставляем на место поврежденного основного тома
					// том для восстановления,...
					this.volList[i] = eccList[eccListIdx];

					//...убирая использованный том из списка
					eccList[eccListIdx] = -1;
				}
			}

			// Сообщаем об окончании процесса обработки
			if(OnFileAnalyzeFinish != null)
			{
				OnFileAnalyzeFinish();
			}

			// Сообщаем, что обработка прошла корректно
			this.processedOK = true;

			// Активируем индикатор актуального состояния переменных-членов
			this.finished = true;

			// Устанавливаем событие завершения обработки
			this.finishedEvent[0].Set();
		}

		#endregion Private Operations
	}
}