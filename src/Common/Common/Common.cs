/*----------------------------------------------------------------------+
 |  filename:   Common.cs                                               |
 |----------------------------------------------------------------------|
 |  version:    2.22                                                    |
 |  revision:   02.04.2013 17:00                                        |
 |  authors:    Дробанов Артём Федорович (DrAF),                        |
 |              RUSpectrum (г. Оренбург).                               |
 |  e-mail:     draf@mail.ru                                            |
 |  purpose:    Общие описания и определения проекта Recovery Star      |
 +----------------------------------------------------------------------*/

using System;

namespace RecoveryStar
{
	/// <summary>
	/// Делегат обновления строкового значения
	/// </summary>
	public delegate void OnUpdateStringValueHandler(String text);

	/// <summary>
	/// Делегат обновления числового значения
	/// </summary>
	public delegate void OnUpdateDoubleValueHandler(double value);

	/// <summary>
	/// Делегат вывода строкового значения вместе с числовым
	/// </summary>
	public delegate void OnUpdateStringAndDoubleValueHandler(String text, double value);

	/// <summary>
	/// Делегат вывода двух значений типа double
	/// </summary>
	public delegate void OnUpdateTwoDoubleValueHandler(double value1, double value2);

	/// <summary>
	/// Делегат вывода двух значений типа int и double
	/// </summary>
	public delegate void OnUpdateTwoIntDoubleValueHandler(int intValue1, int intValue2, double doubleValue1, double doubleValue2);

	/// <summary>
	/// Делегат без параметров
	/// </summary>
	public delegate void OnEventHandler();

	/// <summary>
	/// Перечисление возможных времен ожидания завершения потока (в циклах)
	/// </summary>
	public enum WaitCount
	{
		MinWaitCount = 600,
		MaxWaitCount = 6000
	};

	/// <summary>
	/// Перечисление возможных времен засыпания потока (в тысячных долях секунды)
	/// </summary>
	public enum WaitTime
	{
		MinWaitTime = 100,
		MaxWaitTime = 1000
	};

	/// <summary>
	/// Режимы работы ("Не установлен", "Защита", "Восстановление", "Лечение", "Тестирование")
	/// </summary>
	public enum RSMode
	{
		None,
		Protect,
		Recover,
		Repair,
		Test
	};

	/// <summary>
	/// Типы кодека Рида-Соломона (по типу используемой матрицы кодирования)
	/// </summary>
	public enum RSType
	{
		Dispersal,
		Alternative,
		Cauchy
	};

	/// <summary>
	/// Константы кодера Рида-Соломона (MaxVolCount - максимальное количество томов для режима
	/// с дисперсной матрицей и Коши, MaxVolCountAlt - максимальное количество томов для "альтернативного"
	/// режима)
	/// </summary>
	public enum RSConst
	{
		MaxVolCount = 65535,
		MaxVolCountAlt = 32768
	};

	/// <summary>
	/// Порог входа по количеству томов, ниже которого параллельное выполнение не используется
	/// </summary>
	public enum RSParallelEdge
	{
		Value = 128
	};
}