/*----------------------------------------------------------------------+
 |  filename:   ProcessForm.cs                                          |
 |----------------------------------------------------------------------|
 |  version:    2.22                                                    |
 |  revision:   02.04.2013 17:00                                        |
 |  authors:    Дробанов Артём Федорович (DrAF),                        |
 |              RUSpectrum (г. Оренбург).                               |
 |  e-mail:     draf@mail.ru                                            |
 |  purpose:    Работа с файлами                                        |
 +----------------------------------------------------------------------*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.IO;

namespace RecoveryStar
{
	public partial class ProcessForm : Form
	{
		#region Public Properties & Data

		/// <summary>
		/// Файловый браузер
		/// </summary>
		public FileBrowser.Browser Browser { get; set; }

		/// <summary>
		/// Криптографическая защита данных
		/// </summary>
		public Security Security
		{
			get
			{
				if(this.eRecoveryStarCore != null)
				{
					return this.eRecoveryStarCore.Security;
				}
				else
				{
					return null;
				}
			}

			set
			{
				if(this.eRecoveryStarCore != null)
				{
					this.eRecoveryStarCore.Security = value;
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
				if(this.eRecoveryStarCore != null)
				{
					return this.eRecoveryStarCore.CBCBlockSize;
				}
				else
				{
					return -1;
				}
			}

			set
			{
				if(this.eRecoveryStarCore != null)
				{
					this.eRecoveryStarCore.CBCBlockSize = value;
				}
			}
		}

		/// <summary>
		/// Список полных имен файлов для обработки
		/// </summary>
		public ArrayList FileNamesToProcess
		{
			get { return this.fileNamesToProcess; }

			set { this.fileNamesToProcess = value; }
		}

		/// <summary>
		/// Список полных имен файлов для обработки
		/// </summary>
		private ArrayList fileNamesToProcess;

		/// <summary>
		/// Количество основных томов
		/// </summary>
		public int DataCount
		{
			get { return this.dataCount; }

			set { this.dataCount = value; }
		}

		/// <summary>
		/// Количество основных томов
		/// </summary>
		private int dataCount;

		/// <summary>
		/// Количество томов для восстановления
		/// </summary>
		public int EccCount
		{
			get { return this.eccCount; }

			set { this.eccCount = value; }
		}

		/// <summary>
		/// Количество томов для восстановления
		/// </summary>
		private int eccCount;

		/// <summary>
		/// Тип кодека (по типу используемой матрицы)
		/// </summary>
		public int CodecType
		{
			get { return this.codecType; }

			set { this.codecType = value; }
		}

		/// <summary>
		/// Тип кодека Рида-Соломона (по типу используемой матрицы кодирования)
		/// </summary>
		private int codecType;

		/// <summary>
		/// Используемый режим обработки данных
		/// </summary>
		public RSMode Mode
		{
			get { return this.mode; }

			set { this.mode = value; }
		}

		/// <summary>
		/// Используемый режим обработки
		/// </summary>
		private RSMode mode;

		/// <summary>
		/// Используется быстрое извлечение из томов (без проверки CRC-64)?
		/// </summary>
		public bool FastExtraction
		{
			get { return this.fastExtraction; }

			set { this.fastExtraction = value; }
		}

		/// <summary>
		/// Используется быстрое извлечение из томов (без проверки CRC-64)?
		/// </summary>
		private bool fastExtraction;

		#endregion Public Properties & Data

		#region Data

		/// <summary>
		/// Экземпляр класса для упаковки (распаковки) имени файла в префиксный формат
		/// </summary>
		private FileNamer eFileNamer;

		/// <summary>
		/// Ядро системы отказоустойчивого кодирования
		/// </summary>
		private RecoveryStarCore eRecoveryStarCore;

		/// <summary>
		/// Счетчик корректно обработанных файлов
		/// </summary>
		private int OKCount;

		/// <summary>
		/// Счетчик некорректно обработанных файлов
		/// </summary>
		private int errorCount;

		/// <summary>
		/// Поток обработки данных
		/// </summary>
		private Thread thrRecoveryStarProcess;

		/// <summary>
		/// Приоритет процесса обработки данных
		/// </summary>
		private ThreadPriority threadPriority;

		/// <summary>
		/// Событие прекращения обработки
		/// </summary>
		private ManualResetEvent[] exitEvent;

		/// <summary>
		/// Событие продолжения обработки
		/// </summary>
		private ManualResetEvent[] executeEvent;

		/// <summary>
		/// Событие "пробуждения" цикла ожидания
		/// </summary>
		private ManualResetEvent[] wakeUpEvent;

		/// <summary>
		/// Текст для вывода при обновлении прогресса (выводится таймером)
		/// </summary>
		private String processGroupBoxText;

		/// <summary>
		/// Значение прогресса (выводится таймером)
		/// </summary>
		private int processProgressBarValue;

		/// <summary>
		/// Семафор для предотвращения конфликтов читатель/писатель при работе со статистикой
		/// </summary>
		private Semaphore processStatSema;

		#endregion Data

		#region Construction & Destruction

		/// <summary>
		/// Конструктор формы
		/// </summary>
		public ProcessForm()
		{
			InitializeComponent();

			// По-умолчанию режим обработки не установлен
			this.mode = RSMode.None;

			// Инициализируем экземпляр класса для упаковки (распаковки) имени файла
			// в префиксный формат
			this.eFileNamer = new FileNamer();

			// Создаем экземпляр класса ядра RecoveryStar
			this.eRecoveryStarCore = new RecoveryStarCore();

			// Подписываемся на требуемых делегатов
			this.eRecoveryStarCore.OnUpdateFileSplittingProgress = new OnUpdateDoubleValueHandler(OnUpdateFileSplittingProgress);
			this.eRecoveryStarCore.OnFileSplittingFinish = new OnEventHandler(OnFileSplittingFinish);
			this.eRecoveryStarCore.OnUpdateRSMatrixFormingProgress = new OnUpdateDoubleValueHandler(OnUpdateRSMatrixFormingProgress);
			this.eRecoveryStarCore.OnRSMatrixFormingFinish = new OnEventHandler(OnRSMatrixFormingFinish);
			this.eRecoveryStarCore.OnUpdateFileStreamsOpeningProgress = new OnUpdateDoubleValueHandler(OnUpdateFileStreamsOpeningProgress);
			this.eRecoveryStarCore.OnFileStreamsOpeningFinish = new OnEventHandler(OnFileStreamsOpeningFinish);
			this.eRecoveryStarCore.OnStartedRSCoding = new OnEventHandler(OnStartedRSCoding);
			this.eRecoveryStarCore.OnUpdateFileCodingProgress = new OnUpdateDoubleValueHandler(OnUpdateFileCodingProgress);
			this.eRecoveryStarCore.OnFileCodingFinish = new OnEventHandler(OnFileCodingFinish);
			this.eRecoveryStarCore.OnUpdateFileStreamsClosingProgress = new OnUpdateDoubleValueHandler(OnUpdateFileStreamsClosingProgress);
			this.eRecoveryStarCore.OnFileStreamsClosingFinish = new OnEventHandler(OnFileStreamsClosingFinish);
			this.eRecoveryStarCore.OnUpdateFileAnalyzeProgress = new OnUpdateDoubleValueHandler(OnUpdateFileAnalyzeProgress);
			this.eRecoveryStarCore.OnFileAnalyzeFinish = new OnEventHandler(OnFileAnalyzeFinish);
			this.eRecoveryStarCore.OnGetDamageStat = new OnUpdateTwoIntDoubleValueHandler(OnGetDamageStat);

			// Начальное значение семафора - 1, максимум 1 блок.
			this.processStatSema = new Semaphore(1, 1);

			// Инициализируем список файлов для обработки
			this.fileNamesToProcess = new ArrayList();

			// Считываем значение с элемента управления, ответственного за
			// приоритет процесса обработки данных
			SetThreadPriority(processPriorityComboBox.SelectedIndex);

			// Инициализируем событие прекращения обработки файла
			this.exitEvent = new ManualResetEvent[] {new ManualResetEvent(false)};

			// Инициализируем cобытие продолжения обработки файла
			this.executeEvent = new ManualResetEvent[] {new ManualResetEvent(false)};

			// Инициализируем cобытие "пробуждения" цикла ожидания
			this.wakeUpEvent = new ManualResetEvent[] {new ManualResetEvent(false)};

			// Устанавливаем значение по-умолчанию для приоритета
			processPriorityComboBox.Text = "По-умолчанию";

			this.processProgressBarValue = -1;
		}

		#endregion Construction & Destruction

		#region Public Operations

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
		/// Поток обработки данных
		/// </summary>
		private void Process()
		{
			// Активируем таймер вывода прогресса обработки
			this.Invoke(((OnEventHandler)delegate() { processTimer.Start(); }), new object[] {});

			// Номер обрабатываемого файла
			int fileNum = 0;

			// Сбрасываем статистику обработки
			this.OKCount = 0;
			this.errorCount = 0;

			// Строка, хранящая значение общего количества обрабатываемых файлов
			String filesTotal = Convert.ToString(this.fileNamesToProcess.Count);

			// Строка, хранящая текст для вывода на экран
			String textToOut = "";

			// Обрабатываем все файлы из представленного списка
			foreach(String fullFileName in this.fileNamesToProcess)
			{
				// Получаем короткий вариант длинного имени
				String shortFileName = this.eFileNamer.GetShortFileName(fullFileName);

				// Имя файла для вывода на экран
				String unpackedFileName = shortFileName;

				// Если используется режим не защиты данных, а другой,
				// требуется распаковка имени из префиксного формата
				if(this.mode != RSMode.Protect)
				{
					// Распаковываем короткий вариант имени с получением оригинального
					unpackedFileName = shortFileName;

					// Если не удалось корректно распаковать короткое имя - переходим
					// на следующую итерацию
					if(!this.eFileNamer.Unpack(ref unpackedFileName))
					{
						continue;
					}
				}

				// Подготавливаем текст для вывода в заголовок формы
				switch(this.mode)
				{
					case RSMode.Protect:
						{
							textToOut = " Защита файла \"";
							break;
						}

					case RSMode.Recover:
						{
							textToOut = " Извлечение файла \"";
							break;
						}

					case RSMode.Repair:
						{
							textToOut = " Лечение томов файла \"";
							break;
						}

					default:
					case RSMode.Test:
						{
							textToOut = " Тестирование файла \"";
							break;
						}
				}

				textToOut += unpackedFileName + "\" (" + Convert.ToString(++fileNum) + " из " + filesTotal + ")";

				// Выводим текст в заголовок формы
				this.Invoke(((OnUpdateStringValueHandler)delegate(String value) { this.Text = value; }), new object[] {textToOut});

				// Осуществляем выбранную обработку
				switch(this.mode)
				{
					case RSMode.Protect:
						{
							// Отключаем те элементы управления, которые не будут
							// использоваться в контексте текущего процесса
							fileAnalyzeStatGroupBox.Invoke(((OnEventHandler)delegate() { fileAnalyzeStatGroupBox.Enabled = false; }), new object[] {});
							percOfDamageLabel_.Invoke(((OnEventHandler)delegate() { percOfDamageLabel_.Enabled = false; }), new object[] {});
							percOfAltEccLabel_.Invoke(((OnEventHandler)delegate() { percOfAltEccLabel_.Enabled = false; }), new object[] {});
							percOfDamageLabel.Invoke(((OnEventHandler)delegate() { percOfDamageLabel.Enabled = false; }), new object[] {});
							percOfAltEccLabel.Invoke(((OnEventHandler)delegate() { percOfAltEccLabel.Enabled = false; }), new object[] {});

							// Запускаем отказоустойчивое кодирование
							this.eRecoveryStarCore.StartToProtect(fullFileName, this.dataCount, this.eccCount, this.codecType, true);

							break;
						}

					case RSMode.Recover:
						{
							// Запускаем восстановление данных
							this.eRecoveryStarCore.StartToRecover(fullFileName, this.fastExtraction, true);

							break;
						}

					case RSMode.Repair:
						{
							// Запускаем лечение данных
							this.eRecoveryStarCore.StartToRepair(fullFileName, this.fastExtraction, true);

							break;
						}

					default:
					case RSMode.Test:
						{
							// Запускаем тестирование данных
							this.eRecoveryStarCore.StartToTest(fullFileName, this.fastExtraction, true);

							break;
						}
				}

				// Ждем окончания обработки
				while(true)
				{
					// Если не обнаружили установленного события "executeEvent",
					// то пользователь хочет, чтобы мы поставили обработку на паузу -
					if(!ManualResetEvent.WaitAll(this.executeEvent, 0, false))
					{
						//...приостанавливаем работу контролируемого алгоритма...
						this.eRecoveryStarCore.Pause();

						//...и сами засыпаем
						ManualResetEvent.WaitAll(this.executeEvent);

						// А когда проснулись, указываем, что обработка должна продолжаться
						this.eRecoveryStarCore.Continue();
					}

					// Ждем любое из перечисленных событий...
					int eventIdx = ManualResetEvent.WaitAny(new ManualResetEvent[] {this.wakeUpEvent[0], this.exitEvent[0], this.eRecoveryStarCore.FinishedEvent[0]});

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
						this.eRecoveryStarCore.Stop();

						return;
					}

					//...если получили сигнал о завершении обработки вложенным алгоритмом...
					if(eventIdx == 2)
					{
						//...выходим из цикла ожидания завершения (этого и ждали в while(true)!)
						break;
					}
				} // while(true)

				// В связи с закрытием большого количества файловых потоков
				// необходимо дождаться записи изменений, внесенных потоком
				// кодирования в тело класса. Поток уже не работает, но
				// установленное им булевское свойство, возможно, ещё
				// "не проявилось"
				for(int i = 0; i < (int)WaitCount.MaxWaitCount; i++)
				{
					if(!this.eRecoveryStarCore.Finished)
					{
						Thread.Sleep((int)WaitTime.MinWaitTime);
					}
					else
					{
						break;
					}
				}

				// Производим проверку на корректность обработки
				if(this.eRecoveryStarCore.ProcessedOK)
				{
					// Если обработка файла произошла корректно...
					OnUpdateLogListBox(this.Text.Substring(1) + ": OK!");

					// Изменяем статистику
					this.OKCount++;

					// Формируем текст для вывода на форму
					textToOut = Convert.ToString(this.OKCount);

					okCountLabel.Invoke(((OnUpdateStringValueHandler)delegate(String value) { okCountLabel.Text = value; }), new object[] {textToOut});
				}
				else
				{
					// Если обработка файла произошла некорректно...
					OnUpdateLogListBox(this.Text.Substring(1) + ": Error!");

					// Изменяем статистику
					this.errorCount++;

					// Формируем текст для вывода на форму
					textToOut = Convert.ToString(this.errorCount);

					errorCountLabel.Invoke(((OnUpdateStringValueHandler)delegate(String value) { errorCountLabel.Text = value; }), new object[] {textToOut});
				}

				// Создаем пробел для отделения блоков строк друг от друга
				OnUpdateLogListBox("");
			}

			// Если используется режим восстановления данных и обработка прошла корректно, то
			// после извлечения все ненужные файлы удаляются
			if(
				(this.mode == RSMode.Recover)
				&&
				(this.eRecoveryStarCore.ProcessedOK)
				)
			{
				try
				{
					foreach(String fullFileName in this.fileNamesToProcess)
					{
						// Производим выделение пути из полного имени файла
						String path = this.eFileNamer.GetPath(fullFileName);

						// Производим выделение имени из полного имени файла
						String fileName = this.eFileNamer.GetShortFileName(fullFileName);

						// Если имя корректно распаковывается - оно подлежит удалению,
						// т.к. является уже не нужным фрагментом многотомной структуры
						if(!this.eFileNamer.Unpack(ref fileName, ref this.dataCount, ref this.eccCount, ref this.codecType))
						{
							continue;
						}

						// Обрабатываем все файлы
						for(int i = 0; i < (this.dataCount + this.eccCount); i++)
						{
							// Считываем первоначальное имя файла,...
							String volumeName = fileName;

							//...упаковываем его в префиксный формат...
							this.eFileNamer.Pack(ref volumeName, i, this.dataCount, this.eccCount, this.codecType);

							//...формируем полное имя файла...
							volumeName = path + volumeName;

							//...производим тест на наличие файла...
							if(File.Exists(volumeName))
							{
								//...если таковой имеется, ставим на него атрибуты
								// по-умолчанию...
								File.SetAttributes(volumeName, FileAttributes.Normal);

								//...и затем удаляем
								File.Delete(volumeName);
							}
						}
					}
				}
				catch
				{
				}
			}

			// Формируем текст для вывода...
			textToOut = "Закрыть";

			//...изменяем надпись на кнопке прекращения обработки...
			stopButtonXP.Invoke(((OnUpdateStringValueHandler)delegate(String value) { stopButtonXP.Text = value; }), new object[] {textToOut});

			//...отключаем кнопку "Пауза"...
			pauseButtonXP.Invoke(((OnEventHandler)delegate() { pauseButtonXP.Enabled = false; }), new object[] {});

			//...и таймер обновления прогресса...
			Thread.Sleep(2 * processTimer.Interval);
			this.Invoke(((OnEventHandler)delegate() { processTimer.Stop(); }), new object[] {});

			//...и выпадающий список выбора приоритета процесса...
			processPriorityComboBox.Invoke(((OnEventHandler)delegate() { processPriorityComboBox.Enabled = false; }), new object[] {});

			//...но включаем кнопку закрытия формы
			stopButtonXP.Invoke(((OnEventHandler)delegate() { stopButtonXP.Enabled = true; }), new object[] {});
		}

		/// <summary>
		/// Метод установки приоритета процесса обработки на основании переданного значения int
		/// </summary>
		/// <param name="value">Код приоритета процесса</param>
		private void SetThreadPriority(int value)
		{
			if(
				(this.thrRecoveryStarProcess != null)
				&&
				(this.thrRecoveryStarProcess.IsAlive)
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

				// Устанавливаем выбранный приоритет
				this.thrRecoveryStarProcess.Priority = this.threadPriority;

				this.eRecoveryStarCore.ThreadPriority = value;
			}
		}

		/// <summary>
		/// Вывод строкового значения в заголовок элемента управления "processGroupBox"
		/// </summary>
		/// <param name="text">Текст, описывающий процентное значение</param>
		/// <param name="progress">Процентное значение прогресса</param>
		private void OnUpdateProgressGroupBox(String text, double progress)
		{
			// Попытка осуществить вход в критическую область...
			if(this.processStatSema.WaitOne(0, false))
			{
				this.processGroupBoxText = text + ": " + Convert.ToString((int)(progress) + " %");

				// Выход из критической области
				this.processStatSema.Release();
			}
		}

		/// <summary>
		/// Вывод строкового значения, завершающего обработку
		/// в заголовок элемента управления "processGroupBox"
		/// </summary>
		/// <param name="text">Текст для вывода</param>
		private void OnFinishProgressGroupBox(String text)
		{
			// Вход в критическую область...
			if(this.processStatSema.WaitOne())
			{
				this.processGroupBoxText = text + ": завершено";
				this.processProgressBarValue = 100;

				// Выход из критической области
				this.processStatSema.Release();
			}
		}

		/// <summary>
		/// Вывод значения прогресса в элемент управления "processProgressBar"
		/// </summary>
		/// <param name="progress">Процентное значение прогресса</param>
		private void OnUpdateProcessProgressBar(double progress)
		{
			// Попытка осуществить вход в критическую область...
			if(this.processStatSema.WaitOne(0, false))
			{
				this.processProgressBarValue = (int)progress;

				// Выход из критической области
				this.processStatSema.Release();
			}
		}

		/// <summary>
		/// Обработчик события "Обновление прогресса обработки томов"
		/// </summary>
		/// <param name="progress">Значение прогресса в процентах</param>
		private void OnUpdateFileSplittingProgress(double progress)
		{
			OnUpdateProgressGroupBox("Обработка томов", progress);

			OnUpdateProcessProgressBar(progress);
		}

		/// <summary>
		/// Обработчик события "Завершение процесса обработки томов"
		/// </summary>
		private void OnFileSplittingFinish()
		{
			OnFinishProgressGroupBox("Обработка томов");
		}

		/// <summary>
		/// Обработчик события "Обновление прогресса расчета матрицы кодирования Рида-Соломона"
		/// </summary>
		private void OnUpdateRSMatrixFormingProgress(double progress)
		{
			OnUpdateProgressGroupBox("Расчет матрицы кодирования", progress);

			OnUpdateProcessProgressBar(progress);
		}

		/// <summary>
		/// Обработчик события "Завершение расчета матрицы кодирования Рида-Соломона"
		/// </summary>
		private void OnRSMatrixFormingFinish()
		{
			OnFinishProgressGroupBox("Расчет матрицы кодирования");
		}

		/// <summary>
		/// Обработчик события "Обновление прогресса открытия файловых потоков"
		/// </summary>
		private void OnUpdateFileStreamsOpeningProgress(double progress)
		{
			OnUpdateProgressGroupBox("Открытие файловых потоков", progress);

			OnUpdateProcessProgressBar(progress);
		}

		/// <summary>
		/// Обработчик события "Завершение процесса открытия файловых потоков"
		/// </summary>
		private void OnFileStreamsOpeningFinish()
		{
			OnFinishProgressGroupBox("Открытие файловых потоков");
		}

		/// <summary>
		/// Обработчик события "Начало кодирования Рида-Соломона"
		/// </summary>
		private void OnStartedRSCoding()
		{
			if(processGroupBox.InvokeRequired) processGroupBox.Invoke(new OnEventHandler(OnStartedRSCoding), new object[] {});
			else
			{
				processGroupBox.Text = "Выполняется кэширование файловых потоков (этот процесс может занять несколько минут)";
			}
		}

		/// <summary>
		/// Обработчик события "Обновление прогресса процесса кодирования томов"
		/// </summary>
		private void OnUpdateFileCodingProgress(double progress)
		{
			OnUpdateProgressGroupBox("Кодирование Рида-Соломона", progress);

			OnUpdateProcessProgressBar(progress);
		}

		/// <summary>
		/// Обработчик события "Завершение процесса кодирования томов"
		/// </summary>
		private void OnFileCodingFinish()
		{
			OnFinishProgressGroupBox("Кодирование Рида-Соломона");
		}

		/// <summary>
		/// Обработчик события "Обновление прогресса закрытия файловых потоков"
		/// </summary>
		private void OnUpdateFileStreamsClosingProgress(double progress)
		{
			OnUpdateProgressGroupBox("Закрытие файловых потоков", progress);

			OnUpdateProcessProgressBar(progress);
		}

		/// <summary>
		/// Обработчик события "Завершение процесса закрытия файловых потоков"
		/// </summary>
		private void OnFileStreamsClosingFinish()
		{
			OnFinishProgressGroupBox("Закрытие файловых потоков");
		}

		/// <summary>
		/// Обработчик события "Обновление прогресса процесса анализа томов"
		/// </summary>
		private void OnUpdateFileAnalyzeProgress(double progress)
		{
			OnUpdateProgressGroupBox("Контроль целостности данных", progress);

			OnUpdateProcessProgressBar(progress);
		}

		/// <summary>
		/// Обработчик события "Завершение процесса анализа томов"
		/// </summary>
		private void OnFileAnalyzeFinish()
		{
			OnFinishProgressGroupBox("Контроль целостности данных");
		}

		/// <summary>
		/// Обработчик события "Получение статистики повреждений томов"
		/// </summary>
		private void OnGetDamageStat(int allVolMissCount, int altEccVolPresentCount, double percOfDamage, double percOfAltEcc)
		{
			if(this.InvokeRequired) this.Invoke(new OnUpdateTwoIntDoubleValueHandler(OnGetDamageStat), new object[] {allVolMissCount, altEccVolPresentCount, percOfDamage, percOfAltEcc});
			else
			{
				// Выводим статистику повреждений
				percOfDamageLabel.Text = Convert.ToString((int)(percOfDamage)) + " %  (" + Convert.ToString(allVolMissCount) + ");";
				percOfAltEccLabel.Text = Convert.ToString((int)(percOfAltEcc)) + " %  (" + Convert.ToString(altEccVolPresentCount) + ");";
				logListBox.Items.Add("Всего поврежденных томов: " + percOfDamageLabel.Text);
				logListBox.Items.Add("Резерв томов для восстановления: " + percOfAltEccLabel.Text);
			}
		}

		/// <summary>
		/// Обработчик события "Обновление лога процесса обработки"
		/// </summary>
		private void OnUpdateLogListBox(String text)
		{
			if(logListBox.InvokeRequired) logListBox.Invoke(new OnUpdateStringValueHandler(OnUpdateLogListBox), new object[] {text});
			else
			{
				logListBox.Items.Add(text);
			}
		}

		/// <summary>
		/// Обработчик события "Изменен приоритет процесса"
		/// </summary>
		private void processPriorityComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			// Считываем значение с элемента управления, ответственного за
			// приоритет процесса обработки данных
			SetThreadPriority(processPriorityComboBox.SelectedIndex);

			pauseButtonXP.Focus();
		}

		/// <summary>
		/// Постановка обработки на паузу
		/// </summary>
		private void pauseButtonXP_Click(object sender, EventArgs e)
		{
			if(pauseButtonXP.Text == "Пауза")
			{
				pauseButtonXP.Text = "Продолжить";

				// Отключаем таймер вывода прогресса обработки...
				this.Invoke(((OnEventHandler)delegate() { processTimer.Stop(); }), new object[] {});

				// Ставим обработку на паузу...
				Pause();
			}
			else
			{
				pauseButtonXP.Text = "Пауза";

				// Снимаем обработку с паузы
				Continue();

				// Активируем таймер вывода прогресса обработки...
				this.Invoke(((OnEventHandler)delegate() { processTimer.Start(); }), new object[] {});
			}
		}

		/// <summary>
		/// Метод остановки обработки - подаем сигнал остановки, а, затем,
		/// запускаем таймер, на обработчике которого и произойдет закрытие формы
		/// </summary>
		private void stopButtonXP_Click(object sender, EventArgs e)
		{
			// Если запрос на закрытие поступил во время обработки -
			// нужен дополнительный запрос
			if(this.stopButtonXP.Text == "Прервать обработку")
			{
				string message = "Вы действительно хотите прервать обработку?";
				string caption = " Recovery Star 2.22";
				MessageBoxButtons buttons = MessageBoxButtons.YesNo;
				DialogResult result = MessageBox.Show(null, message, caption, buttons, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);

				// Если пользователь нажал на кнопку "No" - выходим из обработчика
				if(result == DialogResult.No)
				{
					return;
				}
			}

			// Сначала отключаем кнопку прерывания обработки...
			stopButtonXP.Enabled = false;

			// Прекращение обработки данных...
			Stop();

			// Активируем таймер выхода из обработки...
			closingTimer.Start();
		}

		/// <summary>
		/// Обработчик таймера, ответственного за обеспечение выхода из обработки
		/// </summary>
		private void closingTimer_Tick(object sender, EventArgs e)
		{
			// Если ядро системы не завершило свою работу - закрывать форму пока ещё нельзя!
			if(!this.eRecoveryStarCore.Finished)
			{
				return;
			}

			// Деактивируем таймер...
			closingTimer.Stop();

			// Закрываем форму
			Close();

			// Указываем, что обработка не производится
			this.mode = RSMode.None;

			// Включаем браузер на главной форме
			this.Browser.Enabled = true;

			// Инициируем сборку мусора
			GC.Collect();
		}

		/// <summary>
		/// Обработчик события "Тик таймера для обновления статистики обработки"
		/// </summary>
		private void processTimer_Tick(object sender, EventArgs e)
		{
			// Вход в критическую область...
			if(this.processStatSema.WaitOne())
			{
				if(this.processGroupBoxText != null)
				{
					processGroupBox.Text = this.processGroupBoxText;
				}

				if(this.processProgressBarValue != -1)
				{
					processProgressBar.Value = this.processProgressBarValue;
				}

				// Выход из критической области
				this.processStatSema.Release();
			}
		}

		/// <summary>
		/// Обработчик события "Загрузка формы обработки"
		/// </summary>
		private void ProcessForm_Load(object sender, EventArgs e)
		{
			// Указываем, что поток должен выполняться
			this.exitEvent[0].Reset();
			this.executeEvent[0].Set();
			this.wakeUpEvent[0].Reset();

			// Создаем поток обработки данных...
			this.thrRecoveryStarProcess = new Thread(new ThreadStart(Process));

			//...затем даем ему имя...
			this.thrRecoveryStarProcess.Name = "RecoveryStar.Process()";

			//...устанавливаем выбранный приоритет задачи...
			this.thrRecoveryStarProcess.Priority = this.threadPriority;

			// Отключаем браузер на главной форме
			this.Browser.Enabled = false;

			//...и запускаем его
			this.thrRecoveryStarProcess.Start();
		}

		#endregion Private Operations
	}
}