/*----------------------------------------------------------------------+
 |  filename:   PasswordForm.cs                                         |
 |----------------------------------------------------------------------|
 |  version:    2.21                                                    |
 |  revision:   24.08.2012 15:52                                        |
 |  authors:    Дробанов Артём Федорович (DrAF),                        |
 |              RUSpectrum (г. Оренбург).                               |
 |  e-mail:     draf@mail.ru                                            |
 |  purpose:    Ввод/считывание пароля с поддержкой очистки памяти      |
 +----------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

namespace RecoveryStar
{
	public partial class PasswordForm : Form
	{
		#region Constants

		/// <summary>
		/// Размер CBC-блока по-умолчанию (128 Мб)
		/// </summary>
		private const String CBCBlockSizeDefValue = "131072";

		/// <summary>
		/// Минимальная длина пароля
		/// </summary>
		private const int minPasswordLength = 3;

		#endregion Constants

		#region Public Properties & Data

		/// <summary>
		/// Введенный пароль
		/// </summary>
		public String Password
		{
			get { return passwordTextBox1.Text; }
		}

		/// <summary>
		/// Размер CBC-блока (Кб), используемый при шифровании
		/// </summary>
		public int CBCBlockSize
		{
			get
			{
				if(CBCBlockSizeTextBox.Text.Length != 0)
				{
					return Convert.ToInt32(CBCBlockSizeTextBox.Text);
				}
				else
				{
					return 1;
				}
			}
		}

		#endregion Public Properties & Data

		#region Construction & Destruction

		/// <summary>
		/// Конструктор формы
		/// </summary>
		public PasswordForm()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Деструктор формы
		/// </summary>
		~PasswordForm()
		{
			ClearPassword();
		}

		#endregion Construction & Destruction

		#region Public Operations

		/// <summary>
		/// Метод очистки пароля
		/// </summary>
		public void ClearPassword()
		{
			passwordTextBox1.Clear();
			passwordTextBox2.Clear();
			passwordTextBox1.Focus();
		}

		/// <summary>
		/// Проверка введенных паролей на идентичность
		/// </summary>
		public bool TestPassword()
		{
			int minTextLength = passwordTextBox1.TextLength < passwordTextBox2.TextLength ? passwordTextBox1.TextLength : passwordTextBox2.TextLength;

			for(int i = 0; i < minTextLength; i++)
			{
				if(passwordTextBox1.Text[i] != passwordTextBox2.Text[i])
				{
					password1Label.ForeColor = Color.Red;
					password2Label.ForeColor = Color.Red;

					return false;
				}
			}

			bool flag1 = passwordTextBox1.TextLength >= minPasswordLength;
			bool flag2 = passwordTextBox2.TextLength >= minPasswordLength;

			if(flag1 && flag2 && (passwordTextBox1.Text == passwordTextBox2.Text))
			{
				password1Label.ForeColor = Color.Green;
				password2Label.ForeColor = Color.Green;

				return true;
			}

			password1Label.ForeColor = flag1 ? Color.Blue : Color.Black;
			password2Label.ForeColor = flag2 ? Color.Blue : Color.Black;

			return false;
		}

		#endregion Public Operations

		#region Private Operations

		/// <summary>
		/// Обработчик события "Тик таймера для указания текущего языка ввода"
		/// </summary>
		private void LangTimer_Tick(object sender, EventArgs e)
		{
			SetLanguage();
		}

		/// <summary>
		/// Установка языка
		/// </summary>
		private void SetLanguage()
		{
			try
			{
				String languageName = Application.CurrentInputLanguage.Culture.TwoLetterISOLanguageName.ToUpper();
				if(LanguageLabel.Text != languageName)
				{
					LanguageLabel.Text = languageName;
				}
			}
			catch
			{
			}
		}

		/// <summary>
		/// Обработчик нажатия на клавишу в поле ввода пароля №1 и №2
		/// </summary>
		private void passwordTextBoxes_KeyDown(object sender, KeyEventArgs e)
		{
			switch(e.KeyData)
			{
					// При любой попытке стирания (т.к. неизвестно как именно стирается символ)
					// активируется полное стирание паролей
				case Keys.Back:
				case Keys.Delete:
					{
						ClearPassword();

						return;
					}

					// Нажатие "Enter"
				case Keys.Enter:
					{
						// Проверяем оба введенных пароля на идентичность
						if(TestPassword())
						{
							LanguageTimer.Stop();
							Close();
						}

						return;
					}

					// Нажатие "Esc"
				case Keys.Escape:
					{
						ClearPassword();
						CBCBlockSizeTextBox.Text = CBCBlockSizeDefValue;
						Close();

						return;
					}
			}
		}

		/// <summary>
		/// Обработчик изменения текста
		/// </summary>
		private void passwordTextBoxes_TextChanged(object sender, EventArgs e)
		{
			TestPassword();
		}

		/// <summary>
		/// Обработчик нажатия на клавишу в поле ввода размера CBC-блока
		/// </summary>
		private void CBCBlockSizeTextBox_KeyDown(object sender, KeyEventArgs e)
		{
			switch(e.KeyData)
			{
				case Keys.Enter:
					{
						// В данном методе организованы доп. проверки строки
						CBCBlockSizeTextBox_KeyUp(sender, e);

						// Проверяем оба введенных пароля на идентичность
						if(TestPassword())
						{
							LanguageTimer.Stop();
							Close();
						}

						return;
					}

				case Keys.Escape:
					{
						ClearPassword();
						CBCBlockSizeTextBox.Text = CBCBlockSizeDefValue;
						Close();

						return;
					}
			}
		}

		/// <summary>
		/// Обработчик нажатия на клавишу в поле ввода размера CBC-блока
		/// </summary>
		private void CBCBlockSizeTextBox_KeyUp(object sender, KeyEventArgs e)
		{
			try
			{
				if(CBCBlockSizeTextBox.Text == "")
				{
					CBCBlockSizeTextBox.Text = "1";

					// Переводим каретку на конец строки
					CBCBlockSizeTextBox.Select(CBCBlockSizeTextBox.Text.Length, CBCBlockSizeTextBox.Text.Length);
				}

				CBCBlockSizeTextBox.Text = Convert.ToString(Math.Abs(Convert.ToInt32(CBCBlockSizeTextBox.Text)));

				if(CBCBlockSizeTextBox.Text == "0")
				{
					CBCBlockSizeTextBox.Text = "1";
				}
			}

			catch
			{
				// Если пользователь ввел более одного символа - есть что сохранять -
				// в случае обработки исключения просто сокращаем строку на символ...
				if(CBCBlockSizeTextBox.Text.Length > 1)
				{
					CBCBlockSizeTextBox.Text = CBCBlockSizeTextBox.Text.Substring(0, (CBCBlockSizeTextBox.Text.Length - 1));
				}
				else
				{
					//...а иначе затираем строку
					CBCBlockSizeTextBox.Text = "";
				}

				// Переводим каретку на конец строки
				CBCBlockSizeTextBox.Select(CBCBlockSizeTextBox.Text.Length, CBCBlockSizeTextBox.Text.Length);
			}
		}

		/// <summary>
		/// Обработчик закрытия формы
		/// </summary>
		private void PasswordForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			// При закрытии формы проверяем соответствие введенных паролей
			if(passwordTextBox1.Text != passwordTextBox2.Text)
			{
				ClearPassword();
			}
		}

		/// <summary>
		/// Перенос фокуса на первое поле ввода пароля при показе формы
		/// </summary>
		private void PasswordForm_Shown(object sender, EventArgs e)
		{
			CBCBlockSizeTextBox.Text = CBCBlockSizeDefValue;
			SetLanguage();
			passwordTextBox1.Focus();
			LanguageTimer.Start();
		}

		#endregion Private Operations
	}
}