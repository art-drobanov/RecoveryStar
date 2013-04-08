/*----------------------------------------------------------------------+
 |  filename:   SystemInfo.cs                                           |
 |----------------------------------------------------------------------|
 |  version:    2.22                                                    |
 |  revision:   02.04.2013 17:00                                        |
 |  authors:    Дробанов Артём Федорович (DrAF),                        |
 |              RUSpectrum (г. Оренбург).                               |
 |  e-mail:     draf@mail.ru                                            |
 |  purpose:    Предоставление информации о системных ресурсах          |
 +----------------------------------------------------------------------*/

using System;
using System.Management;

namespace RecoveryStar
{
	/// <summary>
	/// Предоставление информации о системных ресурсах
	/// </summary>
	public class SystemInfo
	{
		#region Public Properties & Data

		/// <summary>
		/// Всего физической памяти
		/// </summary>
		public ulong TotalPhysicalMemory
		{
			get { return this.totalPhysicalMemory; }
		}

		/// <summary>
		/// Всего физической памяти
		/// </summary>
		private ulong totalPhysicalMemory = 1 << 26; // 64 Мбайт

		/// <summary>
		/// Свободно физической памяти
		/// </summary>
		public ulong FreePhysicalMemory
		{
			get { return this.freePhysicalMemory; }
		}

		/// <summary>
		/// Свободно физической памяти
		/// </summary>
		private ulong freePhysicalMemory = 1 << 25; // 32 Мбайт

		#endregion Public Properties & Data

		#region Construction & Destruction

		/// <summary>
		/// Конструктор класса
		/// </summary>
		public SystemInfo()
		{
			try
			{
				ManagementScope managementScope1 = new ManagementScope();
				ManagementObjectSearcher managementObjectSearcher1 = new ManagementObjectSearcher(managementScope1, new ObjectQuery("SELECT * FROM Win32_PhysicalMemory"));
				foreach(ManagementObject BankRAM in managementObjectSearcher1.Get()) this.totalPhysicalMemory = (ulong)BankRAM.GetPropertyValue("Capacity");
			}
			catch
			{
				this.totalPhysicalMemory = 1 << 26; // 64 Мбайт
			}

			try
			{
				ManagementScope managementScope2 = new ManagementScope();
				ManagementObjectSearcher managementObjectSearcher2 = new ManagementObjectSearcher(managementScope2, new ObjectQuery("SELECT * FROM Win32_OperatingSystem"));
				foreach(ManagementObject OS in managementObjectSearcher2.Get())
				{
					// << 10 - килобайты переводим в байты (2^10 = 1024)
					this.freePhysicalMemory = (ulong)OS.GetPropertyValue("FreePhysicalMemory") << 10;
					break;
				}
			}
			catch
			{
				this.freePhysicalMemory = 1 << 25; // 32 Мбайт
			}
		}

		#endregion Construction & Destruction
	}
}