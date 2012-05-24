using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace PinkieControls
{
	public class WinAPI
	{
		#region Interop definitions

		[DllImport("UxTheme.dll", CharSet = CharSet.Unicode)]
		public static extern int GetCurrentThemeName(StringBuilder
		                                             	pszThemeFileName, int dwMaxNameChars,
		                                             StringBuilder pszColorBuff, int cchMaxColorChars,
		                                             StringBuilder pszSizeBuff, int cchMaxSizeChars);

		[DllImport("UxTheme.dll")]
		public static extern bool IsAppThemed();

		#endregion

		#region Constructor

		public WinAPI()
		{
		}

		#endregion
	}
}