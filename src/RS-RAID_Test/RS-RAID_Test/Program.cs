/*----------------------------------------------------------------------+
 |  filename:   Program.cs                                              |
 |----------------------------------------------------------------------|
 |  version:    2.20                                                    |
 |  revision:   23.05.2012 17:33                                        |
 |  authors:    Дробанов Артём Федорович (DrAF),                        |
 |              RUSpectrum (г. Оренбург).                               |
 |  e-mail:     draf@mail.ru                                            |
 |  purpose:    Тест RAID-подобного декодера Рида-Соломона (Коши)       |
 +----------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.IO;

namespace RecoveryStar
{
	class Program
	{
		private static void Main(string[] args)
		{
			RSRaidDecoder eRSRaidDecoder = new RSRaidDecoder();
			Random eRandom = new Random();

			Console.Clear();
			Console.WriteLine("");
			Console.WriteLine("Recovery Star 2.20 (Cauchy Reed-Solomon Decoder Test)");
			Console.WriteLine("");

			// Считываем минимальное количество томов
			Console.Write("Enter MINIMUM count of volumes: ");
			int minVolCount = Convert.ToInt16(Console.ReadLine());

			// Считываем максимальное количество томов
			Console.Write("Enter MAXIMUM count of volumes: ");
			int maxVolCount = Convert.ToInt16(Console.ReadLine());
			Console.WriteLine("");

			// Если пользователь перепутал максимум и минимум - меняем их местами
			if(maxVolCount < minVolCount)
			{
				int temp = maxVolCount;
				maxVolCount = minVolCount;
				minVolCount = temp;
			}

			// Количество итераций c флагом OK
			int OKCount = 0;

			// Количество итераций c флагом Error
			int ErrorCount = 0;

			// Общее количество итераций
			int TotalCount = 0;

			while(true)
			{
				// Устанавливаем случайное количество томов
				int allVolCount = minVolCount + eRandom.Next((maxVolCount - minVolCount) + 1);

				// Количество томов для восстановления не превышает количество томов данных
				int eccCount = 1 + eRandom.Next((allVolCount / 2) - 1);

				// Количество томов данных находим по остаточному принципу
				int dataCount = allVolCount - eccCount;

				// Формируем список томов (данные и избыточность)
				ArrayList allVolList = new ArrayList(allVolCount);
				for(int i = 0; i < allVolCount; i++) allVolList.Add(i);

				// Задействуем все тома для восстановления для повышения вероятности
				// получения сингулярной комбинации
				int nErasures = eccCount;

				// Повреждаем только тома данных!
				for(int i = 0; i < nErasures; i++) allVolList.RemoveAt(eRandom.Next(allVolList.Count - eccCount));

				// Формируем задание для декодера...
				int[] volList = new int[dataCount];

				// Копируем список нужных выживших томов в массив для декодера...
				for(int i = 0; i < dataCount; i++) volList[i] = (int)allVolList[i];

				// Устанавливаем конфигурацию декодера...
				eRSRaidDecoder.SetConfig(dataCount, eccCount, volList, (int)RSType.Cauchy);

				// Инициализируем поиск обратной матрицы...
				if(!eRSRaidDecoder.Prepare(false))
				{
					// Сбрасываем в файл данные ошибочной конфигурации
					String logFileName = "Error " + DateTime.Now.ToString().Replace(':', '.') + ".txt";

					File.AppendAllText(logFileName, "dataCount:" + Convert.ToString(dataCount + "; "), Encoding.ASCII);
					File.AppendAllText(logFileName, "eccCount:" + Convert.ToString(eccCount + "; "), Encoding.ASCII);

					for(int i = 0; i < dataCount; i++)
					{
						if(volList[i] < dataCount)
						{
							File.AppendAllText(logFileName, "d:" + Convert.ToString(volList[i] + "; "), Encoding.ASCII);
						}
						else
						{
							File.AppendAllText(logFileName, "e:" + Convert.ToString(volList[i] + "; "), Encoding.ASCII);
						}
					}

					ErrorCount++;
				}

				TotalCount++;
				OKCount = TotalCount - ErrorCount;

				if((TotalCount % 100) == 0)
				{
					Console.WriteLine("OK: " + Convert.ToString(OKCount) + ", " +
					                  "Errors: " + Convert.ToString(ErrorCount) + ";");
				}
			}
		}
	}
}