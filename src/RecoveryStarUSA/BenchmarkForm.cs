/*----------------------------------------------------------------------+
 |  filename:   BenchmarkForm.cs                                        |
 |----------------------------------------------------------------------|
 |  version:    2.22                                                    |
 |  revision:   02.04.2013 17:00                                        |
 |  authors:    Дробанов Артём Федорович (DrAF),                        |
 |              RUSpectrum (г. Оренбург).                               |
 |  e-mail:     draf@mail.ru                                            |
 |  purpose:    Тест быстродействия                                     |
 +----------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace RecoveryStar
{
	public partial class BenchmarkForm : Form
	{
		#region Public Properties & Data

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

		#endregion Public Properties & Data

		#region Data

		/// <summary>
		/// RAID-подобный кодер Рида-Соломона
		/// </summary>
		private RSRaidEncoder eRSRaidEncoder;

		/// <summary>
		/// Арифметика поля Галуа
		/// </summary>
		private GF16 eGF16;

		/// <summary>
		/// Время, которое отработал тест
		/// </summary>
		private double timeInTest;

		/// <summary>
		/// Обработанный объем данных в мегабайтах
		/// </summary>
		private double processedDataCount;

		/// <summary>
		/// Поток обработки данных
		/// </summary>
		private Thread thrBenchmarkProcess;

		/// <summary>
		/// Событие прекращения обработки
		/// </summary>
		private ManualResetEvent[] exitEvent;

		/// <summary>
		/// Событие продолжения обработки
		/// </summary>
		private ManualResetEvent[] executeEvent;

		/// <summary>
		/// Семафор для предотвращения конфликтов читатель/писатель при работе со статистикой
		/// </summary>
		private Semaphore coderStatSema;

		/// <summary>
		/// Состояние счетчика времени на момент запуска теста
		/// </summary>
		private long DateTimeTicksOnStart;

		/// <summary>
		/// Счетчик количества обработанных байт
		/// </summary>
		private long processedBytesCount;

		#endregion Data

		#region Construction & Destruction

		/// <summary>
		/// Конструктор формы
		/// </summary>
		public BenchmarkForm()
		{
			InitializeComponent();

			// Инициализируем событие прекращения обработки файлов
			this.exitEvent = new ManualResetEvent[] {new ManualResetEvent(false)};

			// Инициализируем cобытие продолжения обработки файлов
			this.executeEvent = new ManualResetEvent[] {new ManualResetEvent(false)};

			// Начальное значение семафора - 1, максимум 1 блок.
			this.coderStatSema = new Semaphore(1, 1);

			// Создаем класс арифметики поля Галуа
			this.eGF16 = new GF16();
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
		/// Тест на быстродействие (кодер Рида-Соломона)
		/// </summary>
		private void Benchmark()
		{
			// Создаем RAID-подобный кодер Рида-Соломона
			if(this.eRSRaidEncoder == null)
			{
				this.eRSRaidEncoder = new RSRaidEncoder(this.dataCount, this.eccCount, this.codecType);
			}

			// Подписываемся на делегатов
			this.eRSRaidEncoder.OnUpdateRSMatrixFormingProgress = new OnUpdateDoubleValueHandler(OnUpdateRSMatrixFormingProgress);
			this.eRSRaidEncoder.OnRSMatrixFormingFinish = new OnEventHandler(OnRSMatrixFormingFinish);

			// Запускаем подготовку RAID-подобного кодера Рида-Соломона
			if(this.eRSRaidEncoder.Prepare(true))
			{
				// Цикл ожидания завершения подготовки кодера Рида-Соломона к работе
				while(true)
				{
					// Ждем любое из перечисленных событий...
					int eventIdx = ManualResetEvent.WaitAny(new ManualResetEvent[] {this.exitEvent[0], this.eRSRaidEncoder.FinishedEvent[0]});

					//...если получили сигнал к выходу из обработки...
					if(eventIdx == 0)
					{
						//...останавливаем контролируемый алгоритм
						this.eRSRaidEncoder.Stop();

						return;
					}

					//...если получили сигнал о завершении обработки вложенным алгоритмом...
					if(eventIdx == 1)
					{
						//...выходим из цикла ожидания завершения (этого и ждали в while(true)!)
						break;
					}
				} // while(true)
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
				string message = "Reed-Solomon Coder configuration error!";
				string caption = " Recovery Star 2.22";
				MessageBoxButtons buttons = MessageBoxButtons.OK;
				MessageBox.Show(null, message, caption, buttons, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);

				return;
			}

			// Выделяем память под массивы входных и выходных данных кодера
			int[] sourceLog = new int[this.dataCount];
			int[] target = new int[this.eccCount];

			// Буфер для работы с парами байт
			byte[] wordBuff = new byte[2];

			// Буферы для имитации содержимого файловых потоков
			byte[] fileStreamImitator_0 = new byte[this.dataCount];
			byte[] fileStreamImitator_1 = new byte[this.dataCount];

			// Инициализируем генератор случайных чисел
			Random eRandom = new Random((int)System.DateTime.Now.Ticks);

			// Заполняем буферы случайными данными
			eRandom.NextBytes(fileStreamImitator_0);
			eRandom.NextBytes(fileStreamImitator_1);

			// Состояние счетчика времени на определенный момент времени
			long DateTimeTicksNow = -1;

			// Сбрасываем счетчик количества обработанных байт
			this.processedBytesCount = 0;

			// Обновляем состояние счетчика времени на момент запуска теста
			this.DateTimeTicksOnStart = System.DateTime.Now.Ticks;

			// Бесконечный цикл тестирования кодера Рида-Соломона
			while(true)
			{
				// Заполняем вектор исходных данных кодера данными текущего среза
				for(int j = 0; j < this.dataCount; j++)
				{
					// Читаем пару байт из входного потока
					wordBuff[0] = fileStreamImitator_0[j];
					wordBuff[1] = fileStreamImitator_1[j];

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

					// Теперь пишем пару байт в выходной поток...
					// Т.к. это тест - действие записи данных на диск не производится
				}

				// Обновляем значение времени
				DateTimeTicksNow = System.DateTime.Now.Ticks;

				// Пересчитываем количество обработанных байт
				this.processedBytesCount += (2 * this.dataCount);

				// Попытка осуществить вход в критическую область...
				if(this.coderStatSema.WaitOne(0, false))
				{
					// Вычисляем время работы теста в секундах (1 тик == 10^-07 секунды)
					// 0x01 - чтобы не получить деление на ноль
					this.timeInTest = ((double)((DateTimeTicksNow - this.DateTimeTicksOnStart) | 0x01) / 10000000.0);

					// Вычисляем обработанный объем данных в мегабайтах
					this.processedDataCount = ((double)this.processedBytesCount / (double)(1024 * 1024));

					// Выполнение процедуры "V" на семафоре...
					this.coderStatSema.Release();
				}

				// В случае, если требуется постановка на паузу, событие "executeEvent"
				// будет сброшено, и будем на паузе вплоть до его появления
				ManualResetEvent.WaitAll(this.executeEvent);

				// Если получили сигнал к выходу из обработки...
				if(ManualResetEvent.WaitAll(this.exitEvent, 0, false))
				{
					return;
				}
			} // while(true)
		}

		/// <summary>
		/// Обработчик события "Обновление прогресса расчета матрицы кодирования Рида-Соломона"
		/// </summary>
		private void OnUpdateRSMatrixFormingProgress(double progress)
		{
			// Формируем текст для вывода...
			String textToOut = "Preparing: " + System.Convert.ToString((int)progress) + " %";

			//...изменяем надпись заголовка окна
			this.Invoke(((OnUpdateStringValueHandler)delegate(String value) { this.Text = value; }), new object[] {textToOut});
		}

		/// <summary>
		/// Обработчик события "Завершение расчета матрицы кодирования Рида-Соломона"
		/// </summary>
		private void OnRSMatrixFormingFinish()
		{
			// Формируем текст для вывода...
			String textToOut = "Benchmarking...";

			//...изменяем надпись заголовка окна
			this.Invoke(((OnUpdateStringValueHandler)delegate(String value) { this.Text = value; }), new object[] {textToOut});
		}

		/// <summary>
		/// Обработчик тика таймера - вывод статистики на форму
		/// </summary>
		private void BenchmarkTimer_Tick(object sender, EventArgs e)
		{
			// Вход в критическую область...
			if(this.coderStatSema.WaitOne())
			{
				// Выводим статистику по времени работы теста
				timeInTestLabel.Text = System.Convert.ToString((int)this.timeInTest) + " s";

				// Выводим статистику по объему обработанных мегабайт за время работы теста
				processedDataCountLabel.Text = System.Convert.ToString((int)this.processedDataCount) + " Mbytes";

				// Вычисляем скорость кодирования
				double speed = this.processedDataCount / this.timeInTest;

				// Формируем строку вычисленной скорости
				// (точка в конце, возможно будет лишней, зато гарантирует
				// корректность вызова IndexOf())
				String outSpeedStr = System.Convert.ToString(speed) + ',';

				// Определяем положение десятичного разделителя
				int indexOfPoint = outSpeedStr.IndexOf(',');

				// Длина выделяемой подстроки
				int subStrLen = -1;

				// Если единственная обнаруженная точка в конце строки -
				// это фиктивная точка и её отбрасываем
				if(indexOfPoint == (outSpeedStr.Length - 1))
				{
					subStrLen = (outSpeedStr.Length - 1);
				}
				else
				{
					if(indexOfPoint < (outSpeedStr.Length - 2))
					{
						subStrLen = (indexOfPoint + 1) + 2;
					}
				}

				// Выводим статистику по скорости кодирования
				coderSpeedGroupBox.Text = "Speed: " + outSpeedStr.Substring(0, subStrLen) + " Mbytes/s";

				// Выполнение процедуры "V" на семафоре...
				this.coderStatSema.Release();
			}
		}

		/// <summary>
		/// Постановка тестирования производительности на паузу
		/// </summary>
		private void pauseButtonXP_Click(object sender, EventArgs e)
		{
			if(pauseButtonXP.Text == "Pause")
			{
				pauseButtonXP.Text = "Continue";

				// Отключаем таймер обновления данных теста...
				benchmarkTimer.Stop();

				// ...и ставим обработку на паузу
				Pause();
			}
			else
			{
				pauseButtonXP.Text = "Pause";

				// Сбрасываем счетчик количества обработанных байт
				this.processedBytesCount = 0;

				// Обновляем состояние счетчика времени на момент возобновления теста
				this.DateTimeTicksOnStart = System.DateTime.Now.Ticks;

				// Снимаем обработку с паузы...
				Continue();

				// ...и включаем таймер обновления данных теста
				benchmarkTimer.Start();
			}
		}

		/// <summary>
		/// Метод прерывания тестирования - подаем сигнал остановки, а, затем,
		/// запускаем таймер, на обработчике которого и произойдет закрытие формы
		/// </summary>
		private void closeButtonXP_Click(object sender, EventArgs e)
		{
			// Сначала отключаем кнопку прерывания тестирования...
			closeButtonXP.Enabled = false;

			// Прекращение обработки данных...
			Stop();

			// Активируем таймер выхода из обработки...
			closingTimer.Start();
		}

		/// <summary>
		/// Обработчик таймера, ответственного за обеспечение выхода из
		/// тестирования производительности
		/// </summary>
		private void closingTimer_Tick(object sender, EventArgs e)
		{
			// Деактивируем таймер...
			closingTimer.Stop();

			// Закрываем форму
			Close();

			// Инициируем сборку мусора
			GC.Collect();
		}

		/// <summary>
		/// Обработчик события "Загрузка формы тестирования производительности"
		/// </summary>
		private void BenchmarkForm_Load(object sender, EventArgs e)
		{
			// Устанавливаем соответствующие текстовые метки (конфигурация кодера)
			dataCountLabel.Text = System.Convert.ToString(this.dataCount);
			eccCountLabel.Text = System.Convert.ToString(this.eccCount);

			// Указываем, что поток должен выполняться
			this.exitEvent[0].Reset();
			this.executeEvent[0].Set();

			// Создаем поток обработки данных...
			this.thrBenchmarkProcess = new Thread(new ThreadStart(Benchmark));

			//...затем даем ему имя...
			this.thrBenchmarkProcess.Name = "RecoveryStar.Benchmark()";

			//...и запускаем его
			this.thrBenchmarkProcess.Start();

			// Включаем таймер обновления содержимого формы
			benchmarkTimer.Start();
		}

		#endregion Private Operations
	}
}