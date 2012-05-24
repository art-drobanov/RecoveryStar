// Created by WiB May 2003
// shy_dream@yahoo.com
// You may include the source code, modified source code, assembly
// within your own projects for either personal or commercial use 
// with the only one restriction:
// don't change the name library "PinkieControls.dll".


using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Text;


namespace PinkieControls
{
	public class ButtonXP : ButtonBase
	{
		#region Pens & Brushes

		private Rectangle[] rects0;
		private Rectangle[] rects1;

		private LinearGradientBrush
			brush00, brush01,
			bluesilverBrush02, oliveBrush02,
			bluesilverBrush03, oliveBrush03, brush05, silverBrush07, brush09;

		private SolidBrush
			blueBrush04, oliveBrush04, silverBrush04, silverBrush06, silverBrush08, _brush01, _brush02;

		private Pen
			bluesilverPen01, olivePen01, bluesilverPen02, olivePen02,
			bluesilverPen03, olivePen03, bluesilverPen04, olivePen04,
			bluesilverPen05, olivePen05, bluesilverPen06, olivePen06,
			bluesilverPen07, olivePen07, bluesilverPen08, olivePen08,
			bluesilverPen09, olivePen09, bluesilverPen10, olivePen10,
			bluesilverPen11, olivePen11, bluesilverPen12, olivePen12,
			bluesilverPen13, olivePen13, bluesilverPen14, olivePen14,
			bluesilverPen15, olivePen15, bluesilverPen16, olivePen16,
			pen17, pen18, pen19, pen20, pen21, pen22, pen23, pen24, _pen01, _pen02;

		#endregion

		#region Fields

		public enum Schemes
		{
			Blue,
			OliveGreen,
			Silver
		}

		private Schemes scheme = Schemes.Blue;

		public bool DefaultScheme
		{
			get { return defaultScheme; }
			set
			{
				defaultScheme = value;
				if(defaultScheme)
				{
					try
					{
						StringBuilder sb1 = new StringBuilder(256);
						StringBuilder sb2 = new StringBuilder(256);
						int i = WinAPI.GetCurrentThemeName(sb1, sb1.Capacity, sb2, sb2.Capacity, null, 0);
						string str = sb2.ToString();

						switch(str)
						{
							case @"HomeStead":
								scheme = Schemes.OliveGreen;
								break;
							case @"Metallic":
								scheme = Schemes.Silver;
								break;
							default:
								scheme = Schemes.Blue;
								break;
						}
					}
					catch(Exception)
					{
						return;
					}
					this.Invalidate();
				}
			}
		}

		private bool defaultScheme = true;

		#endregion

		#region Public Properties

		[DefaultValue("Blue"),
		 System.ComponentModel.RefreshProperties(RefreshProperties.Repaint)]
		public Schemes Scheme
		{
			get { return scheme; }
			set
			{
				scheme = value;
				this.Invalidate();
			}
		}

		#endregion

		#region Constructor

		public ButtonXP()
		{
		}

		#endregion

		#region Protected Methods

		protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
		{
			int X = this.Width;
			int Y = this.Height;

			CreatePensBrushes();

			e.Graphics.CompositingQuality = CompositingQuality.GammaCorrected;
			if(!this.Enabled)
			{
				e.Graphics.FillRectangle(_brush02, 2, 2, X - 4, Y - 4);
				e.Graphics.DrawLine(_pen01, 3, 1, X - 4, 1);
				e.Graphics.DrawLine(_pen01, 3, Y - 2, X - 4, Y - 2);
				e.Graphics.DrawLine(_pen01, 1, 3, 1, Y - 4);
				e.Graphics.DrawLine(_pen01, X - 2, 3, X - 2, Y - 4);

				e.Graphics.DrawLine(_pen02, 1, 2, 2, 1);
				e.Graphics.DrawLine(_pen02, 1, Y - 3, 2, Y - 2);
				e.Graphics.DrawLine(_pen02, X - 2, 2, X - 3, 1);
				e.Graphics.DrawLine(_pen02, X - 2, Y - 3, X - 3, Y - 2);
				e.Graphics.FillRectangles(_brush01, rects1);
			}
			else
			{
				e.Graphics.FillRectangle(brush00, new Rectangle(0, 0, X, Y));
				switch(state)
				{
					case States.Normal:
						switch(scheme)
						{
							case Schemes.Silver:
								e.Graphics.FillRectangle(silverBrush06, 2, 2, X - 4, Y - 4);
								e.Graphics.FillRectangle(silverBrush07, 3, 4, X - 6, Y - 8);
								e.Graphics.FillRectangle(silverBrush08, 2, Y - 4, X - 4, 2);

								if(isDefault)
								{
									e.Graphics.FillRectangles(bluesilverBrush02, rects0);
									e.Graphics.DrawLine(pen23, 3, 4, 3, Y - 4);
									e.Graphics.DrawLine(pen23, X - 4, 4, X - 4, Y - 4);

									e.Graphics.DrawLine(bluesilverPen06, 2, 2, X - 3, 2);
									e.Graphics.DrawLine(bluesilverPen07, 2, 3, X - 3, 3);
									e.Graphics.DrawLine(bluesilverPen08, 2, Y - 4, X - 3, Y - 4);
									e.Graphics.DrawLine(bluesilverPen09, 2, Y - 3, X - 3, Y - 3);
								}
								break;

							case Schemes.OliveGreen:
								e.Graphics.FillRectangle(brush01, 2, 2, X - 4, Y - 7);
								e.Graphics.DrawLine(olivePen01, 2, Y - 5, X - 2, Y - 5);
								e.Graphics.DrawLine(olivePen02, 2, Y - 4, X - 2, Y - 4);
								e.Graphics.DrawLine(olivePen03, 2, Y - 3, X - 2, Y - 3);
								e.Graphics.DrawLine(olivePen04, X - 4, 4, X - 4, Y - 5);
								e.Graphics.DrawLine(olivePen05, X - 3, 4, X - 3, Y - 5);

								if(isDefault)
								{
									e.Graphics.FillRectangles(oliveBrush02, rects0);
									e.Graphics.DrawLine(olivePen06, 2, 2, X - 3, 2);
									e.Graphics.DrawLine(olivePen07, 2, 3, X - 3, 3);
									e.Graphics.DrawLine(olivePen08, 2, Y - 4, X - 3, Y - 4);
									e.Graphics.DrawLine(olivePen09, 2, Y - 3, X - 3, Y - 3);
								}
								break;

							default:
								e.Graphics.FillRectangle(brush01, 2, 2, X - 4, Y - 7);
								e.Graphics.DrawLine(bluesilverPen01, 2, Y - 5, X - 2, Y - 5);
								e.Graphics.DrawLine(bluesilverPen02, 2, Y - 4, X - 2, Y - 4);
								e.Graphics.DrawLine(bluesilverPen03, 2, Y - 3, X - 2, Y - 3);
								e.Graphics.DrawLine(bluesilverPen04, X - 4, 4, X - 4, Y - 5);
								e.Graphics.DrawLine(bluesilverPen05, X - 3, 4, X - 3, Y - 5);

								if(isDefault)
								{
									e.Graphics.FillRectangles(bluesilverBrush02, rects0);
									e.Graphics.DrawLine(bluesilverPen06, 2, 2, X - 3, 2);
									e.Graphics.DrawLine(bluesilverPen07, 2, 3, X - 3, 3);
									e.Graphics.DrawLine(bluesilverPen08, 2, Y - 4, X - 3, Y - 4);
									e.Graphics.DrawLine(bluesilverPen09, 2, Y - 3, X - 3, Y - 3);
								}
								break;
						}
						break;

					case States.MouseOver:
						switch(scheme)
						{
							case Schemes.Silver:
								e.Graphics.FillRectangle(silverBrush06, 2, 2, X - 4, Y - 4);
								e.Graphics.FillRectangle(silverBrush07, 3, 4, X - 6, Y - 8);
								e.Graphics.FillRectangle(silverBrush08, 2, Y - 4, X - 4, 2);

								e.Graphics.FillRectangles(bluesilverBrush03, rects0);
								e.Graphics.DrawLine(bluesilverPen10, 2, 2, X - 3, 2);
								e.Graphics.DrawLine(bluesilverPen11, 2, 3, X - 3, 3);
								e.Graphics.DrawLine(bluesilverPen12, 2, Y - 4, X - 3, Y - 4);
								e.Graphics.DrawLine(bluesilverPen13, 2, Y - 3, X - 3, Y - 3);
								break;

							case Schemes.OliveGreen:
								e.Graphics.FillRectangle(brush01, 2, 2, X - 4, Y - 7);
								e.Graphics.DrawLine(olivePen01, 2, Y - 5, X - 4, Y - 5);
								e.Graphics.DrawLine(olivePen02, 2, Y - 4, X - 4, Y - 4);
								e.Graphics.DrawLine(olivePen03, 2, Y - 3, X - 4, Y - 3);
								e.Graphics.DrawLine(olivePen04, X - 4, 4, X - 4, Y - 5);
								e.Graphics.DrawLine(olivePen05, X - 3, 4, X - 3, Y - 5);

								e.Graphics.FillRectangles(oliveBrush03, rects0);
								e.Graphics.DrawLine(olivePen10, 2, 2, X - 3, 2);
								e.Graphics.DrawLine(olivePen11, 2, 3, X - 3, 3);
								e.Graphics.DrawLine(olivePen12, 2, Y - 4, X - 3, Y - 4);
								e.Graphics.DrawLine(olivePen13, 2, Y - 3, X - 3, Y - 3);
								break;

							default:
								e.Graphics.FillRectangle(brush01, 2, 2, X - 4, Y - 7);
								e.Graphics.DrawLine(bluesilverPen01, 2, Y - 5, X - 4, Y - 5);
								e.Graphics.DrawLine(bluesilverPen02, 2, Y - 4, X - 4, Y - 4);
								e.Graphics.DrawLine(bluesilverPen03, 2, Y - 3, X - 4, Y - 3);
								e.Graphics.DrawLine(bluesilverPen04, X - 4, 4, X - 4, Y - 5);
								e.Graphics.DrawLine(bluesilverPen05, X - 3, 4, X - 3, Y - 5);

								e.Graphics.FillRectangles(bluesilverBrush03, rects0);
								e.Graphics.DrawLine(bluesilverPen10, 2, 2, X - 3, 2);
								e.Graphics.DrawLine(bluesilverPen11, 2, 3, X - 3, 3);
								e.Graphics.DrawLine(bluesilverPen12, 2, Y - 4, X - 3, Y - 4);
								e.Graphics.DrawLine(bluesilverPen13, 2, Y - 3, X - 3, Y - 3);
								break;
						}
						break;

					case States.Pushed:
						switch(scheme)
						{
							case Schemes.Silver:
								e.Graphics.FillRectangle(silverBrush06, 2, 2, X - 4, Y - 4);
								e.Graphics.FillRectangle(brush09, 3, 4, X - 6, Y - 9);
								e.Graphics.DrawLine(pen24, 4, 3, X - 4, 3);
								break;

							case Schemes.OliveGreen:
							default:
								e.Graphics.FillRectangle(brush05, 2, 4, X - 4, Y - 8);
								e.Graphics.DrawLine(pen17, 2, 3, 2, Y - 4);
								e.Graphics.DrawLine(pen18, 3, 3, 3, Y - 4);
								e.Graphics.DrawLine(pen19, 2, 2, X - 3, 2);
								e.Graphics.DrawLine(pen20, 2, 3, X - 3, 3);
								e.Graphics.DrawLine(pen21, 2, Y - 4, X - 3, Y - 4);
								e.Graphics.DrawLine(pen22, 2, Y - 3, X - 3, Y - 3);
								break;
						}
						break;
				}

				switch(scheme)
				{
					case Schemes.Silver:
						e.Graphics.DrawLine(bluesilverPen15, 1, 3, 3, 1);
						e.Graphics.DrawLine(bluesilverPen15, X - 2, 3, X - 4, 1);
						e.Graphics.DrawLine(bluesilverPen15, 1, Y - 4, 3, Y - 2);
						e.Graphics.DrawLine(bluesilverPen15, X - 2, Y - 4, X - 4, Y - 2);

						e.Graphics.DrawLine(bluesilverPen16, 1, 2, 2, 1);
						e.Graphics.DrawLine(bluesilverPen16, 1, Y - 3, 2, Y - 2);
						e.Graphics.DrawLine(bluesilverPen16, X - 2, 2, X - 3, 1);
						e.Graphics.DrawLine(bluesilverPen16, X - 2, Y - 3, X - 3, Y - 2);

						e.Graphics.DrawLine(bluesilverPen14, 3, 1, X - 4, 1);
						e.Graphics.DrawLine(bluesilverPen14, 3, Y - 2, X - 4, Y - 2);
						e.Graphics.DrawLine(bluesilverPen14, 1, 3, 1, Y - 4);
						e.Graphics.DrawLine(bluesilverPen14, X - 2, 3, X - 2, Y - 4);

						e.Graphics.FillRectangles(silverBrush04, rects1);
						break;

					case Schemes.OliveGreen:
						e.Graphics.DrawLine(olivePen15, 1, 3, 3, 1);
						e.Graphics.DrawLine(olivePen15, X - 2, 3, X - 4, 1);
						e.Graphics.DrawLine(olivePen15, 1, Y - 4, 3, Y - 2);
						e.Graphics.DrawLine(olivePen15, X - 2, Y - 4, X - 4, Y - 2);

						e.Graphics.DrawLine(olivePen16, 1, 2, 2, 1);
						e.Graphics.DrawLine(olivePen16, 1, Y - 3, 2, Y - 2);
						e.Graphics.DrawLine(olivePen16, X - 2, 2, X - 3, 1);
						e.Graphics.DrawLine(olivePen16, X - 2, Y - 3, X - 3, Y - 2);

						e.Graphics.DrawLine(olivePen14, 3, 1, X - 4, 1);
						e.Graphics.DrawLine(olivePen14, 3, Y - 2, X - 4, Y - 2);
						e.Graphics.DrawLine(olivePen14, 1, 3, 1, Y - 4);
						e.Graphics.DrawLine(olivePen14, X - 2, 3, X - 2, Y - 4);

						e.Graphics.FillRectangles(oliveBrush04, rects1);
						break;

					default:
						e.Graphics.DrawLine(bluesilverPen15, 1, 3, 3, 1);
						e.Graphics.DrawLine(bluesilverPen15, X - 2, 3, X - 4, 1);
						e.Graphics.DrawLine(bluesilverPen15, 1, Y - 4, 3, Y - 2);
						e.Graphics.DrawLine(bluesilverPen15, X - 2, Y - 4, X - 4, Y - 2);

						e.Graphics.DrawLine(bluesilverPen16, 1, 2, 2, 1);
						e.Graphics.DrawLine(bluesilverPen16, 1, Y - 3, 2, Y - 2);
						e.Graphics.DrawLine(bluesilverPen16, X - 2, 2, X - 3, 1);
						e.Graphics.DrawLine(bluesilverPen16, X - 2, Y - 3, X - 3, Y - 2);

						e.Graphics.DrawLine(bluesilverPen14, 3, 1, X - 4, 1);
						e.Graphics.DrawLine(bluesilverPen14, 3, Y - 2, X - 4, Y - 2);
						e.Graphics.DrawLine(bluesilverPen14, 1, 3, 1, Y - 4);
						e.Graphics.DrawLine(bluesilverPen14, X - 2, 3, X - 2, Y - 4);

						e.Graphics.FillRectangles(blueBrush04, rects1);
						break;
				}

				if(this.Focused)
					ControlPaint.DrawFocusRectangle(e.Graphics,
					                                new Rectangle(3, 3, X - 6, Y - 6), Color.Black, this.BackColor);
			}

			base.OnPaint(e);
			DisposePensBrushes();
		}

		protected override void OnParentChanged(System.EventArgs e)
		{
			if(Parent == null) return;
			this.BackColor = Color.FromArgb(0, this.Parent.BackColor);
			base.OnParentChanged(e);
		}

		protected override void CreatePensBrushes()
		{
			DisposePensBrushes();
			if(Region == null) return;

			int X = this.Width;
			int Y = this.Height;

			brush00 = ColorManager.Brush00(new Rectangle(0, 0, X, Y));
			bluesilverBrush02 = ColorManager.Brush02(Schemes.Blue, new Rectangle(2, 3, X - 4, Y - 7));
			oliveBrush02 = ColorManager.Brush02(Schemes.OliveGreen, new Rectangle(2, 3, X - 4, Y - 7));
			bluesilverBrush03 = ColorManager.Brush03(Schemes.Blue, new Rectangle(2, 3, X - 4, Y - 7));
			oliveBrush03 = ColorManager.Brush03(Schemes.OliveGreen, new Rectangle(2, 3, X - 4, Y - 7));

			blueBrush04 = ColorManager.Brush04(Schemes.Blue);
			oliveBrush04 = ColorManager.Brush04(Schemes.OliveGreen);
			silverBrush04 = ColorManager.Brush04(Schemes.Silver);

			bluesilverPen06 = ColorManager.Pen06(Schemes.Blue);
			olivePen06 = ColorManager.Pen06(Schemes.OliveGreen);
			bluesilverPen07 = ColorManager.Pen07(Schemes.Blue);
			olivePen07 = ColorManager.Pen07(Schemes.OliveGreen);
			bluesilverPen08 = ColorManager.Pen08(Schemes.Blue);
			olivePen08 = ColorManager.Pen08(Schemes.OliveGreen);

			bluesilverPen09 = ColorManager.Pen09(Schemes.Blue);
			olivePen09 = ColorManager.Pen09(Schemes.OliveGreen);
			bluesilverPen10 = ColorManager.Pen10(Schemes.Blue);
			olivePen10 = ColorManager.Pen10(Schemes.OliveGreen);
			bluesilverPen11 = ColorManager.Pen11(Schemes.Blue);
			olivePen11 = ColorManager.Pen11(Schemes.OliveGreen);
			bluesilverPen12 = ColorManager.Pen12(Schemes.Blue);
			olivePen12 = ColorManager.Pen12(Schemes.OliveGreen);
			bluesilverPen13 = ColorManager.Pen13(Schemes.Blue);
			olivePen13 = ColorManager.Pen13(Schemes.OliveGreen);
			bluesilverPen14 = ColorManager.Pen14(Schemes.Blue);
			olivePen14 = ColorManager.Pen14(Schemes.OliveGreen);
			bluesilverPen15 = ColorManager.Pen15(Schemes.Blue);
			olivePen15 = ColorManager.Pen15(Schemes.OliveGreen);
			bluesilverPen16 = ColorManager.Pen16(Schemes.Blue);
			olivePen16 = ColorManager.Pen16(Schemes.OliveGreen);

			_brush01 = ColorManager._Brush01(scheme);
			_brush02 = ColorManager._Brush02(scheme);
			_pen01 = ColorManager._Pen01(scheme);
			_pen02 = ColorManager._Pen02(scheme);

			silverBrush06 = ColorManager.Brush06();
			silverBrush07 = ColorManager.Brush07(new Rectangle(3, 3, X - 6, Y - 7));
			silverBrush08 = ColorManager.Brush08();
			brush09 = ColorManager.Brush09(new Rectangle(3, 3, X - 5, Y - 8));
			pen23 = ColorManager.Pen23();
			pen24 = ColorManager.Pen24();

			brush01 = ColorManager.Brush01(scheme, new Rectangle(2, 2, X - 5, Y - 7));
			brush05 = ColorManager.Brush05(scheme, new Rectangle(2, 2, X - 5, Y - 7));

			bluesilverPen01 = ColorManager.Pen01(Schemes.Blue);
			bluesilverPen02 = ColorManager.Pen02(Schemes.Blue);
			bluesilverPen03 = ColorManager.Pen03(Schemes.Blue);
			bluesilverPen04 = ColorManager.Pen04(Schemes.Blue, new Rectangle(X - 3, 4, 1, Y - 5));
			bluesilverPen05 = ColorManager.Pen05(Schemes.Blue, new Rectangle(X - 2, 4, 1, Y - 5));

			olivePen01 = ColorManager.Pen01(Schemes.OliveGreen);
			olivePen02 = ColorManager.Pen02(Schemes.OliveGreen);
			olivePen03 = ColorManager.Pen03(Schemes.OliveGreen);
			olivePen04 = ColorManager.Pen04(Schemes.OliveGreen, new Rectangle(X - 3, 4, 1, Y - 5));
			olivePen05 = ColorManager.Pen05(Schemes.OliveGreen, new Rectangle(X - 2, 4, 1, Y - 5));

			pen17 = ColorManager.Pen17(scheme, new Rectangle(2, 3, X - 4, Y - 7));
			pen18 = ColorManager.Pen18(scheme, new Rectangle(3, 3, X - 4, Y - 7));
			pen19 = ColorManager.Pen19(scheme);
			pen20 = ColorManager.Pen20(scheme);
			pen21 = ColorManager.Pen21(scheme);
			pen22 = ColorManager.Pen22(scheme);

			base.CreatePensBrushes();
		}

		protected override void DisposePensBrushes()
		{
			if(brush01 != null) brush01.Dispose();

			if(bluesilverBrush02 != null) bluesilverBrush02.Dispose();
			if(bluesilverBrush03 != null) bluesilverBrush03.Dispose();
			if(oliveBrush02 != null) oliveBrush02.Dispose();
			if(oliveBrush03 != null) oliveBrush03.Dispose();

			if(blueBrush04 != null) blueBrush04.Dispose();
			if(oliveBrush04 != null) oliveBrush04.Dispose();
			if(silverBrush04 != null) silverBrush04.Dispose();

			if(brush05 != null) brush05.Dispose();
			if(silverBrush06 != null) silverBrush06.Dispose();
			if(silverBrush07 != null) silverBrush07.Dispose();
			if(silverBrush08 != null) silverBrush08.Dispose();
			if(brush09 != null) brush09.Dispose();

			if(bluesilverPen01 != null) bluesilverPen01.Dispose();
			if(bluesilverPen02 != null) bluesilverPen02.Dispose();
			if(bluesilverPen03 != null) bluesilverPen03.Dispose();
			if(bluesilverPen04 != null) bluesilverPen04.Dispose();
			if(bluesilverPen05 != null) bluesilverPen05.Dispose();
			if(bluesilverPen06 != null) bluesilverPen06.Dispose();
			if(bluesilverPen07 != null) bluesilverPen07.Dispose();
			if(bluesilverPen08 != null) bluesilverPen08.Dispose();
			if(bluesilverPen09 != null) bluesilverPen09.Dispose();
			if(bluesilverPen10 != null) bluesilverPen10.Dispose();
			if(bluesilverPen11 != null) bluesilverPen11.Dispose();
			if(bluesilverPen12 != null) bluesilverPen12.Dispose();
			if(bluesilverPen13 != null) bluesilverPen13.Dispose();
			if(bluesilverPen14 != null) bluesilverPen14.Dispose();
			if(bluesilverPen15 != null) bluesilverPen15.Dispose();
			if(bluesilverPen16 != null) bluesilverPen16.Dispose();

			if(olivePen01 != null) olivePen01.Dispose();
			if(olivePen02 != null) olivePen02.Dispose();
			if(olivePen03 != null) olivePen03.Dispose();
			if(olivePen04 != null) olivePen04.Dispose();
			if(olivePen05 != null) olivePen05.Dispose();
			if(olivePen06 != null) olivePen06.Dispose();
			if(olivePen07 != null) olivePen07.Dispose();
			if(olivePen08 != null) olivePen08.Dispose();
			if(olivePen09 != null) olivePen09.Dispose();
			if(olivePen10 != null) olivePen10.Dispose();
			if(olivePen11 != null) olivePen11.Dispose();
			if(olivePen12 != null) olivePen12.Dispose();
			if(olivePen13 != null) olivePen13.Dispose();
			if(olivePen14 != null) olivePen14.Dispose();
			if(olivePen15 != null) olivePen15.Dispose();
			if(olivePen16 != null) olivePen16.Dispose();

			if(pen17 != null) pen17.Dispose();
			if(pen18 != null) pen18.Dispose();
			if(pen19 != null) pen19.Dispose();
			if(pen20 != null) pen20.Dispose();
			if(pen21 != null) pen21.Dispose();
			if(pen22 != null) pen22.Dispose();
			if(pen23 != null) pen23.Dispose();
			if(pen24 != null) pen24.Dispose();

			if(_brush01 != null) _brush01.Dispose();
			if(_brush02 != null) _brush02.Dispose();
			if(_pen01 != null) _pen01.Dispose();
			if(_pen02 != null) _pen02.Dispose();

			base.DisposePensBrushes();
		}

		protected override void CreateRegion()
		{
			int X = this.Width;
			int Y = this.Height;

			rects0 = new Rectangle[2];
			rects0[0] = new Rectangle(2, 4, 2, Y - 8);
			rects0[1] = new Rectangle(X - 4, 4, 2, Y - 8);

			rects1 = new Rectangle[8];
			rects1[0] = new Rectangle(2, 1, 2, 2);
			rects1[1] = new Rectangle(1, 2, 2, 2);
			rects1[2] = new Rectangle(X - 4, 1, 2, 2);
			rects1[3] = new Rectangle(X - 3, 2, 2, 2);
			rects1[4] = new Rectangle(2, Y - 3, 2, 2);
			rects1[5] = new Rectangle(1, Y - 4, 2, 2);
			rects1[6] = new Rectangle(X - 4, Y - 3, 2, 2);
			rects1[7] = new Rectangle(X - 3, Y - 4, 2, 2);

			Point[] points = {
			                 	new Point(1, 0),
			                 	new Point(X - 1, 0),
			                 	new Point(X - 1, 1),
			                 	new Point(X, 1),
			                 	new Point(X, Y - 1),
			                 	new Point(X - 1, Y - 1),
			                 	new Point(X - 1, Y),
			                 	new Point(1, Y),
			                 	new Point(1, Y - 1),
			                 	new Point(0, Y - 1),
			                 	new Point(0, 1),
			                 	new Point(1, 1)
			                 };

			GraphicsPath path = new GraphicsPath();
			path.AddLines(points);

			this.Region = new Region(path);
			base.CreateRegion();
		}

		#endregion
	}
}