using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Collections;
using System.Drawing;

using ShellDll;

using System.Threading;
using System.ComponentModel;
using System.IO;

namespace FileBrowser
{
	/// <summary>
	/// This class takes care of showing ContextMenu's for a BrowserTreeView
	/// </summary>
	class BrowserTVContextMenuWrapper : NativeWindow
	{
		#region Fields

		// The browser for which to provide the context menu's
		private Browser br;

		// If this bool is true the next time the context menu has to be shown will be cancelled
		private bool suspendContextMenu;

		// The interfaces for the needed context menu's
		private IContextMenu iContextMenu;
		private IContextMenu2 iContextMenu2;
		private IContextMenu3 iContextMenu3;

		private bool contextMenuVisible;

		// The cmd for a custom added menu item
		private enum CMD_CUSTOM
		{
			ExpandCollapse = (int)ShellAPI.CMD_LAST + 1
		}

		#endregion

		/// <summary>
		/// Registers the neccesairy events
		/// </summary>
		/// <param name="br">The browser for which to support the ContextMenu</param>
		public BrowserTVContextMenuWrapper(Browser br)
		{
			this.br = br;

			br.FolderView.MouseUp += new System.Windows.Forms.MouseEventHandler(FolderView_MouseUp);
			br.FolderView.AfterLabelEdit += new NodeLabelEditEventHandler(FolderView_AfterLabelEdit);
			br.FolderView.BeforeLabelEdit += new NodeLabelEditEventHandler(FolderView_BeforeLabelEdit);
			br.FolderView.KeyDown += new KeyEventHandler(FolderView_KeyDown);

			this.CreateHandle(new CreateParams());
		}

		#region Public

		public bool SuspendContextMenu
		{
			get { return suspendContextMenu; }
			set { suspendContextMenu = value; }
		}

		#endregion

		#region Override

		/// <summary>
		/// This method receives WindowMessages. It will make the "Open With" and "Send To" work 
		/// by calling HandleMenuMsg and HandleMenuMsg2. It will also call the OnContextMenuMouseHover 
		/// method of Browser when hovering over a ContextMenu item.
		/// </summary>
		/// <param name="m">the Message of the Browser's WndProc</param>
		/// <returns>true if the message has been handled, false otherwise</returns>
		protected override void WndProc(ref Message m)
		{
			#region IContextMenu

			if(iContextMenu != null &&
			   m.Msg == (int)ShellAPI.WM.MENUSELECT &&
			   ((int)ShellHelper.HiWord(m.WParam) & (int)ShellAPI.MFT.SEPARATOR) == 0 &&
			   ((int)ShellHelper.HiWord(m.WParam) & (int)ShellAPI.MFT.POPUP) == 0)
			{
				string info = string.Empty;

				if(ShellHelper.LoWord(m.WParam) == (int)CMD_CUSTOM.ExpandCollapse)
					info = "Expands or collapses the current selected item";
				else
				{
					info = ContextMenuHelper.GetCommandString(
						iContextMenu,
						ShellHelper.LoWord(m.WParam) - ShellAPI.CMD_FIRST,
						false);
				}

				br.OnContextMenuMouseHover(new ContextMenuMouseHoverEventArgs(info.ToString()));
			}

			#endregion

			#region IContextMenu2

			if(iContextMenu2 != null &&
			   (m.Msg == (int)ShellAPI.WM.INITMENUPOPUP ||
			    m.Msg == (int)ShellAPI.WM.MEASUREITEM ||
			    m.Msg == (int)ShellAPI.WM.DRAWITEM))
			{
				if(iContextMenu2.HandleMenuMsg(
					(uint)m.Msg, m.WParam, m.LParam) == ShellAPI.S_OK)
					return;
			}

			#endregion

			#region IContextMenu3

			if(iContextMenu3 != null &&
			   m.Msg == (int)ShellAPI.WM.MENUCHAR)
			{
				if(iContextMenu3.HandleMenuMsg2(
					(uint)m.Msg, m.WParam, m.LParam, IntPtr.Zero) == ShellAPI.S_OK)
					return;
			}

			#endregion

			base.WndProc(ref m);
		}

		#endregion

		#region Events

		private void FolderView_KeyDown(object sender, KeyEventArgs e)
		{
			ContextMenuHelper.ProcessKeyCommands(br, sender, e);
		}

		private void FolderView_BeforeLabelEdit(object sender, NodeLabelEditEventArgs e)
		{
			ShellItem item = e.Node.Tag as ShellItem;

			if(!item.CanRename)
			{
				e.CancelEdit = true;
				System.Media.SystemSounds.Beep.Play();
			}
			if(item.IsDisk)
			{
				IntPtr editHandle = ShellAPI.SendMessage(br.FolderView.Handle, ShellAPI.WM.TVM_GETEDITCONTROL, 0, IntPtr.Zero);
				ShellAPI.SendMessage(editHandle, ShellAPI.WM.SETTEXT, 0,
				                     Marshal.StringToHGlobalAuto(item.Text.Substring(0, item.Text.LastIndexOf(' '))));
			}
		}

		private void FolderView_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
		{
			ShellItem item = e.Node.Tag as ShellItem;

			IntPtr newPidl = IntPtr.Zero;
			if(e.Label != null && !(item.IsDisk && item.Text.Substring(0, item.Text.LastIndexOf(' ')) == e.Label) &&
			   item.ParentItem.ShellFolder.SetNameOf(
			   	br.Handle,
			   	item.PIDLRel.Ptr,
			   	e.Label,
			   	ShellAPI.SHGNO.NORMAL,
			   	out newPidl) == ShellAPI.S_OK)
			{
				item.Update(newPidl, ShellItemUpdateType.Renamed);
			}
			else
			{
				e.CancelEdit = true;
			}

			br.FolderView.LabelEdit = false;
		}

		/// <summary>
		/// When the mouse goes up on an node and suspendContextMenu is true, this method will show the
		/// ContextMenu for that node and after the user selects an item, it will execute that command.
		/// </summary>
		private void FolderView_MouseUp(object sender, MouseEventArgs e)
		{
			if(suspendContextMenu || contextMenuVisible)
			{
				suspendContextMenu = false;
				return;
			}

			TreeViewHitTestInfo hitTest = br.FolderView.HitTest(e.Location);

			contextMenuVisible = true;
			if(e.Button == MouseButtons.Right &&
			   (hitTest.Location == TreeViewHitTestLocations.Image ||
			    hitTest.Location == TreeViewHitTestLocations.Label ||
			    hitTest.Location == TreeViewHitTestLocations.StateImage))
			{
				#region Fields

				ShellItem item = (ShellItem)hitTest.Node.Tag;

				IntPtr contextMenu = IntPtr.Zero,
				       iContextMenuPtr = IntPtr.Zero,
				       iContextMenuPtr2 = IntPtr.Zero,
				       iContextMenuPtr3 = IntPtr.Zero;
				IShellFolder parentShellFolder =
					(item.ParentItem != null) ? item.ParentItem.ShellFolder : item.ShellFolder;

				#endregion

				#region Show / Invoke

				try
				{
					if(ContextMenuHelper.GetIContextMenu(parentShellFolder, new IntPtr[] {item.PIDLRel.Ptr},
					                                     out iContextMenuPtr, out iContextMenu))
					{
						contextMenu = ShellAPI.CreatePopupMenu();

						iContextMenu.QueryContextMenu(
							contextMenu,
							0,
							ShellAPI.CMD_FIRST,
							ShellAPI.CMD_LAST,
							ShellAPI.CMF.EXPLORE |
							ShellAPI.CMF.CANRENAME |
							((Control.ModifierKeys & Keys.Shift) != 0 ? ShellAPI.CMF.EXTENDEDVERBS : 0));

						string topInvoke = hitTest.Node.IsExpanded ? "Collapse" : "Expand";
						ShellAPI.MFT extraFlag = (hitTest.Node.Nodes.Count > 0) ? 0 : ShellAPI.MFT.GRAYED;
						ShellAPI.InsertMenu(contextMenu, 0,
						                    ShellAPI.MFT.BYPOSITION | extraFlag,
						                    (int)CMD_CUSTOM.ExpandCollapse, topInvoke);
						ShellAPI.InsertMenu(contextMenu, 1,
						                    ShellAPI.MFT.BYPOSITION | ShellAPI.MFT.SEPARATOR,
						                    0, "-");

						ShellAPI.SetMenuDefaultItem(
							contextMenu,
							0,
							true);

						Marshal.QueryInterface(iContextMenuPtr, ref ShellAPI.IID_IContextMenu2, out iContextMenuPtr2);
						Marshal.QueryInterface(iContextMenuPtr, ref ShellAPI.IID_IContextMenu3, out iContextMenuPtr3);

						try
						{
							iContextMenu2 =
								(IContextMenu2)Marshal.GetTypedObjectForIUnknown(iContextMenuPtr2, typeof(IContextMenu2));

							iContextMenu3 =
								(IContextMenu3)Marshal.GetTypedObjectForIUnknown(iContextMenuPtr3, typeof(IContextMenu3));
						}
						catch(Exception)
						{
						}

						Point ptInvoke = br.FolderView.PointToScreen(e.Location);
						uint selected = ShellAPI.TrackPopupMenuEx(
							contextMenu,
							ShellAPI.TPM.RETURNCMD,
							ptInvoke.X,
							ptInvoke.Y,
							this.Handle,
							IntPtr.Zero);

						br.OnContextMenuMouseHover(new ContextMenuMouseHoverEventArgs(string.Empty));

						if(selected == (int)CMD_CUSTOM.ExpandCollapse)
						{
							if(hitTest.Node.IsExpanded)
								hitTest.Node.Collapse(true);
							else
								hitTest.Node.Expand();
						}
						else if(selected >= ShellAPI.CMD_FIRST)
						{
							string command = ContextMenuHelper.GetCommandString(
								iContextMenu,
								selected - ShellAPI.CMD_FIRST,
								true);

							if(command == "rename")
							{
								br.FolderView.LabelEdit = true;
								hitTest.Node.BeginEdit();
							}
							else
							{
								ContextMenuHelper.InvokeCommand(
									iContextMenu,
									selected - ShellAPI.CMD_FIRST,
									(item.ParentItem != null) ?
									                          	ShellItem.GetRealPath(item.ParentItem) : ShellItem.GetRealPath(item),
									ptInvoke);
							}
						}
					}
				}
					#endregion

				catch(Exception)
				{
				}
					#region Finally

				finally
				{
					if(iContextMenu != null)
					{
						Marshal.ReleaseComObject(iContextMenu);
						iContextMenu = null;
					}

					if(iContextMenu2 != null)
					{
						Marshal.ReleaseComObject(iContextMenu2);
						iContextMenu2 = null;
					}

					if(iContextMenu3 != null)
					{
						Marshal.ReleaseComObject(iContextMenu3);
						iContextMenu3 = null;
					}

					if(contextMenu != null)
						ShellAPI.DestroyMenu(contextMenu);

					if(iContextMenuPtr != IntPtr.Zero)
						Marshal.Release(iContextMenuPtr);

					if(iContextMenuPtr2 != IntPtr.Zero)
						Marshal.Release(iContextMenuPtr2);

					if(iContextMenuPtr3 != IntPtr.Zero)
						Marshal.Release(iContextMenuPtr3);
				}

				#endregion
			}
			contextMenuVisible = false;
		}

		#endregion
	}

	#region ContextMenuHelper

	/// <summary>
	/// This class provides static methods which are being used to retrieve IContextMenu's for specific items
	/// and to invoke certain commands.
	/// </summary>
	static class ContextMenuHelper
	{
		#region GetCommandString

		public static string GetCommandString(IContextMenu iContextMenu, uint idcmd, bool executeString)
		{
			string command = GetCommandStringW(iContextMenu, idcmd, executeString);

			if(string.IsNullOrEmpty(command))
				command = GetCommandStringA(iContextMenu, idcmd, executeString);

			return command;
		}

		/// <summary>
		/// Retrieves the command string for a specific item from an iContextMenu (Ansi)
		/// </summary>
		/// <param name="iContextMenu">the IContextMenu to receive the string from</param>
		/// <param name="idcmd">the id of the specific item</param>
		/// <param name="executeString">indicating whether it should return an execute string or not</param>
		/// <returns>if executeString is true it will return the executeString for the item, 
		/// otherwise it will return the help info string</returns>
		public static string GetCommandStringA(IContextMenu iContextMenu, uint idcmd, bool executeString)
		{
			string info = string.Empty;
			byte[] bytes = new byte[256];
			int index;

			iContextMenu.GetCommandString(
				idcmd,
				(executeString ? ShellAPI.GCS.VERBA : ShellAPI.GCS.HELPTEXTA),
				0,
				bytes,
				ShellAPI.MAX_PATH);

			index = 0;
			while(index < bytes.Length && bytes[index] != 0)
			{
				index++;
			}

			if(index < bytes.Length)
				info = Encoding.Default.GetString(bytes, 0, index);

			return info;
		}

		/// <summary>
		/// Retrieves the command string for a specific item from an iContextMenu (Unicode)
		/// </summary>
		/// <param name="iContextMenu">the IContextMenu to receive the string from</param>
		/// <param name="idcmd">the id of the specific item</param>
		/// <param name="executeString">indicating whether it should return an execute string or not</param>
		/// <returns>if executeString is true it will return the executeString for the item, 
		/// otherwise it will return the help info string</returns>
		public static string GetCommandStringW(IContextMenu iContextMenu, uint idcmd, bool executeString)
		{
			string info = string.Empty;
			byte[] bytes = new byte[256];
			int index;

			iContextMenu.GetCommandString(
				idcmd,
				(executeString ? ShellAPI.GCS.VERBW : ShellAPI.GCS.HELPTEXTW),
				0,
				bytes,
				ShellAPI.MAX_PATH);

			index = 0;
			while(index < bytes.Length - 1 && (bytes[index] != 0 || bytes[index + 1] != 0))
			{
				index += 2;
			}

			if(index < bytes.Length - 1)
				info = Encoding.Unicode.GetString(bytes, 0, index + 1);

			return info;
		}

		#endregion

		#region Invoke Commands

		/// <summary>
		/// Invokes a specific command from an IContextMenu
		/// </summary>
		/// <param name="iContextMenu">the IContextMenu containing the item</param>
		/// <param name="cmd">the index of the command to invoke</param>
		/// <param name="parentDir">the parent directory from where to invoke</param>
		/// <param name="ptInvoke">the point (in screen coördinates) from which to invoke</param>
		public static void InvokeCommand(IContextMenu iContextMenu, uint cmd, string parentDir, Point ptInvoke)
		{
			ShellAPI.CMINVOKECOMMANDINFOEX invoke = new ShellAPI.CMINVOKECOMMANDINFOEX();
			invoke.cbSize = ShellAPI.cbInvokeCommand;
			invoke.lpVerb = (IntPtr)cmd;
			invoke.lpDirectory = parentDir;
			invoke.lpVerbW = (IntPtr)cmd;
			invoke.lpDirectoryW = parentDir;
			invoke.fMask = ShellAPI.CMIC.UNICODE | ShellAPI.CMIC.PTINVOKE |
			               ((Control.ModifierKeys & Keys.Control) != 0 ? ShellAPI.CMIC.CONTROL_DOWN : 0) |
			               ((Control.ModifierKeys & Keys.Shift) != 0 ? ShellAPI.CMIC.SHIFT_DOWN : 0);
			invoke.ptInvoke = new ShellAPI.POINT(ptInvoke.X, ptInvoke.Y);
			invoke.nShow = ShellAPI.SW.SHOWNORMAL;

			iContextMenu.InvokeCommand(ref invoke);
		}

		/// <summary>
		/// Invokes a specific command from an IContextMenu
		/// </summary>
		/// <param name="iContextMenu">the IContextMenu containing the item</param>
		/// <param name="cmdA">the Ansi execute string to invoke</param>
		/// <param name="cmdW">the Unicode execute string to invoke</param>
		/// <param name="parentDir">the parent directory from where to invoke</param>
		/// <param name="ptInvoke">the point (in screen coördinates) from which to invoke</param>
		public static void InvokeCommand(IContextMenu iContextMenu, string cmd, string parentDir, Point ptInvoke)
		{
			ShellAPI.CMINVOKECOMMANDINFOEX invoke = new ShellAPI.CMINVOKECOMMANDINFOEX();
			invoke.cbSize = ShellAPI.cbInvokeCommand;
			invoke.lpVerb = Marshal.StringToHGlobalAnsi(cmd);
			invoke.lpDirectory = parentDir;
			invoke.lpVerbW = Marshal.StringToHGlobalUni(cmd);
			invoke.lpDirectoryW = parentDir;
			invoke.fMask = ShellAPI.CMIC.UNICODE | ShellAPI.CMIC.PTINVOKE |
			               ((Control.ModifierKeys & Keys.Control) != 0 ? ShellAPI.CMIC.CONTROL_DOWN : 0) |
			               ((Control.ModifierKeys & Keys.Shift) != 0 ? ShellAPI.CMIC.SHIFT_DOWN : 0);
			invoke.ptInvoke = new ShellAPI.POINT(ptInvoke.X, ptInvoke.Y);
			invoke.nShow = ShellAPI.SW.SHOWNORMAL;

			iContextMenu.InvokeCommand(ref invoke);
		}

		/// <summary>
		/// Invokes a specific command for a set of pidls
		/// </summary>
		/// <param name="parent">the parent ShellItem which contains the pidls</param>
		/// <param name="pidls">the pidls from the items for which to invoke</param>
		/// <param name="cmd">the execute string from the command to invoke</param>
		/// <param name="ptInvoke">the point (in screen coördinates) from which to invoke</param>
		public static void InvokeCommand(ShellItem parent, IntPtr[] pidls, string cmd, Point ptInvoke)
		{
			IntPtr icontextMenuPtr;
			IContextMenu iContextMenu;

			if(GetIContextMenu(parent.ShellFolder, pidls, out icontextMenuPtr, out iContextMenu))
			{
				try
				{
					InvokeCommand(
						iContextMenu,
						cmd,
						ShellItem.GetRealPath(parent),
						ptInvoke);
				}
				catch(Exception)
				{
				}
				finally
				{
					if(iContextMenu != null)
						Marshal.ReleaseComObject(iContextMenu);

					if(icontextMenuPtr != IntPtr.Zero)
						Marshal.Release(icontextMenuPtr);
				}
			}
		}

		#endregion

		/// <summary>
		/// Retrieves the IContextMenu for specific items
		/// </summary>
		/// <param name="parent">the parent IShellFolder which contains the items</param>
		/// <param name="pidls">the pidls of the items for which to retrieve the IContextMenu</param>
		/// <param name="icontextMenuPtr">the pointer to the IContextMenu</param>
		/// <param name="iContextMenu">the IContextMenu for the items</param>
		/// <returns>true if the IContextMenu has been retrieved succesfully, false otherwise</returns>
		public static bool GetIContextMenu(
			IShellFolder parent,
			IntPtr[] pidls,
			out IntPtr iContextMenuPtr,
			out IContextMenu iContextMenu)
		{
			if(parent.GetUIObjectOf(
				IntPtr.Zero,
				(uint)pidls.Length,
				pidls,
				ref ShellAPI.IID_IContextMenu,
				IntPtr.Zero,
				out iContextMenuPtr) == ShellAPI.S_OK)
			{
				iContextMenu =
					(IContextMenu)Marshal.GetTypedObjectForIUnknown(
						iContextMenuPtr, typeof(IContextMenu));

				return true;
			}
			else
			{
				iContextMenuPtr = IntPtr.Zero;
				iContextMenu = null;

				return false;
			}
		}

		public static bool GetNewContextMenu(ShellItem item, out IntPtr iContextMenuPtr, out IContextMenu iContextMenu)
		{
			if(ShellAPI.CoCreateInstance(
				ref ShellAPI.CLSID_NewMenu,
				IntPtr.Zero,
				ShellAPI.CLSCTX.INPROC_SERVER,
				ref ShellAPI.IID_IContextMenu,
				out iContextMenuPtr) == ShellAPI.S_OK)
			{
				iContextMenu = Marshal.GetTypedObjectForIUnknown(iContextMenuPtr, typeof(IContextMenu)) as IContextMenu;

				IntPtr iShellExtInitPtr;
				if(Marshal.QueryInterface(
					iContextMenuPtr,
					ref ShellAPI.IID_IShellExtInit,
					out iShellExtInitPtr) == ShellAPI.S_OK)
				{
					IShellExtInit iShellExtInit = Marshal.GetTypedObjectForIUnknown(
						iShellExtInitPtr, typeof(IShellExtInit)) as IShellExtInit;

					PIDL pidlFull = item.PIDLFull;
					iShellExtInit.Initialize(pidlFull.Ptr, IntPtr.Zero, 0);

					Marshal.ReleaseComObject(iShellExtInit);
					Marshal.Release(iShellExtInitPtr);
					pidlFull.Free();

					return true;
				}
				else
				{
					if(iContextMenu != null)
					{
						Marshal.ReleaseComObject(iContextMenu);
						iContextMenu = null;
					}

					if(iContextMenuPtr != IntPtr.Zero)
					{
						Marshal.Release(iContextMenuPtr);
						iContextMenuPtr = IntPtr.Zero;
					}

					return false;
				}
			}
			else
			{
				iContextMenuPtr = IntPtr.Zero;
				iContextMenu = null;
				return false;
			}
		}

		/// <summary>
		/// When keys are pressed, this method will check for known key combinations. For example copy and past with
		/// Ctrl + C and Ctrl + V.
		/// </summary>
		public static void ProcessKeyCommands(Browser br, object sender, KeyEventArgs e)
		{
			if(e.Control && !e.Shift && !e.Alt)
			{
				switch(e.KeyCode)
				{
					case Keys.C:
					case Keys.Insert:
					case Keys.V:
					case Keys.X:

						#region Copy/Paste/Cut

						{
							Cursor.Current = Cursors.WaitCursor;
							IntPtr[] pidls;
							ShellItem parent;

							pidls = new IntPtr[1];
							pidls[0] = br.SelectedItem.PIDLRel.Ptr;
							parent = (br.SelectedItem.ParentItem != null ? br.SelectedItem.ParentItem : br.SelectedItem);

							if(pidls.Length > 0)
							{
								string cmd;
								if(e.KeyCode == Keys.C || e.KeyCode == Keys.Insert)
									cmd = "copy";
								else if(e.KeyCode == Keys.V)
									cmd = "paste";
								else
									cmd = "cut";

								ContextMenuHelper.InvokeCommand(parent, pidls, cmd, new Point(0, 0));
								Cursor.Current = Cursors.Default;
							}
							e.Handled = true;
							e.SuppressKeyPress = true;
						}

						#endregion

						break;

					case Keys.N:

						#region Create New Folder

						if(!br.CreateNewFolder())
							System.Media.SystemSounds.Beep.Play();

						e.Handled = true;
						e.SuppressKeyPress = true;

						#endregion

						break;

					case Keys.Z:
						break;

					case Keys.Y:
						break;
				}
			}
			else
			{
				switch(e.KeyCode)
				{
					case Keys.Insert:

						#region Paste

						if(e.Shift && !e.Control && !e.Alt)
						{
							IntPtr[] pidls = new IntPtr[1];
							pidls[0] = br.SelectedItem.PIDLRel.Ptr;
							ShellItem parent = (br.SelectedItem.ParentItem != null ? br.SelectedItem.ParentItem : br.SelectedItem);
							ContextMenuHelper.InvokeCommand(parent, pidls, "paste", new Point(0, 0));
						}
						e.Handled = true;
						e.SuppressKeyPress = true;

						#endregion

						break;

					case Keys.Delete:

						#region Delete

						if(!e.Control && !e.Alt)
						{
							IntPtr[] pidls;
							ShellItem parent;
							pidls = new IntPtr[1];
							pidls[0] = br.SelectedItem.PIDLRel.Ptr;
							parent = (br.SelectedItem.ParentItem != null ? br.SelectedItem.ParentItem : br.SelectedItem);

							if(pidls.Length > 0)
								ContextMenuHelper.InvokeCommand(parent, pidls, "delete", new Point(0, 0));
						}
						e.Handled = true;
						e.SuppressKeyPress = true;

						#endregion

						break;

					case Keys.F2:

						#region Rename

						if(sender.Equals(br.FolderView))
						{
							if(br.FolderView.SelectedNode != null)
							{
								br.FolderView.LabelEdit = true;
								br.FolderView.SelectedNode.BeginEdit();
							}
						}

						#endregion

						break;

					case Keys.Back:

						#region Up

						{
							if(br.FolderView.SelectedNode != null && br.FolderView.SelectedNode.Parent != null)
								br.FolderView.SelectedNode = br.FolderView.SelectedNode.Parent;
						}
						e.Handled = true;
						e.SuppressKeyPress = true;

						#endregion

						break;
				}
			}
		}
	}

	#endregion
}