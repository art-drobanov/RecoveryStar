/*----------------------------------------------------------------------+
 |  filename:   MainForm.cs                                             |
 |----------------------------------------------------------------------|
 |  version:    2.21                                                    |
 |  revision:   24.08.2012 15:52                                        |
 |  authors:    Дробанов Артём Федорович (DrAF),                        |
 |              RUSpectrum (г. Оренбург).                               |
 |  e-mail:     draf@mail.ru                                            |
 |  purpose:    Отказоустойчивое кодирование по типу RAID-систем        |
 +----------------------------------------------------------------------*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace RecoveryStar
{
	public partial class MainForm : Form
	{
		#region Data

		/// <summary>
		/// Объект для упаковки (распаковки) имени в префиксный формат
		/// </summary>
		private FileNamer eFileNamer;

		/// <summary>
		/// Массив значений индексов для элементов управления "TrackBar" (количество томов)
		/// </summary>
		private int[] allVolCountTrackBarValuesArr;

		/// <summary>
		/// Массив значений индексов для элементов управления "TrackBar" (избыточность)
		/// </summary>
		private int[] redundancyTrackBarValuesArr;

		/// <summary>
		/// Общее количество томов
		/// </summary>
		private int allVolCount;

		/// <summary>
		/// Избыточность кодирования
		/// </summary>
		private int redundancy;

		/// <summary>
		/// Количество основных томов
		/// </summary>
		private int dataCount;

		/// <summary>
		/// Количество томов для восстановления
		/// </summary>
		private int eccCount;

		/// <summary>
		/// Форма ввода пароля
		/// </summary>
		private PasswordForm ePasswordForm;

		/// <summary>
		/// Форма обработки файлов
		/// </summary>
		private ProcessForm eProcessForm;

		/// <summary>
		/// Форма тестирования
		/// </summary>
		private BenchmarkForm eBenchmarkForm;

		#endregion Data

		#region Construction & Destruction

		/// <summary>
		/// Конструктор формы
		/// </summary>
		public MainForm()
		{
			InitializeComponent();

			// Создаем форму ввода пароля
			this.ePasswordForm = new PasswordForm();

			// Инициализируем экземпляр класса для упаковки (распаковки) имени файла
			// в префиксный формат
			this.eFileNamer = new FileNamer();

			// Инициализируем массивы, хранящие константные значения размера тома и избыточности,
			// доступных пользователю
			this.allVolCountTrackBarValuesArr = new int[(allVolCountMacTrackBar.Maximum + 1)];

			// Задаем стартовые значения степени двойки и полусуммы соседних степеней
			int p1 = 2, p2 = 3;
			for(int i = 0; i < allVolCountMacTrackBar.Maximum; i += 2)
			{
				// Ряд доступных нечетных значений количества томов рассчитывается как
				// степень двойки, ряд четных значений - как полусумма расположенных рядом
				// значений
				this.allVolCountTrackBarValuesArr[i + 0] = p1;
				this.allVolCountTrackBarValuesArr[i + 1] = p2;

				// Увеличиаем степень значений
				p1 <<= 1;
				p2 <<= 1;
			}

			// Дописываем не обработанный в цикле элемент
			this.allVolCountTrackBarValuesArr[allVolCountMacTrackBar.Maximum] = p1;

			this.redundancyTrackBarValuesArr = new int[(redundancyMacTrackBar.Maximum + 1)];

			for(int i = 0; i <= redundancyMacTrackBar.Maximum; i++)
			{
				this.redundancyTrackBarValuesArr[i] = (i + 1) * 5;
			}
		}

		#endregion Construction & Destruction

		#region Private Operations

		/// <summary>
		/// Метод обработки файлов в выбранной директории
		/// </summary>
		private void ProcessFiles()
		{
			// Если в браузере в качестве текущего элемента выбрана директория
			if(browser.SelectedItem.IsFolder)
			{
				// Если в данный момент форма открыта - просто выходим
				if(
					(this.eProcessForm != null)
					&&
					(this.eProcessForm.Visible)
					)
				{
					return;
				}

				// Устанавливаем параметры кодера
				this.eProcessForm.DataCount = this.dataCount;
				this.eProcessForm.EccCount = this.eccCount;
				this.eProcessForm.CodecType = (int)RSType.Cauchy;

				// Список файлов в текущей директории
				FileInfo[] fileInfos;
				try
				{
					fileInfos = new DirectoryInfo(browser.SelectedItem.Path).GetFiles();
				}
				catch
				{
					// Cбрасываем установленный режим
					this.eProcessForm.Mode = RSMode.None;

					return;
				}

				// Сохраняем ссылку на браузер...
				this.eProcessForm.Browser = browser;

				// Копируем имена файлов для обработки
				for(int i = 0; i < fileInfos.Length; i++)
				{
					// Извлекаем очередное имя из списка...
					String fullFileName = fileInfos[i].DirectoryName + @"\" + fileInfos[i].Name;

					// Получаем короткий вариант имени файла
					String shortFileName = this.eFileNamer.GetShortFileName(fullFileName);

					// Если имя исходного файла превышает 50 символов - он не может быть обработан
					// (потому что при добавлении префикса на выходе получится более 64-х символов)
					if(shortFileName.Length > 50)
					{
						string message = "Длина имени файла \"" + shortFileName + "\" превышает 50 символов! Пропустить этот файл и продолжать процесс формирования списка для обработки?";
						string caption = " Recovery Star 2.21";
						MessageBoxButtons buttons = MessageBoxButtons.YesNo;
						DialogResult result = MessageBox.Show(null, message, caption, buttons, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);

						// Если пользователь нажал на кнопку "No" - прерываем обработку...
						if(result == DialogResult.No)
						{
							//...предварительно сбросив установленный режим
							this.eProcessForm.Mode = RSMode.None;

							return;
						}
					}
					else
					{
						// Если файл присутствует - добавляем его в список на обработку
						if(File.Exists(fullFileName))
						{
							this.eProcessForm.FileNamesToProcess.Add(fullFileName);
						}
					}
				}

				// Если размер списка для обработки не равен нулю
				// (т.е. есть что обрабатывать) - будем осуществлять обработку
				if(this.eProcessForm.FileNamesToProcess.ToArray().Length != 0)
				{
					// Отображаем окно обработки
					this.eProcessForm.Show();
				}
				else
				{
					string message = "В указанной директории не найдено доступных файлов для обработки!";
					string caption = " Recovery Star 2.21";
					MessageBoxButtons buttons = MessageBoxButtons.OK;
					MessageBox.Show(null, message, caption, buttons, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);

					// Cбрасываем установленный режим
					this.eProcessForm.Mode = RSMode.None;
				}
			}
			else
			{
				// Cбрасываем установленный режим
				this.eProcessForm.Mode = RSMode.None;
			}
		}

		/// <summary>
		/// Метод обработки файлов в выбранной директории с учетом уникального имени,
		/// породившего последовательность
		/// </summary>
		private void ProcessUniqueFiles()
		{
			// Если в браузере в качестве текущего элемента выбрана директория
			if(browser.SelectedItem.IsFolder)
			{
				// Если в данный момент форма открыта - просто выходим
				if(
					(this.eProcessForm != null)
					&&
					(this.eProcessForm.Visible)
					)
				{
					return;
				}

				// Список уникальных имен файлов для обработки
				ArrayList uniqueNamesToProcess = new ArrayList();

				// Список файлов в текущей директории
				FileInfo[] fileInfos;
				try
				{
					fileInfos = new DirectoryInfo(browser.SelectedItem.Path).GetFiles();
				}
				catch
				{
					// Cбрасываем установленный режим
					this.eProcessForm.Mode = RSMode.None;

					return;
				}

				// Сохраняем ссылку на браузер...
				this.eProcessForm.Browser = browser;

				// Копируем имена файлов для обработки
				for(int i = 0; i < fileInfos.Length; i++)
				{
					// Извлекаем очередное имя из списка...
					String fullFileName = fileInfos[i].DirectoryName + @"\" + fileInfos[i].Name;

					//...получаем его короткий вариант...
					String shortFileName = this.eFileNamer.GetShortFileName(fullFileName);

					//...и распаковываем его с получением оригинального имени...
					String unpackedFileName = shortFileName;

					// Если не удалось корректно распаковать короткое имя - переходим
					// на следующую итерацию
					if(!this.eFileNamer.Unpack(ref unpackedFileName))
					{
						continue;
					}

					//...затем проверяем его на уникальность - если такое имя уже есть
					// в словаре "uniqueNamesToProcess", то добавлять его не будем

					// Сначала предполагаем, что распакованное имя файла уникально
					bool unpackedFileNameIsUnique = true;

					// Перебираем весь имеющийся список уникальных имен
					foreach(String currUnpackedFileName in uniqueNamesToProcess)
					{
						// Если обнаружили совпадение - имя не уникально,
						// сообщаем об этом и выходим из списка
						if(currUnpackedFileName == unpackedFileName)
						{
							unpackedFileNameIsUnique = false;

							break;
						}
					}

					// Если распакованный файл уникален...
					if(unpackedFileNameIsUnique)
					{
						// Если файл присутствует...
						if(File.Exists(fullFileName))
						{
							//...добавляем имя в список уникальных имен...
							uniqueNamesToProcess.Add(unpackedFileName);

							//...добавляем имя в список для обработки...
							this.eProcessForm.FileNamesToProcess.Add(fullFileName);
						}
					}
				}

				// Если размер списка для обработки не равен нулю
				// (т.е. есть что обрабатывать) - будем осуществлять обработку
				if(this.eProcessForm.FileNamesToProcess.ToArray().Length != 0)
				{
					// Отображаем окно обработки
					this.eProcessForm.Show();
				}
				else
				{
					string message = "В указанной директории не найдено доступных файлов для обработки!";
					string caption = " Recovery Star 2.21";
					MessageBoxButtons buttons = MessageBoxButtons.OK;
					MessageBox.Show(null, message, caption, buttons, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);

					// Cбрасываем установленный режим
					this.eProcessForm.Mode = RSMode.None;
				}
			}
			else
			{
				// Cбрасываем установленный режим
				this.eProcessForm.Mode = RSMode.None;
			}
		}

		/// <summary>
		/// Запуск режима кодирования файла
		/// </summary>
		private void protectButton_Click(object sender, EventArgs e)
		{
			// После клика на кнопке переносим фокус на файловый браузер
			browser.Focus();

			// Если форма обработки имеет ненулевой хендл и у нее установлен режим -
			// идёт обработка и прерывать её нельзя!
			if(
				(this.eProcessForm != null)
				&&
				(this.eProcessForm.Mode != RSMode.None)
				)
			{
				return;
			}

			// Создаем форму кодирования файла
			this.eProcessForm = new ProcessForm();

			// Делаем форму обработки привязанной к главной форме
			this.eProcessForm.Owner = this;

			// Закрываем форму, если была открыта в конце обработки
			if(this.eProcessForm.Visible)
			{
				this.eProcessForm.Close();
			}

			// Если пароль не пуст - устанавливаем безопасность
			if(this.ePasswordForm.Password.Length != 0)
			{
				this.eProcessForm.Security = new Security(this.ePasswordForm.Password);
				this.eProcessForm.CBCBlockSize = this.ePasswordForm.CBCBlockSize;
			}

			// Устанавливаем режим работы
			this.eProcessForm.Mode = RSMode.Protect;

			// Запускаем обработку файла
			ProcessFiles();
		}

		/// <summary>
		/// Запуск режима восстановления данных файла
		/// </summary>
		private void recoverButton_Click(object sender, EventArgs e)
		{
			// После клика на кнопке переносим фокус на файловый браузер
			browser.Focus();

			// Если форма обработки имеет ненулевой хендл и у нее установлен режим -
			// идёт обработка и прерывать её нельзя!
			if(
				(this.eProcessForm != null)
				&&
				(this.eProcessForm.Mode != RSMode.None)
				)
			{
				return;
			}

			// Создаем форму кодирования файла
			this.eProcessForm = new ProcessForm();

			// Делаем форму обработки привязанной к главной форме
			this.eProcessForm.Owner = this;

			// Закрываем форму, если была открыта в конце обработки
			if(this.eProcessForm.Visible)
			{
				this.eProcessForm.Close();
			}

			// Если пароль не пуст - устанавливаем безопасность
			if(this.ePasswordForm.Password.Length != 0)
			{
				this.eProcessForm.Security = new Security(this.ePasswordForm.Password);
				this.eProcessForm.CBCBlockSize = this.ePasswordForm.CBCBlockSize;
			}

			// Используется быстрое извлечение из томов (без проверки CRC-64)?
			this.eProcessForm.FastExtraction = быстроеИзвлечениеToolStripMenuItem.Checked;

			// Устанавливаем режим работы
			this.eProcessForm.Mode = RSMode.Recover;

			// Запускаем обработку множества уникальных имен файлов (без учета префиксов)
			ProcessUniqueFiles();
		}

		/// <summary>
		/// Запуск режима лечения набора данных файла
		/// </summary>
		private void repairButton_Click(object sender, EventArgs e)
		{
			// После клика на кнопке переносим фокус на файловый браузер
			browser.Focus();

			// Если форма обработки имеет ненулевой хендл и у нее установлен режим -
			// идёт обработка и прерывать её нельзя!
			if(
				(this.eProcessForm != null)
				&&
				(this.eProcessForm.Mode != RSMode.None)
				)
			{
				return;
			}

			// Создаем форму кодирования файла
			this.eProcessForm = new ProcessForm();

			// Делаем форму обработки привязанной к главной форме
			this.eProcessForm.Owner = this;

			// Закрываем форму, если была открыта в конце обработки
			if(this.eProcessForm.Visible)
			{
				this.eProcessForm.Close();
			}

			// Используется быстрое извлечение из томов (без проверки CRC-64)?
			this.eProcessForm.FastExtraction = быстроеИзвлечениеToolStripMenuItem.Checked;

			// Устанавливаем режим работы
			this.eProcessForm.Mode = RSMode.Repair;

			// Запускаем обработку множества уникальных имен файлов (без учета префиксов)
			ProcessUniqueFiles();
		}

		/// <summary>
		/// Запуск режима тестирования набора данных файла
		/// </summary>
		private void testButton_Click(object sender, EventArgs e)
		{
			// После клика на кнопке переносим фокус на файловый браузер
			browser.Focus();

			// Если форма обработки имеет ненулевой хендл и у нее установлен режим -
			// идёт обработка и прерывать её нельзя!
			if(
				(this.eProcessForm != null)
				&&
				(this.eProcessForm.Mode != RSMode.None)
				)
			{
				return;
			}

			// Создаем форму кодирования файла
			this.eProcessForm = new ProcessForm();

			// Делаем форму обработки привязанной к главной форме
			this.eProcessForm.Owner = this;

			// Закрываем форму, если была открыта в конце обработки
			if(this.eProcessForm.Visible)
			{
				this.eProcessForm.Close();
			}

			// Используется быстрое извлечение из томов (без проверки CRC-64)?
			this.eProcessForm.FastExtraction = быстроеИзвлечениеToolStripMenuItem.Checked;

			// Устанавливаем режим работы
			this.eProcessForm.Mode = RSMode.Test;

			// Запускаем обработку множества уникальных имен файлов (без учета префиксов)
			ProcessUniqueFiles();
		}

		/// <summary>
		/// Запуск бенчмарка кодирования
		/// </summary>
		private void тестБыстродействияToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// Если форма обработки имеет ненулевой хендл и у нее установлен режим -
			// идёт обработка и ей мешать нельзя!
			if(
				(this.eProcessForm != null)
				&&
				(this.eProcessForm.Mode != RSMode.None)
				)
			{
				return;
			}

			// Создаем форму кодирования файла
			this.eBenchmarkForm = new BenchmarkForm();

			// Устанавливаем параметры кодера
			this.eBenchmarkForm.DataCount = this.dataCount;
			this.eBenchmarkForm.EccCount = this.eccCount;
			this.eBenchmarkForm.CodecType = (int)RSType.Cauchy;

			// Делаем форму обработки привязанной к главной форме
			this.eBenchmarkForm.Owner = this;

			// Закрываем форму, если была открыта в конце обработки
			if(this.eBenchmarkForm.Visible)
			{
				this.eBenchmarkForm.Close();
			}

			// Отображаем диалоговое окно обработки
			this.eBenchmarkForm.ShowDialog();
		}

		/// <summary>
		/// Метод установки конфигурации кодера с отображением соответствующих
		/// изменений на форме
		/// </summary>
		private void SetCoderConfig()
		{
			// Снимаем данные с элементов управления
			this.allVolCount = this.allVolCountTrackBarValuesArr[allVolCountMacTrackBar.Value];
			this.redundancy = this.redundancyTrackBarValuesArr[redundancyMacTrackBar.Value];

			// Устанавливаем текстовые метки, соответствующую значениям элемента управления
			allVolCountGroupBox.Text = "Общее количество томов: " + System.Convert.ToString(this.allVolCount);
			redundancyGroupBox.Text = "Избыточность кодирования: " + System.Convert.ToString(this.redundancy) + " %";

			// Абсолютное значение количества процентов на том
			double percByVol = (double)this.allVolCount / (double)(100 + this.redundancy);

			// Вычисляем количество томов для восстановления
			this.eccCount = (int)((double)this.redundancy * percByVol); // Томов для восстановления

			// В случае необходимости корректируем количество томов для восстановления
			if(this.eccCount < 1)
			{
				this.eccCount = 1;
			}

			// Количество основных томов находим по остаточному принципу
			this.dataCount = this.allVolCount - this.eccCount;

			// Вычисляем коэффициент выхода
			double outX = ((double)(this.dataCount + this.eccCount)) / (double)this.dataCount;

			// Вычисляем результирующий объем
			String outXStr = System.Convert.ToString(outX);

			// Длина подстроки, выделяемой по-умолчанию
			int subStrLen = 3;

			// Для двузначного значения целой части избыточности нужно брать подстроку
			// на один символ больше
			if(outX >= 10)
			{
				subStrLen++;
			}

			// Корректируем (в случае надобности) длину извлекаемой подстроки
			if(outXStr.Length < subStrLen)
			{
				subStrLen = outXStr.Length;
			}

			// Получаем строковое представление выхода
			outXStr = outXStr.Substring(0, subStrLen);

			// Преобразуем в число строковое значение избыточности, которое увидит пользователь
			double visibleX = System.Convert.ToDouble(outXStr);

			// Если в результате преобразования были утеряны значащие цифры, добавляем 0.1
			// к выводимому значению
			if(visibleX != outX)
			{
				outX += 0.1;
			}

			// Вычисляем результирующий объем
			outXStr = System.Convert.ToString(outX);

			// Длина подстроки, выделяемой по-умолчанию
			subStrLen = 3;

			// Для двузначного значения целой части избыточности нужно брать подстроку
			// на один символ больше
			if(outX >= 10)
			{
				subStrLen++;
			}

			// Корректируем (в случае надобности) длину извлекаемой подстроки
			if(outXStr.Length < subStrLen)
			{
				subStrLen = outXStr.Length;
			}

			// Получаем строковое представление выхода
			outXStr = outXStr.Substring(0, subStrLen);

			// Выводим конфигурацию кодера
			coderConfigGroupBox.Text = "Конфигурация кодера (основных томов: " + System.Convert.ToString(this.dataCount)
			                           + "; томов для восстановления: " + System.Convert.ToString(this.eccCount)
			                           + "; объем выхода: " + outXStr + " X)";
		}

		/// <summary>
		/// Обработчик события изменения общего количества томов
		/// </summary>
		private void allVolCountMacTrackBar_ValueChanged(object sender, decimal value)
		{
			// Снимаем данные с элемента управления
			this.allVolCount = this.allVolCountTrackBarValuesArr[allVolCountMacTrackBar.Value];

			// Устанавливаем текстовую метку, соответствующую значениям элемента управления
			allVolCountGroupBox.Text = "Общее количество томов: " + System.Convert.ToString(this.allVolCount);

			// Устанавливаем конфигурацию кодера
			SetCoderConfig();
		}

		/// <summary>
		/// Обработчик события изменения избыточности кодирования
		/// </summary>
		private void redundancyMacTrackBar_ValueChanged(object sender, decimal value)
		{
			// Снимаем данные с элемента управления
			this.redundancy = this.redundancyTrackBarValuesArr[redundancyMacTrackBar.Value];

			// Устанавливаем текстовую метку, соответствующую значениям элемента управления
			redundancyGroupBox.Text = "Избыточность кодирования: " + System.Convert.ToString(this.redundancy) + " %";

			// Устанавливаем конфигурацию кодера
			SetCoderConfig();
		}

		/// <summary>
		/// Ввод пароля
		/// </summary>
		private void шифрующийФильтрToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if(шифрующийФильтрToolStripMenuItem.Checked == true)
			{
				шифрующийФильтрToolStripMenuItem.Checked = false;

				// Удаляем пароль
				this.ePasswordForm.ClearPassword();
			}
			else
			{
				// Выводим диалог ввода пароля
				this.ePasswordForm.ShowDialog();

				// Если пароль был установлен - фиксируем это
				if(this.ePasswordForm.Password.Length != 0)
				{
					шифрующийФильтрToolStripMenuItem.Checked = true;
				}
			}
		}

		/// <summary>
		/// Активация режима быстрого извлечения данных (без проверки CRC-64)
		/// </summary>
		private void быстроеИзвлечениеToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if(быстроеИзвлечениеToolStripMenuItem.Checked == true)
			{
				быстроеИзвлечениеToolStripMenuItem.Checked = false;
			}
			else
			{
				быстроеИзвлечениеToolStripMenuItem.Checked = true;
			}
		}

		/// <summary>
		/// Вызов справки
		/// </summary>
		private void вызовСправкиToolStripMenuItem_Click(object sender, EventArgs e)
		{
			try
			{
				// Открываем файл справки
				System.Diagnostics.Process.Start("HelpRUS.mht");
			}
			catch
			{
				string message = "Не могу открыть \"HelpRUS.mht\"!";
				string caption = " Recovery Star 2.21";
				MessageBoxButtons buttons = MessageBoxButtons.OK;
				MessageBox.Show(null, message, caption, buttons, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
			}
		}

		/// <summary>
		/// О программе
		/// </summary>
		private void оПрограммеToolStripMenuItem_Click(object sender, EventArgs e)
		{
			AboutForm eAboutForm = new AboutForm();
			eAboutForm.ShowDialog();
		}

		/// <summary>
		/// Выход
		/// </summary>
		private void выходToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// Если форма обработки имеет ненулевой хендл и у нее установлен режим -
			// идёт обработка и ей мешать нельзя!
			if(
				(this.eProcessForm != null)
				&&
				(this.eProcessForm.Mode != RSMode.None)
				)
			{
				return;
			}

			Close();
		}

		/// <summary>
		/// Обработчик закрытия формы
		/// </summary>
		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			// Если форма обработки имеет ненулевой хендл - её нужно закрыть
			if(this.eProcessForm != null)
			{
				// На обработчике Close() отработает остановка процесса
				this.eProcessForm.Close();
			}
		}

		/// <summary>
		/// Обработчик загрузки формы
		/// </summary>
		private void MainForm_Load(object sender, EventArgs e)
		{
			// Устанавливаем общее количество томов по-умолчанию - 1024
			allVolCountMacTrackBar.Value = 18;

			// Устанавлиаем избыточность кодирования - 100%
			redundancyMacTrackBar.Value = 19;

			// Устанавливаем конфигурацию кодера
			SetCoderConfig();
		}

		#endregion Private Operations
	}
}