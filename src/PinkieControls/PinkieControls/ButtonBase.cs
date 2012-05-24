// Created by WiB July 2003
// shy_dream@yahoo.com
// Edited by Wes Haggard(AKA puzzlehacker)
// wes@puzzleware.net
// You may include the source code, modified source code, assembly
// within your own projects for either personal or commercial use 
// with the only one restriction:
// don't change the name of namespace and library.

using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Drawing.Design;

namespace PinkieControls
{
	public class ButtonBase : System.Windows.Forms.Control, IButtonControl
	{
		#region Fields

		private System.ComponentModel.Container components = null;

		public enum States
		{
			Normal,
			MouseOver,
			Pushed
		}

		protected States state = States.Normal;
		protected GraphicsPath path;
		protected ToolTip toolTip;
		private Rectangle bounds;
		private Image image;
		private StringFormat sf;
		private Color textColor = SystemColors.ControlText;
		private SolidBrush textBrush;
		private Point iPoint, tPoint;
		protected bool isDefault = false;
		private DialogResult dialogResult = DialogResult.None;

		#endregion

		#region Constructor

		public ButtonBase()
		{
			try
			{
				this.SetStyle(ControlStyles.DoubleBuffer, true);
				this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
				this.SetStyle(ControlStyles.UserPaint, true);
				this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
				this.SetStyle(ControlStyles.StandardDoubleClick, false);
				this.SetStyle(ControlStyles.Selectable, true);
				this.Size = new Size(10, 10);
				this.ResizeRedraw = true;
				this.toolTip = new ToolTip();
			}
			catch
			{
			}
		}

		#endregion

		#region Public Properties

		[DefaultValue(null),
		 System.ComponentModel.RefreshProperties(RefreshProperties.Repaint)]
		public Image Image
		{
			get { return image; }
			set
			{
				image = value;
				this.Invalidate();
			}
		}

		public override string Text
		{
			get { return base.Text; }
			set
			{
				base.Text = value;
				this.Invalidate();
			}
		}

		[DefaultValue(typeof(Color), "ControlText"),
		 System.ComponentModel.RefreshProperties(RefreshProperties.Repaint)]
		public Color TextColor
		{
			get { return textColor; }
			set
			{
				textColor = value;
				if(textBrush != null) textBrush.Dispose();
				textBrush = new SolidBrush(textColor);
				this.Invalidate();
			}
		}

		public String Hint
		{
			get { return toolTip.GetToolTip(this); }
			set
			{
				toolTip.RemoveAll();
				toolTip.SetToolTip(this, value);
			}
		}

		#endregion

		#region Protected Methods

		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				DisposePensBrushes();
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}


		protected override void OnMouseMove(MouseEventArgs e)
		{
			if(!bounds.Contains(e.X, e.Y)) state = States.Normal;
			else state = States.MouseOver;
			this.Invalidate(bounds);
			base.OnMouseMove(e);
		}

		protected override void OnMouseEnter(EventArgs e)
		{
			state = States.MouseOver;
			this.Invalidate(bounds);
			base.OnMouseEnter(e);
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			state = States.Normal;
			this.Invalidate();
			base.OnMouseLeave(e);
		}

		protected override void OnMouseDown(System.Windows.Forms.MouseEventArgs e)
		{
			if((e.Button & MouseButtons.Left) != MouseButtons.Left) return;

			if(bounds.Contains(e.X, e.Y))
			{
				state = States.Pushed;
				this.Focus();
			}
			else state = States.Normal;
			this.Invalidate(bounds);
			base.OnMouseDown(e);
		}

		protected override void OnMouseUp(System.Windows.Forms.MouseEventArgs e)
		{
			if((e.Button & MouseButtons.Left) == MouseButtons.Left)
				state = States.Normal;
			this.Invalidate(bounds);
			base.OnMouseUp(e);
		}

		protected override void OnEnter(System.EventArgs e)
		{
			this.Invalidate(bounds);
			base.OnEnter(e);
		}

		protected override void OnLeave(System.EventArgs e)
		{
			this.Invalidate(bounds);
			base.OnLeave(e);
		}

		protected override void OnClick(System.EventArgs e)
		{
			if(state == States.Pushed)
			{
				state = States.Normal;
				this.Invalidate(bounds);
			}
			if(this.dialogResult != DialogResult.None)
			{
				Form form = (Form)this.FindForm();
				form.DialogResult = this.DialogResult;
			}
			base.OnClick(e);
		}

		protected override void OnKeyDown(KeyEventArgs ke)
		{
			if(ke.KeyData == Keys.Enter || ke.KeyData == Keys.Space)
				this.PerformClick();
			base.OnKeyDown(ke);
		}

		protected override void OnKeyUp(KeyEventArgs ke)
		{
			base.OnKeyUp(ke);
		}

		protected override bool ProcessMnemonic(char charCode)
		{
			if(Control.IsMnemonic(charCode, base.Text))
			{
				this.PerformClick();
				return true;
			}
			return base.ProcessMnemonic(charCode);
		}

		protected override void OnSizeChanged(System.EventArgs e)
		{
			bounds = new Rectangle(0, 0, this.Width, this.Height);
			OnParentChanged(e);
			base.OnSizeChanged(e);
		}

		protected override void OnParentChanged(EventArgs e)
		{
			if(Parent == null) return;
			GetPoints();
			CreateRegion();
			base.OnParentChanged(e);
		}

		protected override void OnTextChanged(EventArgs e)
		{
			if(sf != null) sf.Dispose();
			sf = new StringFormat();
			sf.HotkeyPrefix = System.Drawing.Text.HotkeyPrefix.Show;
			GetPoints();
			base.OnTextChanged(e);
		}

		protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
		{
			if(image != null)
			{
				if(this.Enabled) e.Graphics.DrawImage(image, iPoint);
				else ControlPaint.DrawImageDisabled(e.Graphics, image, iPoint.X, iPoint.Y, this.BackColor);
			}
			e.Graphics.DrawString(Text, Font, textBrush, tPoint, sf);
		}

		#endregion

		#region Private Methods

		private void GetPoints()
		{
			int X = this.Width, Y = this.Height;

			if(Image != null)
			{
				if(Text.Length == 0) iPoint = new Point((X - Image.Width) / 2, (Y - Image.Height) / 2);
				else iPoint = new Point(BT.LeftMargin, (Y - Image.Height) / 2);

				tPoint = new Point(BT.LeftMargin + Image.Width + BT.TextMargin, (Y - this.Font.Height) / 2);
			}
			else
			{
				Size size = TextUtil.GetTextSize(this.CreateGraphics(), Text.Replace("&", ""), Font, new Size(X, Y));
				tPoint = new Point((X - size.Width - 2) / 2, (Y - this.Font.Height) / 2);
			}
		}

		#endregion

		#region Virtual methods

		protected virtual void CreateRegion()
		{
		}

		protected virtual void CreatePensBrushes()
		{
			if(textBrush != null) textBrush.Dispose();
			textBrush = new SolidBrush(textColor);
		}

		protected virtual void DisposePensBrushes()
		{
			if(textBrush != null) textBrush.Dispose();
		}

		#endregion

		#region Implementation of IButtonControl

		public void PerformClick()
		{
			if(base.CanSelect)
			{
				OnClick(EventArgs.Empty);
			}
		}

		public void NotifyDefault(bool value)
		{
			this.isDefault = value;
			this.Invalidate();
		}

		public System.Windows.Forms.DialogResult DialogResult
		{
			get { return this.dialogResult; }
			set { this.dialogResult = value; }
		}

		#endregion
	}
}