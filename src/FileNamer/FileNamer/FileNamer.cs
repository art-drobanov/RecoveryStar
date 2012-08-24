/*----------------------------------------------------------------------+
 |  filename:   FileNamer.cs                                            |
 |----------------------------------------------------------------------|
 |  version:    2.21                                                    |
 |  revision:   24.08.2012 15:52                                        |
 |  authors:    Дробанов Артём Федорович (DrAF),                        |
 |              RUSpectrum (г. Оренбург).                               |
 |  e-mail:     draf@mail.ru                                            |
 |  purpose:    Упаковка (распаковка) имени файла в префиксный формат   |
 +----------------------------------------------------------------------*/

using System;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;

namespace RecoveryStar
{
	/// <summary>
	/// Класс для формирования имени тома на основе порядкового номера и конфигурации кодера
	/// </summary>
	public class FileNamer
	{
		#region Data

		/// <summary>
		/// Формат упаковки
		/// </summary>
		private String packFormat = "{0}{1:X4}{2:X4}{3:X4}.{4}";

		/// <summary>
		/// Регулярное выражение для распаковки имени файла
		/// </summary>
		private Regex unpackRegex = new Regex("^(?<codecPrefix>[@, A, C])(?<volNum>[0-9A-F]{4})(?<dataCount>[0-9A-F]{4})(?<eccCount>[0-9A-F]{4})\\.(?<fileName>.+)", RegexOptions.IgnoreCase);

		#endregion Data

		#region Public Operations

		/// <summary>
		/// Возвращает путь, выделяя его из полного имени файла
		/// </summary>
		/// <param name="fullFileName">Полное имя файла</param>
		/// <returns>Путь</returns>
		public string GetPath(String fullFileName)
		{
			return Path.GetDirectoryName(fullFileName) + "\\";
		}

		/// <summary>
		/// Возвращает короткое имя файла, отсекая путь
		/// </summary>
		/// <param name="fullFileName">Полное имя файла</param>
		/// <returns>Короткое имя файла</returns>
		public string GetShortFileName(String fullFileName)
		{
			return Path.GetFileName(fullFileName);
		}

		/// <summary>
		/// "Упаковка" исходного имени файла в префиксный формат
		/// </summary>
		/// <param name="fileName">Имя файла для "упаковки"</param>
		/// <param name="volNum">Номер текущего тома</param>
		/// <param name="dataCount">Количество основных томов</param>
		/// <param name="eccCount">Количество томов для восстановления</param>
		/// <param name="codecType">Тип кодека Рида-Соломона (по типу матрицы)</param>
		/// <returns>Булевский флаг операции</returns>
		public bool Pack(ref String fileName, int volNum, int dataCount, int eccCount, int codecType)
		{
			char codecPrefix;

			switch(codecType)
			{
				case (int)RSType.Dispersal:
					{
						codecPrefix = '@';
						break;
					}

				case (int)RSType.Alternative:
					{
						codecPrefix = 'A';
						break;
					}

				default:
				case (int)RSType.Cauchy:
					{
						codecPrefix = 'C';
						break;
					}
			}

			fileName = string.Format(this.packFormat, codecPrefix, volNum, dataCount, eccCount, fileName);

			return true;
		}

		/// <summary>
		/// "Распаковка" имени файла из префиксного формата в исходный
		/// </summary>
		/// <param name="fileName">Имя файла для "распаковки"</param>
		/// <param name="volNum">Номер текущего тома</param>
		/// <param name="dataCount">Количество основных томов</param>
		/// <param name="eccCount">Количество томов для восстановления</param>
		/// <param name="codecType">Тип кодека Рида-Соломона (по типу матрицы)</param>
		/// <returns>Булевский флаг операции</returns>
		public bool Unpack(ref String fileName, ref int volNum, ref int dataCount, ref int eccCount, ref int codecType)
		{
			try
			{
				Match regexMatch = this.unpackRegex.Match(fileName);

				if(!regexMatch.Success) return false;

				// Определяем тип кодека по префиксу тома (если префикс нестандартный - выходим с ошибкой)
				if(regexMatch.Groups["codecPrefix"].Value == "C") codecType = (int)RSType.Cauchy;
				else if(regexMatch.Groups["codecPrefix"].Value == "A") codecType = (int)RSType.Alternative;
				else if(regexMatch.Groups["codecPrefix"].Value == "@") codecType = (int)RSType.Dispersal;
				else return false;

				volNum = int.Parse(regexMatch.Groups["volNum"].Value, NumberStyles.HexNumber);
				dataCount = int.Parse(regexMatch.Groups["dataCount"].Value, NumberStyles.HexNumber);
				eccCount = int.Parse(regexMatch.Groups["eccCount"].Value, NumberStyles.HexNumber);
				fileName = regexMatch.Groups["fileName"].Value;

				return true;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// "Распаковка" имени файла из префиксного формата в исходный
		/// </summary>
		/// <param name="fileName">Имя файла для "распаковки"</param>
		/// <param name="dataCount">Количество основных томов</param>
		/// <param name="eccCount">Количество томов для восстановления</param>
		/// <param name="codecType">Тип кодека Рида-Соломона (по типу матрицы)</param>
		/// <returns>Булевский флаг операции</returns>
		public bool Unpack(ref String fileName, ref int dataCount, ref int eccCount, ref int codecType)
		{
			int volNum = 0;
			return Unpack(ref fileName, ref volNum, ref dataCount, ref eccCount, ref codecType);
		}

		/// <summary>
		/// "Распаковка" имени файла из префиксного формата в исходный
		/// </summary>
		/// <param name="fileName">Имя файла для "распаковки"</param>
		/// <param name="codecType">Тип кодека Рида-Соломона (по типу матрицы)</param>
		/// <returns>Булевский флаг операции</returns>
		public bool Unpack(ref String fileName, ref int codecType)
		{
			int volNum = 0;
			int dataCount = 0;
			int eccCount = 0;
			return Unpack(ref fileName, ref volNum, ref dataCount, ref eccCount, ref codecType);
		}

		/// <summary>
		/// "Распаковка" имени файла из префиксного формата в исходный
		/// </summary>
		/// <param name="fileName">Имя файла для "распаковки"</param>
		/// <returns>Булевский флаг операции</returns>
		public bool Unpack(ref String fileName)
		{
			int volNum = 0;
			int dataCount = 0;
			int eccCount = 0;
			int codecType = 0;
			return Unpack(ref fileName, ref volNum, ref dataCount, ref eccCount, ref codecType);
		}

		#endregion Public Operations
	}
}