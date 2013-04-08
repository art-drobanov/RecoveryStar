/*----------------------------------------------------------------------+
 |  filename:   FileSplitter.cs                                         |
 |----------------------------------------------------------------------|
 |  version:    2.22                                                    |
 |  revision:   02.04.2013 17:00                                        |
 |  authors:    Дробанов Артём Федорович (DrAF),                        |
 |              RUSpectrum (г. Оренбург).                               |
 |  e-mail:     draf@mail.ru                                            |
 |  purpose:    Разрезание (склеивание) файлов-томов на фрагменты       |
 +----------------------------------------------------------------------*/

using System;
using System.Threading;
using System.IO;

namespace RecoveryStar
{
	/// <summary>
	/// Класс для разрезания (склеивания) файлов на фрагменты
	/// </summary>
	public class FileSplitter
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

		#endregion Delegates

		#region Constants

		/// <summary>
		/// Указываем размер CBC-блока в килобайтах по-умолчанию (128 Мб)
		/// </summary>
		private const int defCbcBlockSize = 1 << 17;

		/// <summary>
		/// Размер блока 256 бит в байтах
		/// </summary>
		private const int bits256 = 32;

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
					(this.thrFileSplitter != null)
					&&
					(
						(this.thrFileSplitter.ThreadState == ThreadState.Running)
						||
						(this.thrFileSplitter.ThreadState == ThreadState.WaitSleepJoin)
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
				if(!InProcessing)
				{
					return this.eSecurity;
				}
				else
				{
					return null;
				}
			}

			set
			{
				if(!InProcessing)
				{
					this.eSecurity = value;
				}
			}
		}

		/// <summary>
		/// Экземпляр класса криптографической защиты данных
		/// </summary>
		private Security eSecurity;

		/// <summary>
		/// Размер CBC-блока (Кб), используемый при шифровании
		/// </summary>
		public int CBCBlockSize
		{
			get
			{
				if(!InProcessing)
				{
					return this.cbcBlockSize;
				}
				else
				{
					return -1;
				}
			}

			set
			{
				if(!InProcessing)
				{
					if(value > 0)
					{
						this.cbcBlockSize = value;
					}
					else
					{
						this.cbcBlockSize = defCbcBlockSize;
					}
				}
			}
		}

		/// <summary>
		/// Размер CBC-блока (Кб), используемый при шифровании
		/// </summary>
		private int cbcBlockSize;

		/// <summary>
		/// Размер Файлового буфера
		/// </summary>
		public int BufferLength
		{
			get
			{
				// Если класс не занят обработкой - возвращаем значение...
				if(!InProcessing)
				{
					return this.bufferLength;
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
					this.bufferLength = value - (value % 8);
				}
			}
		}

		/// <summary>
		/// Размер файлового буфера
		/// </summary>
		private int bufferLength = 1 << 26; // 64 Мб;

		/// <summary>
		/// Приоритет процесса
		/// </summary>
		public int ThreadPriority
		{
			get { return (int)this.threadPriority; }

			set
			{
				if(
					(this.thrFileSplitter != null)
					&&
					(this.thrFileSplitter.IsAlive)
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
					this.thrFileSplitter.Priority = this.threadPriority;
				}
			}
		}

		/// <summary>
		/// Приоритет процесса разбиения (склеивания) файла
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
		/// Экземпляр класса для формирования имени тома
		/// </summary>
		private FileNamer eFileNamer;

		/// <summary>
		/// Путь к файлам для обработки
		/// </summary>
		private String path;

		/// <summary>
		/// Имя файла
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
		/// Файловый буфер
		/// </summary>
		private byte[] buffer;

		/// <summary>
		/// Поток разбиения (склеивания) файла на фрагменты
		/// </summary>
		private Thread thrFileSplitter;

		/// <summary>
		/// Событие прекращения обработки файлов
		/// </summary>
		private ManualResetEvent[] exitEvent;

		/// <summary>
		/// Событие продолжения обработки файлов
		/// </summary>
		private ManualResetEvent[] executeEvent;

		#endregion Data

		#region Construction & Destruction

		/// <summary>
		/// Конструктор класса
		/// </summary>
		public FileSplitter()
		{
			// Устанавливаем размер CBC-блока по-умолчанию
			this.cbcBlockSize = defCbcBlockSize;

			// Создаем экземпляр класса для формирования имени тома
			this.eFileNamer = new FileNamer();

			// Путь к файлам для обработки по-умолчанию пустой
			this.path = "";

			// Инициализируем имя файла по-умолчанию
			this.fileName = "NONAME";

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

			// Событие, устанавливаемое по завершении обработки
			this.finishedEvent = new ManualResetEvent[] {new ManualResetEvent(true)};
		}

		#endregion Construction & Destruction

		#region Public Operations

		/// <summary>
		/// Разбиение исходного файла на фрагменты (тома)
		/// </summary>
		/// <param name="path">Путь к файлам для обработки</param>
		/// <param name="fileName">Имя файла для разбиения</param>
		/// <param name="dataCount">Конфигурация количества основных томов</param>
		/// <param name="eccCount">Конфигурация количества томов для восстановления</param>
		/// <param name="codecType">Тип кодека Рида-Соломона (по типу матрицы)</param>
		/// <param name="runAsSeparateThread">Запускать в отдельном потоке?</param>
		/// <returns>Булевский флаг операции</returns>
		public bool StartToSplit(String path, String fileName, int dataCount, int eccCount, int codecType, bool runAsSeparateThread)
		{
			// Если поток разбиения файла на фрагменты работает - не позволяем повторный запуск
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

			// Сохраняем тип кодека Рида-Соломона
			this.codecType = codecType;

			// Указываем, что поток должен выполняться
			this.exitEvent[0].Reset();
			this.executeEvent[0].Set();
			this.finishedEvent[0].Reset();

			// Если указано, что не требуется запуск в отдельном потоке,
			// запускаем в данном
			if(!runAsSeparateThread)
			{
				// Разбиваем исходный файл на фрагменты
				Split();

				// Возвращаем результат обработки
				return this.processedOK;
			}

			// Создаем поток разбиения файла на фрагменты...
			this.thrFileSplitter = new Thread(new ThreadStart(Split));

			//...затем даем ему имя...
			this.thrFileSplitter.Name = "FileSplitter.Split()";

			//...устанавливаем выбранный приоритет задачи...
			this.thrFileSplitter.Priority = this.threadPriority;

			//...и запускаем его
			this.thrFileSplitter.Start();

			// Сообщаем, что все нормально
			return true;
		}

		/// <summary>
		/// Склеивание файла из фрагментов
		/// </summary>
		/// <param name="path">Путь к файлам для обработки</param>
		/// <param name="fileName">Имя файла одного из основных томов</param>
		/// <param name="dataCount">Конфигурация количества основных томов</param>
		/// <param name="eccCount">Конфигурация количества томов для восстановления</param>
		/// <param name="codecType">Тип кодека Рида-Соломона (по типу матрицы)</param>
		/// <param name="runAsSeparateThread">Запускать в отдельном потоке?</param>
		/// <returns>Булевский флаг операции</returns>
		public bool StartToGlue(String path, String fileName, int dataCount, int eccCount, int codecType, bool runAsSeparateThread)
		{
			// Если поток склеивания файла из фрагментов работает - не позволяем повторный запуск
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
			this.finishedEvent[0].Reset();

			// Если указано, что не требуется запуск в отдельном потоке,
			// запускаем в данном
			if(!runAsSeparateThread)
			{
				// Выполняем "склеивание" файлов из фрагментов в исходный
				Glue();

				// Возвращаем результат обработки
				return this.processedOK;
			}

			// Создаем поток склеивания файлов из фрагментов...
			this.thrFileSplitter = new Thread(new ThreadStart(Glue));

			//...затем даем ему имя...
			this.thrFileSplitter.Name = "FileSplitter.Glue()";

			//...устанавливаем выбранный приоритет задачи...
			this.thrFileSplitter.Priority = this.threadPriority;

			//...и запускаем его
			this.thrFileSplitter.Start();

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

		#region Private Operations

		/// <summary>
		/// Разбиение файла на фрагменты
		/// </summary>
		private void Split()
		{
			// Экземпляры файловых потоков (исходный и целевой)
			FileStream fileStreamSource = null;
			FileStream fileStreamTarget = null;

			// Экземпляр класса записи обычных типов данных в двоичный файл
			BinaryWriter eBinaryWriter = null;

			try
			{
				// Имя файла для обработки
				String fileName;

				// Формируем полное имя файла
				fileName = this.path + this.fileName;

				// Открываем поток исходного файла на чтение
				fileStreamSource = new FileStream(fileName, FileMode.Open, System.IO.FileAccess.Read);

				// Вычисляем длину основного тома
				long volumeLength = fileStreamSource.Length / this.dataCount;

				// Сохраняем счетчик недозаписанных байт (из исходного потока в целевой)
				Int64 unwrittenCounter = fileStreamSource.Length;

				// Если при выбранной длине тома все данные не вместятся в набор томов,
				// добавляем ещё по одному байту к каждому тому. При этом в части томов этот
				// байт не будет израсходован
				if((fileStreamSource.Length % this.dataCount) != 0)
				{
					volumeLength++;
				}

				// Одним из требований RAID-подобного кодера Рида-Соломона является четный размер
				// входа. Обеспечиваем его.
				if((volumeLength % 2) != 0)
				{
					volumeLength++;
				}

				// Вычисляем значение модуля, который позволит выводить процент обработки
				// ровно при единичном приращении для цикла по "volNum"
				int progressMod1 = this.dataCount / 100;

				// Если модуль равен нулю, то увеличиваем его до значения "1", чтобы
				// прогресс выводился на каждой итерации (файл очень маленький)
				if(progressMod1 == 0)
				{
					progressMod1 = 1;
				}

				// Вычисляем значение модуля, который позволит выводить процент обработки
				// ровно при единичном приращении для цикла по "i" внутри цикла по "volNum"
				int progressMod2 = (int)((fileStreamSource.Length / this.bufferLength) / 100);

				// Если модуль равен нулю, то увеличиваем его до значения "1", чтобы
				// прогресс выводился на каждой итерации (файл очень маленький)
				if(progressMod2 == 0)
				{
					progressMod2 = 1;
				}

				// Cчетчик количества записанных байт в выходной поток
				Int64 volumeWriteCounter = 0;

				// Устанавливаем выбранный пользователем размер буфера
				// (в режиме защиты данных он совпадает с размером CBC-блока AES-256)
				// Размер буфера указывается в килобайтах!
				if(this.eSecurity != null)
				{
					this.bufferLength = (this.cbcBlockSize * 1024);
				}

				// Выделяем память под файловый буфер
				this.buffer = new byte[this.bufferLength + bits256]; // bits256 - довесок для выравнивания потока CryptoStream

				// Устанавливаем размер тома для режима шифрования данных
				long secVolumeLength = 0;

				// Работаем со всеми основными томами (+1 фиктивная итерация для сброса буферов)
				for(int volNum = 0; volNum <= this.dataCount; volNum++)
				{
					// Если мы находимся не на первой итерации, то требуется сбросить
					// файловый буфер выходного потока с дозаписью значения реально
					// содержащихся данных
					if(volNum != 0)
					{
						// Используем имеющийся открытый файловый поток для инициализации
						// экземпляра класса записи обычных типов данных в двоичный файл
						eBinaryWriter = new BinaryWriter(fileStreamTarget);

						if(eBinaryWriter != null)
						{
							// Перемещаемся на конец файла...
							eBinaryWriter.Seek(0, SeekOrigin.End);

							//...и пишем в его конец длину блока полезных данных...
							eBinaryWriter.Write(volumeWriteCounter);

							//...и, затем, обнуляем числовое значение
							volumeWriteCounter = 0;

							// Сбрасываем буфер "BinaryWriter"
							eBinaryWriter.Flush();

							// Закрываем
							eBinaryWriter.Close();
							eBinaryWriter = null;
						}

						if(fileStreamTarget != null)
						{
							//...и закрываем файловый поток
							fileStreamTarget.Close();
							fileStreamTarget = null;
						}
					}

					// Если данное условие выполнится - мы находимся на фиктивной итерации,
					// и требуется выход из цикла (т.к. все тома уже обработаны)
					if(volNum == this.dataCount)
					{
						if(fileStreamSource != null)
						{
							// Перед выходом закрываем поток исходного файла
							fileStreamSource.Close();
							fileStreamSource = null;
						}

						// Сообщаем, что обработка файла закончена
						if(OnFileSplittingFinish != null)
						{
							OnFileSplittingFinish();
						}

						break;
					}

					// Считываем первоначальное имя файла
					fileName = this.fileName;

					// Упаковываем исходное имя файла в префиксный формат
					// (для перебора всех томов в цикле)
					if(!this.eFileNamer.Pack(ref fileName, volNum, this.dataCount, this.eccCount, this.codecType))
					{
						// Закрываем исходный и целевой файловые потоки
						if(fileStreamSource != null)
						{
							fileStreamSource.Close();
							fileStreamSource = null;
						}

						if(fileStreamTarget != null)
						{
							fileStreamTarget.Close();
							fileStreamTarget = null;
						}

						// Указываем на то, что произошла ошибка работы с файлами
						this.processedOK = false;

						// Активируем индикатор актуального состояния переменных-членов
						this.finished = true;

						// Устанавливаем событие завершения обработки
						this.finishedEvent[0].Set();

						return;
					}

					// Формируем полное имя файла
					fileName = this.path + fileName;

					// Производим тест на наличие файла...
					if(File.Exists(fileName))
					{
						//...если таковой имеется, ставим на него атрибуты
						// по-умолчанию...
						File.SetAttributes(fileName, FileAttributes.Normal);
					}

					// ...затем открываем поток целевого файла на запись
					fileStreamTarget = new FileStream(fileName, FileMode.Create, System.IO.FileAccess.Write);

					// Количество основных итераций (копирование полным буфером)
					Int64 nIterations = -1;

					// Остаток, не подпадающий под вычисления основных итераций
					int iterRest = -1;

					// Если есть что записать
					if(unwrittenCounter > 0)
					{
						// Если счетчик недозаписанных байт больше либо равен размеру тома -
						// нужно применять обычное копирование с последующей дозаписью значения
						// размера тома и переходом на новую итерацию
						if(unwrittenCounter >= volumeLength)
						{
							// Узнаем количество основных итераций (копирование полным буфером)
							nIterations = volumeLength / this.bufferLength;

							// Вычисляем остаток, не подпадающий под вычисления основных итераций
							iterRest = (int)(volumeLength - (nIterations * this.bufferLength));

							// Если находимся на первой итерации - вычисляем размер тома
							if(volNum == 0)
							{
								// Узнаем откорректированное значение длины тома
								secVolumeLength = volumeLength;

								// Если установлен режим обеспечения защиты данных, необходимо
								// обеспечить кратность bits256 (это выравнивание по размеру данных
								// шифроблока, 256 бит)
								if((secVolumeLength % bits256) != 0)
								{
									secVolumeLength += (bits256 - (secVolumeLength % bits256));
								}

								// bits256 - довесок для выравнивания потока CryptoStream,
								// и на каждой итерации полным буфером он свой
								secVolumeLength += (bits256 * nIterations);

								// Плюс довесок для последней итерации
								if(iterRest != 0)
								{
									secVolumeLength += bits256;
								}
							}
						}
						else
						{
							// Узнаем количество основных итераций (копирование полным буфером)
							nIterations = unwrittenCounter / this.bufferLength;

							// Вычисляем остаток, не подпадающий под вычисления основных итераций
							iterRest = (int)(unwrittenCounter - (nIterations * this.bufferLength));

							// Если находимся на первой итерации - вычисляем размер тома
							if(volNum == 0)
							{
								// Узнаем откорректированное значение длины тома
								secVolumeLength = unwrittenCounter;

								// Если установлен режим обеспечения защиты данных, необходимо
								// обеспечить кратность bits256 (это выравнивание по размеру данных
								// шифроблока, 256 бит)
								if((secVolumeLength % bits256) != 0)
								{
									secVolumeLength += (bits256 - (secVolumeLength % bits256));
								}

								// bits256 - довесок для выравнивания потока CryptoStream,
								// и на каждой итерации полным буфером он свой
								secVolumeLength += (bits256 * nIterations);

								// Плюс довесок для последней итерации
								if(iterRest != 0)
								{
									secVolumeLength += bits256;
								}
							}

							if(this.eSecurity != null)
							{
								// Расширяем размер файлового потока до "secVolumeLength"
								fileStreamTarget.SetLength(secVolumeLength);
							}
							else
							{
								// Расширяем размер файлового потока до "volumeLength"
								fileStreamTarget.SetLength(volumeLength);
							}
						}

						// Работа с полноразмерными буферами (работа в основных итерациях)
						for(Int64 i = 0; i < nIterations; i++)
						{
							// Читаем данные в буфер
							int dataLen = this.bufferLength;
							int readed = 0;
							int toRead = 0;
							while((toRead = dataLen - (readed += fileStreamSource.Read(this.buffer, readed, toRead))) != 0) ;

							if(this.eSecurity != null)
							{
								// Шифруем данные, если это требуется...
								this.eSecurity.Encrypt(this.buffer, this.bufferLength);
								fileStreamTarget.Write(this.buffer, 0, (this.bufferLength + bits256)); // bits256 - довесок для выравнивания потока CryptoStream
							}
							else
							{
								//...а иначе пишем в открытом виде
								fileStreamTarget.Write(this.buffer, 0, this.bufferLength);
							}

							volumeWriteCounter += this.bufferLength;
							unwrittenCounter -= this.bufferLength;

							// Выводим прогресс обработки
							if(
								((((volNum * nIterations) + i) % progressMod2) == 0)
								&&
								(OnUpdateFileSplittingProgress != null)
								)
							{
								OnUpdateFileSplittingProgress(((double)((volNum * nIterations) + (i + 1)) / (double)(this.dataCount * nIterations)) * 100.0);
							}

							// В случае, если требуется постановка на паузу, событие "executeEvent"
							// будет сброшено, и будем на паузе вплоть до его появления
							ManualResetEvent.WaitAll(this.executeEvent);

							// Если указано, что требуется выйти из потока - выходим
							if(ManualResetEvent.WaitAll(this.exitEvent, 0, false))
							{
								// Закрываем исходный и целевой файловые потоки
								if(fileStreamSource != null)
								{
									fileStreamSource.Close();
									fileStreamSource = null;
								}

								if(fileStreamTarget != null)
								{
									fileStreamTarget.Close();
									fileStreamTarget = null;
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

						// Дорабатываем остаток (если он есть)
						if(iterRest > 0)
						{
							// Читаем данные в буфер
							int dataLen = iterRest;
							int readed = 0;
							int toRead = 0;
							while((toRead = dataLen - (readed += fileStreamSource.Read(this.buffer, readed, toRead))) != 0) ;

							if(this.eSecurity != null)
							{
								// Первоначально размер последнего блока шифрования данных
								// полагаем равный недовыработанному остатку, но затем его
								// корректируем на основании требований к кратности 256 битам.
								int lastBlockSize = iterRest;

								// Если установлен режим обеспечения защиты данных, необходимо
								// обеспечить кратность bits256 (это выравнивание по размеру данных
								// шифроблока, 256 бит)
								if((lastBlockSize % bits256) != 0)
								{
									lastBlockSize += (bits256 - (lastBlockSize % bits256));
								}

								// Шифруем данные, если это требуется...
								this.eSecurity.Encrypt(this.buffer, lastBlockSize);
								fileStreamTarget.Write(this.buffer, 0, (lastBlockSize + bits256)); // bits256 - довесок для выравнивания потока CryptoStream
							}
							else
							{
								//...а иначе пишем в открытом виде
								fileStreamTarget.Write(this.buffer, 0, iterRest);
							}

							volumeWriteCounter += iterRest;
							unwrittenCounter -= iterRest;

							// Выводим прогресс обработки
							if(
								((volNum % progressMod1) == 0)
								&&
								(OnUpdateFileSplittingProgress != null)
								)
							{
								OnUpdateFileSplittingProgress(((double)(volNum + 1) / (double)this.dataCount) * 100.0);
							}

							// В случае, если требуется постановка на паузу, событие "executeEvent"
							// будет сброшено, и будем на паузе вплоть до его появления
							ManualResetEvent.WaitAll(this.executeEvent);

							// Если указано, что требуется выйти из потока - выходим
							if(ManualResetEvent.WaitAll(this.exitEvent, 0, false))
							{
								// Закрываем исходный и целевой файловые потоки
								if(fileStreamSource != null)
								{
									fileStreamSource.Close();
									fileStreamSource = null;
								}

								if(fileStreamTarget != null)
								{
									fileStreamTarget.Close();
									fileStreamTarget = null;
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

						continue;
					}

					// Если в исходном потоке данных больше нет, нужно просто заполнить
					// весь том нулями, не забыв про дополнительные 8 байт переменной,
					// указывающей на количество оригинальных данных
					if(unwrittenCounter == 0)
					{
						if(this.eSecurity != null)
						{
							// Расширяем размер файлового потока до "secVolumeLength"
							fileStreamTarget.SetLength(secVolumeLength);
						}
						else
						{
							// Расширяем размер файлового потока до "volumeLength"
							fileStreamTarget.SetLength(volumeLength);
						}

						// Выводим прогресс обработки
						if(
							((volNum % progressMod1) == 0)
							&&
							(OnUpdateFileSplittingProgress != null)
							)
						{
							OnUpdateFileSplittingProgress(((double)(volNum + 1) / (double)this.dataCount) * 100.0);
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

						continue;
					}
				}
			}

			// Если было хотя бы одно исключение - закрываем файловые потоки и
			// сообщаем об ошибке
			catch
			{
				// Указываем на то, что произошла ошибка работы с файлами
				this.processedOK = false;

				// Активируем индикатор актуального состояния переменных-членов
				this.finished = true;

				// Устанавливаем событие завершения обработки
				this.finishedEvent[0].Set();

				return;
			}

			finally
			{
				// Закрываем исходный и целевой файловые потоки
				if(fileStreamSource != null)
				{
					fileStreamSource.Close();
					fileStreamSource = null;
				}

				if(fileStreamTarget != null)
				{
					fileStreamTarget.Close();
					fileStreamTarget = null;
				}
			}

			// Указываем на то, что обработка была произведена корректно
			this.processedOK = true;

			// Активируем индикатор актуального состояния переменных-членов
			this.finished = true;

			// Устанавливаем событие завершения обработки
			this.finishedEvent[0].Set();
		}

		/// <summary>
		/// Склеивание файлов из фрагментов
		/// </summary>
		private void Glue()
		{
			// Экземпляры файловых потоков (исходный и целевой)
			FileStream fileStreamSource = null;
			FileStream fileStreamTarget = null;

			// Номер текущего тома
			int volNum;

			// Имя файла для обработки
			String fileName;

			try
			{
				// Формируем полное имя файла
				fileName = this.path + this.fileName;

				// Производим тест на наличие файла...
				if(File.Exists(fileName))
				{
					//...если таковой имеется, ставим на него атрибуты
					// по-умолчанию...
					File.SetAttributes(fileName, FileAttributes.Normal);
				}

				// ...затем открываем поток целевого файла на запись
				fileStreamTarget = new FileStream(fileName, FileMode.Create, System.IO.FileAccess.Write);

				// Вычисляем значение модуля, который позволит выводить процент обработки
				// ровно при единичном приращении для цикла по "volNum"
				int progressMod1 = this.dataCount / 100;

				// Если модуль равен нулю, то увеличиваем его до значения "1", чтобы
				// прогресс выводился на каждой итерации (файл очень маленький)
				if(progressMod1 == 0)
				{
					progressMod1 = 1;
				}

				// Считываем первоначальное имя файла
				fileName = this.fileName;

				// Упаковываем исходное имя файла в префиксный формат для получения имени первого тома
				this.eFileNamer.Pack(ref fileName, 0, this.dataCount, this.eccCount, this.codecType);

				// Формируем полное имя файла
				fileName = this.path + fileName;

				// Открываем поток исходного файла на чтение...
				fileStreamSource = new FileStream(fileName, FileMode.Open, System.IO.FileAccess.Read);

				// Вычисляем значение модуля, который позволит выводить процент обработки
				// ровно при единичном приращении для цикла по "i" внутри цикла по "volNum"
				int progressMod2 = (int)(((fileStreamSource.Length - 8) / this.bufferLength) / 100);

				// Если модуль равен нулю, то увеличиваем его до значения "1", чтобы
				// прогресс выводился на каждой итерации (файл очень маленький)
				if(progressMod2 == 0)
				{
					progressMod2 = 1;
				}

				// Закрываем поток исходного файла
				if(fileStreamSource != null)
				{
					fileStreamSource.Close();
					fileStreamSource = null;
				}

				// Устанавливаем выбранный пользователем размер буфера
				// (в режиме защиты данных он совпадает с размером CBC-блока AES-256)
				// Размер буфера указывается в килобайтах!
				if(this.eSecurity != null)
				{
					this.bufferLength = (this.cbcBlockSize * 1024);
				}

				// Выделяем память под файловый буфер
				this.buffer = new byte[this.bufferLength + bits256]; // bits256 - довесок для выравнивания потока CryptoStream

				// Работаем со всеми основными томами
				for(volNum = 0; volNum < this.dataCount; ++volNum)
				{
					// Считываем первоначальное имя файла
					fileName = this.fileName;

					// Упаковываем исходное имя файла в префиксный формат
					// (для перебора всех томов в цикле)
					this.eFileNamer.Pack(ref fileName, volNum, this.dataCount, this.eccCount, this.codecType);

					// Формируем полное имя файла
					fileName = this.path + fileName;

					// Если исходный файл не существует, сообщаем об ошибке
					if(!File.Exists(fileName))
					{
						// Указываем на то, что обработка была прервана
						this.processedOK = false;

						// Активируем индикатор актуального состояния переменных-членов
						this.finished = true;

						// Устанавливаем событие завершения обработки
						this.finishedEvent[0].Set();

						return;
					}

					// Открываем поток исходного файла на чтение...
					fileStreamSource = new FileStream(fileName, FileMode.Open, System.IO.FileAccess.Read);

					//...и выполняем позиционирование на конец файла, чтобы считать
					// количество полезных байт в данном томе
					fileStreamSource.Seek(((Int64)fileStreamSource.Length - (8 + 8)), SeekOrigin.Begin);

					// Буфер для преобразования байтового представления размера полезных данных
					byte[] dataLengthArr = new byte[8];

					// Читаем сохраненное в конце файла значение CRC-64...
					int dataLen = 8;
					int readed = 0;
					int toRead = 0;
					while((toRead = dataLen - (readed += fileStreamSource.Read(dataLengthArr, readed, toRead))) != 0) ;

					// Устанавливаем курсор в файле на начало
					fileStreamSource.Seek(0, SeekOrigin.Begin);

					// Сохраненное в файле значение количества полезных байт в данном томе
					UInt64 dataLength;

					// Теперь преобразуем массив byte[] в Int64
					dataLength = DataConverter.GetUInt64(dataLengthArr);

					// Теперь, когда мы знаем количество полезных байт в данном томе, их все нужно
					// записать в целевой файл
					// Узнаем количество основных итераций (копирование полным буфером)
					Int64 nIterations = (Int64)(dataLength / (UInt64)this.bufferLength);

					// Вычисляем остаток, не подпадающий под вычисления основных итераций
					int iterRest = (int)((Int64)dataLength - (nIterations * this.bufferLength));

					// Работа с полноразмерными буферами (работа в основных итерациях)
					for(Int64 i = 0; i < nIterations; i++)
					{
						if(this.eSecurity != null)
						{
							// Читаем данные в буфер (с учетом довеска)
							dataLen = (this.bufferLength + bits256);
							readed = 0;
							toRead = 0;
							while((toRead = dataLen - (readed += fileStreamSource.Read(this.buffer, readed, toRead))) != 0) ;

							// Расшифровываем данные, если это требуется...
							if(!this.eSecurity.Decrypt(this.buffer, (this.bufferLength + bits256)))
							{
								// Закрываем исходный и целевой файловые потоки
								if(fileStreamSource != null)
								{
									fileStreamSource.Close();
									fileStreamSource = null;
								}

								if(fileStreamTarget != null)
								{
									fileStreamTarget.Close();
									fileStreamTarget = null;
								}

								// Указываем на то, что произошла ошибка работы с файлами
								this.processedOK = false;

								// Активируем индикатор актуального состояния переменных-членов
								this.finished = true;

								// Устанавливаем событие завершения обработки
								this.finishedEvent[0].Set();

								return;
							}

							fileStreamTarget.Write(this.buffer, 0, this.bufferLength);
						}
						else
						{
							// Читаем данные в буфер
							dataLen = this.bufferLength;
							readed = 0;
							toRead = 0;
							while((toRead = dataLen - (readed += fileStreamSource.Read(this.buffer, readed, toRead))) != 0) ;

							//...а иначе пишем без расшифровки (из исходного в целевой)
							fileStreamTarget.Write(this.buffer, 0, this.bufferLength);
						}

						// Выводим прогресс обработки
						if(
							((((volNum * nIterations) + i) % progressMod2) == 0)
							&&
							(OnUpdateFileSplittingProgress != null)
							)
						{
							OnUpdateFileSplittingProgress(((double)((volNum * nIterations) + (i + 1)) / (double)(this.dataCount * nIterations)) * 100.0);
						}

						// В случае, если требуется постановка на паузу, событие "executeEvent"
						// будет сброшено, и будем на паузе вплоть до его появления
						ManualResetEvent.WaitAll(this.executeEvent);

						// Если указано, что требуется выйти из потока - выходим
						if(ManualResetEvent.WaitAll(this.exitEvent, 0, false))
						{
							// Закрываем исходный и целевой файловые потоки
							if(fileStreamSource != null)
							{
								fileStreamSource.Close();
								fileStreamSource = null;
							}

							if(fileStreamTarget != null)
							{
								fileStreamTarget.Close();
								fileStreamTarget = null;
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

					// Дорабатываем остаток (если он есть)
					if(iterRest > 0)
					{
						if(this.eSecurity != null)
						{
							// Первоначально размер последнего блока расшифрованных данных
							// полагаем равный недовыработанному остатку, но затем его
							// корректируем на основании требований к кратности 256 битам.
							int lastBlockSize = iterRest;

							// Если установлен режим обеспечения защиты данных, необходимо
							// обеспечить кратность bits256 (это выравнивание по размеру данных
							// шифроблока, 256 бит)
							if((lastBlockSize % bits256) != 0)
							{
								lastBlockSize += (bits256 - (lastBlockSize % bits256));
							}

							// Читаем данные в буфер
							dataLen = (lastBlockSize + bits256);
							readed = 0;
							toRead = 0;
							while((toRead = dataLen - (readed += fileStreamSource.Read(this.buffer, readed, toRead))) != 0) ;

							//...расшифровываем данные...
							if(!this.eSecurity.Decrypt(this.buffer, (lastBlockSize + bits256)))
							{
								// Закрываем исходный и целевой файловые потоки
								if(fileStreamSource != null)
								{
									fileStreamSource.Close();
									fileStreamSource = null;
								}

								if(fileStreamTarget != null)
								{
									fileStreamTarget.Close();
									fileStreamTarget = null;
								}

								// Указываем на то, что произошла ошибка работы с файлами
								this.processedOK = false;

								// Активируем индикатор актуального состояния переменных-членов
								this.finished = true;

								// Устанавливаем событие завершения обработки
								this.finishedEvent[0].Set();

								return;
							}

							//...и пишем в целевой файл уже без выравнивания!
							fileStreamTarget.Write(this.buffer, 0, iterRest);
						}
						else
						{
							// Читаем данные в буфер
							dataLen = iterRest;
							readed = 0;
							toRead = 0;
							while((toRead = dataLen - (readed += fileStreamSource.Read(this.buffer, readed, toRead))) != 0) ;

							//...и пишем в целевой файл
							fileStreamTarget.Write(this.buffer, 0, iterRest);
						}

						// Выводим прогресс обработки
						if(
							((volNum % progressMod1) == 0)
							&&
							(OnUpdateFileSplittingProgress != null)
							)
						{
							OnUpdateFileSplittingProgress(((double)(volNum + 1) / (double)this.dataCount) * 100.0);
						}

						// В случае, если требуется постановка на паузу, событие "executeEvent"
						// будет сброшено, и будем на паузе вплоть до его появления
						ManualResetEvent.WaitAll(this.executeEvent);

						// Если указано, что требуется выйти из потока - выходим
						if(ManualResetEvent.WaitAll(this.exitEvent, 0, false))
						{
							// Закрываем исходный и целевой файловые потоки
							if(fileStreamSource != null)
							{
								fileStreamSource.Close();
								fileStreamSource = null;
							}

							if(fileStreamTarget != null)
							{
								fileStreamTarget.Close();
								fileStreamTarget = null;
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

					// Закрываем файл исходного тома
					if(fileStreamSource != null)
					{
						fileStreamSource.Close();
						fileStreamSource = null;
					}
				}

				// Сообщаем, что обработка файла закончена
				if(OnFileSplittingFinish != null)
				{
					OnFileSplittingFinish();
				}
			}

			// Если было хотя бы одно исключение - закрываем файловые потоки и
			// сообщаем об ошибке
			catch
			{
				// Указываем на то, что произошла ошибка работы с файлами
				this.processedOK = false;

				// Активируем индикатор актуального состояния переменных-членов
				this.finished = true;

				// Устанавливаем событие завершения обработки
				this.finishedEvent[0].Set();

				return;
			}

			finally
			{
				// Закрываем исходный и целевой файловые потоки
				if(fileStreamSource != null)
				{
					fileStreamSource.Close();
					fileStreamSource = null;
				}

				if(fileStreamTarget != null)
				{
					fileStreamTarget.Flush();
					fileStreamTarget.Close();
					fileStreamTarget = null;
				}
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