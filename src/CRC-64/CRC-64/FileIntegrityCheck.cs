/*----------------------------------------------------------------------+
 |  filename:   FileIntegrityCheck.cs                                   |
 |----------------------------------------------------------------------|
 |  version:    2.21                                                    |
 |  revision:   24.08.2012 15:52                                        |
 |  authors:    Дробанов Артём Федорович (DrAF),                        |
 |              RUSpectrum (г. Оренбург).                               |
 |  e-mail:     draf@mail.ru                                            |
 |  purpose:    Контроль целостности данных                             |
 +----------------------------------------------------------------------*/

using System;
using System.Threading;
using System.IO;

namespace RecoveryStar
{
	/// <summary>
	/// Класс вычисления и проверки целостности файла на основе CRC-64
	/// </summary>
	public class FileIntegrityCheck
	{
		#region Delegates

		/// <summary>
		/// Делегат обновления прогресса контроля целостности файла
		/// </summary>
		public OnUpdateDoubleValueHandler OnUpdateFileIntegrityCheckProgress;

		/// <summary>
		/// Делегат завершения процесса контроля целостности файла
		/// </summary>
		public OnEventHandler OnFileIntegrityCheckFinish;

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
					(this.thrFileIntegrityCheck != null)
					&&
					(
						(this.thrFileIntegrityCheck.ThreadState == ThreadState.Running)
						||
						(this.thrFileIntegrityCheck.ThreadState == ThreadState.WaitSleepJoin)
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
		/// Булевское свойство "CRC-64 файла вычислено корректно?"
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
		/// CRC-64 вычислено корректно?
		/// </summary>
		private bool processedOK;

		/// <summary>
		/// Имя файла, обработанного последним
		/// </summary>
		public String FullFilename
		{
			get
			{
				// Если класс не занят обработкой - возвращаем значение...
				if(!InProcessing)
				{
					return this.fullFilename;
				}
				else
				{
					//...а иначе сообщаем об обратном
					return null;
				}
			}
		}

		/// <summary>
		/// Имя файла для обработки
		/// </summary>
		private String fullFilename;

		/// <summary>
		/// Имя файла, обработанного последним
		/// </summary>
		public UInt64 CRC64
		{
			get
			{
				// Если класс не занят обработкой - возвращаем реальное значение...
				if(!InProcessing)
				{
					return this.crc64;
				}
				else
				{
					///...а иначе сообщаем об обратном
					return 0xFFFFFFFFFFFFFFFF;
				}
			}
		}

		/// <summary>
		/// Значение CRC-64, соответствующее "fullFilename"
		/// </summary>
		private UInt64 crc64;

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
					(this.thrFileIntegrityCheck != null)
					&&
					(this.thrFileIntegrityCheck.IsAlive)
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
					this.thrFileIntegrityCheck.Priority = this.threadPriority;
				}
			}
		}

		/// <summary>
		/// Приоритет процесса расчета CRC-64
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

		#endregion Data & Public Properties

		#region Data

		/// <summary>
		/// Экземпляр класса расчета CRC-64
		/// </summary>
		private CRC64 eCRC64;

		/// <summary>
		/// Файловый буфер
		/// </summary>
		private byte[] buffer;

		/// <summary>
		/// Поток вычисления CRC-64 файла "fullFilename" с сохранением результата в "processedOK"
		/// </summary>
		private Thread thrFileIntegrityCheck;

		/// <summary>
		/// Событие прекращения обработки файла
		/// </summary>
		private ManualResetEvent[] exitEvent;

		/// <summary>
		/// Событие продолжения обработки файла
		/// </summary>
		private ManualResetEvent[] executeEvent;

		#endregion Data

		#region Construction

		/// <summary>
		/// Конструктор класса
		/// </summary>
		public FileIntegrityCheck()
		{
			// Создаем экземпляр класса расчета CRC-64
			this.eCRC64 = new CRC64();

			// Инициализируем имя файла по-умолчанию
			this.fullFilename = "NONAME";

			// Выделяем память под файловый буфер
			this.buffer = new byte[this.bufferLength];

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

			// Событие, устанавливаемое по завершении обработки
			this.finishedEvent = new ManualResetEvent[] {new ManualResetEvent(true)};
		}

		#endregion Construction

		#region Public Operations

		/// <summary>
		/// Метод запуска потока обработки вычисления и записи CRC64 в конец файла
		/// </summary>
		/// <param name="fullFilename">Имя файла для обработки</param>
		/// <param name="runAsSeparateThread">Запускать в отдельном потоке?</param>
		/// <returns>Булевский флаг операции</returns>
		public bool StartToWriteCRC64(String fullFilename, bool runAsSeparateThread)
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

			if(fullFilename == null)
			{
				// Активируем индикатор актуального состояния переменных-членов
				this.finished = true;

				return false;
			}

			// Если исходный файл не существует, сообщаем об ошибке
			if(!File.Exists(fullFilename))
			{
				// Активируем индикатор актуального состояния переменных-членов
				this.finished = true;

				return false;
			}

			// Сохраняем имя файла
			this.fullFilename = fullFilename;

			// Указываем, что поток должен выполняться
			this.exitEvent[0].Reset();
			this.executeEvent[0].Set();
			this.finishedEvent[0].Reset();

			// Если указано, что не требуется запуск в отдельном потоке,
			// запускаем в данном
			if(!runAsSeparateThread)
			{
				// Запускаем вычисление и запись CRC-64 в конец файла
				WriteCRC64();

				// Возвращаем результат обработки
				return this.processedOK;
			}

			// Создаем поток вычисления и записи CRC-64...
			this.thrFileIntegrityCheck = new Thread(new ThreadStart(WriteCRC64));

			//...затем даем ему имя...
			this.thrFileIntegrityCheck.Name = "FileIntegrityCheck.WriteCRC64()";

			//...устанавливаем выбранный приоритет задачи...
			this.thrFileIntegrityCheck.Priority = this.threadPriority;

			//...и запускаем его
			this.thrFileIntegrityCheck.Start();

			// Сообщаем, что все нормально
			return true;
		}

		/// <summary>
		/// Метод запуска потока обработки проверки CRC64, записанного в конец файла
		/// </summary>
		/// <param name="fullFilename">Имя файла для обработки</param>
		/// <param name="runAsSeparateThread">Запускать в отдельном потоке?</param>
		/// <returns>Булевский флаг операции</returns>
		public bool StartToCheckCRC64(String fullFilename, bool runAsSeparateThread)
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

			if(fullFilename == null)
			{
				// Активируем индикатор актуального состояния переменных-членов
				this.finished = true;

				// Устанавливаем событие завершения обработки
				this.finishedEvent[0].Set();

				return false;
			}

			// Если исходный файл не существует, сообщаем об ошибке
			if(!File.Exists(fullFilename))
			{
				// Активируем индикатор актуального состояния переменных-членов
				this.finished = true;

				// Устанавливаем событие завершения обработки
				this.finishedEvent[0].Set();

				return false;
			}

			// Сохраняем имя файла
			this.fullFilename = fullFilename;

			// Указываем, что поток должен выполняться
			this.exitEvent[0].Reset();
			this.executeEvent[0].Set();
			this.finishedEvent[0].Reset();

			// Если указано, что не требуется запуск в отдельном потоке,
			// запускаем в данном
			if(!runAsSeparateThread)
			{
				// Запускаем вычисление и проверку значения CRC-64
				CheckCRC64();

				// Возвращаем результат обработки
				return this.processedOK;
			}

			// Создаем поток вычисления и проверки CRC-64...
			this.thrFileIntegrityCheck = new Thread(new ThreadStart(CheckCRC64));

			//...затем даем ему имя...
			this.thrFileIntegrityCheck.Name = "FileIntegrityCheck.CheckCRC64()";

			//...устанавливаем выбранный приоритет задачи...
			this.thrFileIntegrityCheck.Priority = this.threadPriority;

			//...и запускаем его
			this.thrFileIntegrityCheck.Start();

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
		/// Вычисление CRC-64 указанного файла
		/// </summary>
		/// <param name="fullFileName">Имя файла для обработки</param>
		/// <param name="endOffset">Смещение, "недорабатываемое" при вычислениях, с конца файла</param>
		/// <returns>Булевский флаг операции</returns>
		public bool CalcCRC64(String fullFileName, int endOffset)
		{
			try
			{
				FileInfo fileInfo = new FileInfo(fullFileName);
				if(!fileInfo.Exists) return false;
				if(fileInfo.Length <= endOffset) return false;
				return CalcCRC64(fileInfo, endOffset);
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Вычисление CRC-64 файла
		/// </summary>
		/// <param name="fileInfo">Объект, хранящий информацию о файле для обработки</param>
		/// <param name="endOffset">Смещение, "недорабатываемое" при вычислениях, с конца потока</param>
		/// <returns>Булевский флаг операции</returns>
		public bool CalcCRC64(FileInfo fileInfo, int endOffset)
		{
			try
			{
				using(FileStream fileStream = fileInfo.OpenRead()) return CalcCRC64(fileStream, endOffset, fileInfo.Length);
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Вычисление CRC-64 из потока
		/// </summary>
		/// <param name="stream">Исходный поток</param>
		/// <param name="endOffset">Смещение, "недорабатываемое" при вычислениях, с конца потока</param>
		/// <param name="length">Длина "зоны интереса" в пределах потока</param>
		/// <returns>Булевский флаг операции</returns>
		public bool CalcCRC64(Stream stream, int endOffset, long length)
		{
			try
			{
				// dataLength - длина данных, подлежащих обработке (длина логического блока минус смещение с конца)
				long dataLength = length - endOffset;
				int needRead = 0;
				int readLength = 0;

				// Сбрасываем CRC-64 в начальное значение
				this.eCRC64.Reset();

				while((needRead = (dataLength < this.bufferLength) ? (int)dataLength : this.bufferLength) > 0 && (readLength = stream.Read(this.buffer, 0, needRead)) > 0)
				{
					// Вычисляем CRC-64 с того буфера, который удалось считать
					this.eCRC64.Calculate(this.buffer, 0, readLength);

					// Уменьшаем размер данных, подлежащих обработке
					dataLength -= readLength;
				}

				this.crc64 = this.eCRC64.Value;

				return true;
			}

			catch
			{
				return false;
			}

			finally
			{
				try
				{
					if(stream != null) stream.Close();
				}
				catch
				{
				}
			}
		}

		/// <summary>
		/// Вычисление и запись в конец файла значения CRC-64
		/// </summary>
		private void WriteCRC64()
		{
			// Начальное значение CRC-64
			this.crc64 = 0xFFFFFFFFFFFFFFFF;

			// Если вычисление CRC-64 с данного файла прошло корректно...
			if(CalcCRC64(this.fullFilename, 0))
			{
				// Экземпляр файлового потока
				FileStream eFileStream = null;

				// Экземпляр класса записи обычных типов данных в двоичный файл
				BinaryWriter eBinaryWriter = null;

				try
				{
					// Производим тест на наличие файла...
					if(File.Exists(this.fullFilename))
					{
						//...если таковой имеется, ставим на него атрибуты
						// по-умолчанию...
						File.SetAttributes(this.fullFilename, FileAttributes.Normal);
					}

					//...открываем файловый поток на запись...
					eFileStream = new FileStream(this.fullFilename, FileMode.Append, System.IO.FileAccess.Write);

					//...и используем его для инициализации экземпляра класса записи
					// обычных типов данных в двоичный файл
					eBinaryWriter = new BinaryWriter(eFileStream);

					// Перемещаемся на конец файла...
					eBinaryWriter.Seek(0, SeekOrigin.End);

					//...и пишем в его конец вычисленное значение CRC-64,...
					eBinaryWriter.Write(this.crc64);

					//...сливаем файловый буфер...
					eBinaryWriter.Flush();

					//...и закрываем файл
					if(eBinaryWriter != null)
					{
						eBinaryWriter.Close();
						eBinaryWriter = null;
					}
				}

					// Если было хотя бы одно исключение - закрываем файловый поток и
					// сообщаем об ошибке
				catch
				{
					// Закрываем файл
					if(eBinaryWriter != null)
					{
						eBinaryWriter.Close();
						eBinaryWriter = null;
					}

					// Сбрасываем флаг корректности результата
					this.processedOK = false;

					// Активируем индикатор актуального состояния переменных-членов
					this.finished = true;

					// Устанавливаем событие завершения обработки
					this.finishedEvent[0].Set();

					return;
				}

				// Указываем на то, что CRC-64 у данного файла было вычислено корректно
				this.processedOK = true;
			}
			else
			{
				// Указываем на то, что CRC-64 у данного файла было вычислено некорректно
				this.processedOK = false;
			}

			// Активируем индикатор актуального состояния переменных-членов
			this.finished = true;

			// Устанавливаем событие завершения обработки
			this.finishedEvent[0].Set();
		}

		/// <summary>
		/// Вычисление и проверка значения CRC-64, записанного в конце файла
		/// </summary>
		private void CheckCRC64()
		{
			// Начальное значение CRC-64
			this.crc64 = 0xFFFFFFFFFFFFFFFF;

			// Массив, хранящий байты считываемой CRC-64
			byte[] crc64Arr = new byte[8];

			// Сохраненное в файле значение CRC-64:
			UInt64 crc64;

			// Экземпляр файлового потока
			FileStream eFileStream = null;

			// Если вычисление CRC-64 с данного файла со смещением "8" прошло корректно...
			if(CalcCRC64(this.fullFilename, 8))
			{
				try
				{
					//...то открываем файловый поток на чтение...
					eFileStream = new FileStream(this.fullFilename, FileMode.Open, System.IO.FileAccess.Read);

					//...и выполняем позиционирование на конец файла, чтобы считать значение CRC-64
					eFileStream.Seek(-8, SeekOrigin.End);

					// Читаем сохраненное в конце файла значение CRC-64...
					int readed = 0;
					int toRead = 8;
					while((toRead -= (readed += eFileStream.Read(crc64Arr, readed, toRead))) != 0) ;

					//...и закрываем файл
					if(eFileStream != null)
					{
						eFileStream.Close();
						eFileStream = null;
					}
				}

					// Если было хотя бы одно исключение - закрываем файловый поток и
					// сообщаем об ошибке
				catch
				{
					if(eFileStream != null)
					{
						eFileStream.Close();
						eFileStream = null;
					}

					// Сбрасываем флаг корректности результата
					this.processedOK = false;

					// Активируем индикатор актуального состояния переменных-членов
					this.finished = true;

					// Устанавливаем событие завершения обработки
					this.finishedEvent[0].Set();

					return;
				}

				// Теперь преобразуем массив byte[] в UInt64
				crc64 = DataConverter.GetUInt64(crc64Arr);

				// Если вычисленное значение CRC-64 не совпало с сохраненным,
				// указываем на ошибку
				this.processedOK = (this.crc64 == crc64);
			}
			else
			{
				// Указываем на то, что чтение CRC-64 файла прошло не корректно
				this.processedOK = false;
			}

			// Активируем индикатор актуального состояния переменных-членов
			this.finished = true;

			// Устанавливаем событие завершения обработки
			this.finishedEvent[0].Set();
		}

		#endregion Private Operations
	}
}