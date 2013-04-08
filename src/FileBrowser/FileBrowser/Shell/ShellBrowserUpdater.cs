using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace ShellDll
{
	class ShellBrowserUpdater : NativeWindow
	{
		#region Fields

		private uint notifyId = 0;

		#endregion

		public ShellBrowserUpdater()
		{
		}

		~ShellBrowserUpdater()
		{
			if(notifyId > 0)
			{
				ShellAPI.SHChangeNotifyDeregister(notifyId);
				GC.SuppressFinalize(this);
			}
		}

		protected override void WndProc(ref Message m)
		{
			if(m.Msg == (int)ShellAPI.WM.SH_NOTIFY)
			{
				ShellAPI.SHNOTIFYSTRUCT shNotify =
					(ShellAPI.SHNOTIFYSTRUCT)Marshal.PtrToStructure(m.WParam, typeof(ShellAPI.SHNOTIFYSTRUCT));

				//Console.Out.WriteLine("Event: {0}", (ShellAPI.SHCNE)m.LParam);
				//if (shNotify.dwItem1 != IntPtr.Zero)
				//PIDL.Write(shNotify.dwItem1);
				//if (shNotify.dwItem2 != IntPtr.Zero)
				//PIDL.Write(shNotify.dwItem2);

				switch((ShellAPI.SHCNE)m.LParam)
				{
						#region File Changes

					case ShellAPI.SHCNE.CREATE:

						#region Create Item

						{
							if(!PIDL.IsEmpty(shNotify.dwItem1))
							{
								IntPtr parent, child;
								PIDL.SplitPidl(shNotify.dwItem1, out parent, out child);
								PIDL parentPIDL = new PIDL(parent, false);
								Marshal.FreeCoTaskMem(child);
								parentPIDL.Free();
							}
						}

						#endregion

						break;

					case ShellAPI.SHCNE.RENAMEITEM:

						break;

					case ShellAPI.SHCNE.DELETE:

						#region Delete Item

						{
							if(!PIDL.IsEmpty(shNotify.dwItem1))
							{
								IntPtr parent, child;
								PIDL.SplitPidl(shNotify.dwItem1, out parent, out child);
								PIDL parentPIDL = new PIDL(parent, false);
								Marshal.FreeCoTaskMem(child);
								parentPIDL.Free();
							}
						}

						#endregion

						break;

					case ShellAPI.SHCNE.UPDATEITEM:

						break;

						#endregion

						#region Folder Changes

					case ShellAPI.SHCNE.MKDIR:
					case ShellAPI.SHCNE.DRIVEADD:
					case ShellAPI.SHCNE.DRIVEADDGUI:

						#region Make Directory

						{
							if(!PIDL.IsEmpty(shNotify.dwItem1))
							{
								IntPtr parent, child;
								PIDL.SplitPidl(shNotify.dwItem1, out parent, out child);
								PIDL parentPIDL = new PIDL(parent, false);
								Marshal.FreeCoTaskMem(child);
								parentPIDL.Free();
							}
						}

						#endregion

						break;

					case ShellAPI.SHCNE.RENAMEFOLDER:

						break;

					case ShellAPI.SHCNE.RMDIR:
					case ShellAPI.SHCNE.DRIVEREMOVED:

						#region Remove Directory

						{
							if(!PIDL.IsEmpty(shNotify.dwItem1))
							{
								IntPtr parent, child;
								PIDL.SplitPidl(shNotify.dwItem1, out parent, out child);
								PIDL parentPIDL = new PIDL(parent, false);
								Marshal.FreeCoTaskMem(child);
								parentPIDL.Free();
							}
						}

						#endregion

						break;

					case ShellAPI.SHCNE.UPDATEDIR:
					case ShellAPI.SHCNE.ATTRIBUTES:

						break;

					case ShellAPI.SHCNE.MEDIAINSERTED:
					case ShellAPI.SHCNE.MEDIAREMOVED:

						break;

						#endregion

						#region Other Changes

					case ShellAPI.SHCNE.ASSOCCHANGED:

						#region Update Images

						{
						}

						#endregion

						break;

					case ShellAPI.SHCNE.NETSHARE:
					case ShellAPI.SHCNE.NETUNSHARE:
						break;

					case ShellAPI.SHCNE.UPDATEIMAGE:
						UpdateRecycleBin();
						break;

						#endregion
				}
			}

			base.WndProc(ref m);
		}

		private void UpdateRecycleBin(){}
	}
}