using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;


namespace PinkieControls
{
	public class ColorManager
	{
		#region Brushes (enable state)

		public static LinearGradientBrush Brush00(Rectangle rect)
		{
			return new LinearGradientBrush(rect
			                               , Color.FromArgb(64, 171, 168, 137), Color.FromArgb(92, 255, 255, 255), 85.0f);
		}

		public static LinearGradientBrush Brush01(ButtonXP.Schemes scheme, Rectangle rect)
		{
			switch(scheme)
			{
				case ButtonXP.Schemes.OliveGreen:
					return new LinearGradientBrush(rect
					                               , Color.FromArgb(255, 255, 246), Color.FromArgb(246, 243, 224), 90.0f);
				default:
					return new LinearGradientBrush(rect
					                               , Color.FromArgb(255, 255, 255), Color.FromArgb(240, 240, 234), 90.0f);
			}
		}

		public static LinearGradientBrush Brush02(ButtonXP.Schemes scheme, Rectangle rect)
		{
			switch(scheme)
			{
				case ButtonXP.Schemes.OliveGreen:
					return new LinearGradientBrush(rect
					                               , Color.FromArgb(177, 203, 128), Color.FromArgb(144, 193, 84), 90.0f);
				default:
					return new LinearGradientBrush(rect
					                               , Color.FromArgb(186, 211, 245), Color.FromArgb(137, 173, 228), 90.0f);
			}
		}

		public static LinearGradientBrush Brush03(ButtonXP.Schemes scheme, Rectangle rect)
		{
			switch(scheme)
			{
				case ButtonXP.Schemes.OliveGreen:
					return new LinearGradientBrush(rect
					                               , Color.FromArgb(237, 190, 150), Color.FromArgb(227, 145, 79), 90.0f);
				default:
					return new LinearGradientBrush(rect
					                               , Color.FromArgb(253, 216, 137), Color.FromArgb(248, 178, 48), 90.0f);
			}
		}

		public static SolidBrush Brush04(ButtonXP.Schemes scheme)
		{
			switch(scheme)
			{
				case ButtonXP.Schemes.Silver:
					return new SolidBrush(Color.FromArgb(92, 85, 125, 162));
				case ButtonXP.Schemes.OliveGreen:
					return new SolidBrush(Color.FromArgb(92, 109, 138, 77));
				default:
					return new SolidBrush(Color.FromArgb(92, 85, 125, 162));
			}
		}

		public static LinearGradientBrush Brush05(ButtonXP.Schemes scheme, Rectangle rect)
		{
			switch(scheme)
			{
				case ButtonXP.Schemes.OliveGreen:
					return new LinearGradientBrush(rect
					                               , Color.FromArgb(238, 230, 210), Color.FromArgb(236, 228, 206), 90.0f);
				default:
					return new LinearGradientBrush(rect
					                               , Color.FromArgb(229, 228, 221), Color.FromArgb(226, 226, 218), 90.0f);
			}
		}

		public static SolidBrush Brush06()
		{
			return new SolidBrush(Color.FromArgb(255, 255, 255));
		}

		public static LinearGradientBrush Brush07(Rectangle rect)
		{
			LinearGradientBrush brush = new LinearGradientBrush(rect
			                                                    , Color.FromArgb(253, 253, 253), Color.FromArgb(201, 200, 220), 90.0f);

			float[] relativeIntensities = {0.0f, 0.008f, 1.0f};
			float[] relativePositions = {0.0f, 0.32f, 1.0f};

			Blend blend = new Blend();
			blend.Factors = relativeIntensities;
			blend.Positions = relativePositions;
			brush.Blend = blend;
			return brush;
		}

		public static SolidBrush Brush08()
		{
			return new SolidBrush(Color.FromArgb(198, 197, 215));
		}

		public static LinearGradientBrush Brush09(Rectangle rect)
		{
			LinearGradientBrush brush = new LinearGradientBrush(rect
			                                                    , Color.FromArgb(172, 171, 191), Color.FromArgb(248, 252, 253), 90.0f);
			float[] relativeIntensities = {0.0f, 0.992f, 1.0f};
			float[] relativePositions = {0.0f, 0.68f, 1.0f};

			Blend blend = new Blend();
			blend.Factors = relativeIntensities;
			blend.Positions = relativePositions;
			brush.Blend = blend;
			return brush;
		}

		#endregion

		#region Brushes (disable state)

		public static SolidBrush _Brush01(ButtonXP.Schemes scheme)
		{
			switch(scheme)
			{
				case ButtonXP.Schemes.OliveGreen:
					return new SolidBrush(Color.FromArgb(64, 202, 196, 184));
				case ButtonXP.Schemes.Silver:
					return new SolidBrush(Color.FromArgb(64, 196, 195, 191));
				default:
					return new SolidBrush(Color.FromArgb(64, 201, 199, 186));
			}
		}

		public static SolidBrush _Brush02(ButtonXP.Schemes scheme)
		{
			switch(scheme)
			{
				case ButtonXP.Schemes.OliveGreen:
					return new SolidBrush(Color.FromArgb(246, 242, 233));
				case ButtonXP.Schemes.Silver:
					return new SolidBrush(Color.FromArgb(241, 241, 237));
				default:
					return new SolidBrush(Color.FromArgb(245, 244, 234));
			}
		}

		#endregion

		#region Pens (enable state)

		public static Pen Pen01(ButtonXP.Schemes scheme)
		{
			switch(scheme)
			{
				case ButtonXP.Schemes.OliveGreen:
					return new Pen(Color.FromArgb(243, 238, 219));
				default:
					return new Pen(Color.FromArgb(236, 235, 230));
			}
		}

		public static Pen Pen02(ButtonXP.Schemes scheme)
		{
			switch(scheme)
			{
				case ButtonXP.Schemes.OliveGreen:
					return new Pen(Color.FromArgb(236, 225, 201));
				default:
					return new Pen(Color.FromArgb(226, 223, 214));
			}
		}

		public static Pen Pen03(ButtonXP.Schemes scheme)
		{
			switch(scheme)
			{
				case ButtonXP.Schemes.OliveGreen:
					return new Pen(Color.FromArgb(227, 209, 184));
				default:
					return new Pen(Color.FromArgb(214, 208, 197));
			}
		}

		public static Pen Pen04(ButtonXP.Schemes scheme, Rectangle rect)
		{
			LinearGradientBrush _brush;
			Pen pen;

			switch(scheme)
			{
				case ButtonXP.Schemes.OliveGreen:
					_brush = new LinearGradientBrush(rect
					                                 , Color.FromArgb(251, 247, 232), Color.FromArgb(64, 216, 181, 144), 90.0f);

					pen = new Pen(_brush);
					_brush.Dispose();
					return pen;

				default:
					_brush = new LinearGradientBrush(rect
					                                 , Color.FromArgb(245, 244, 242), Color.FromArgb(64, 186, 174, 160), 90.0f);

					pen = new Pen(_brush);
					_brush.Dispose();
					return pen;
			}
		}

		public static Pen Pen05(ButtonXP.Schemes scheme, Rectangle rect)
		{
			LinearGradientBrush _brush;
			Pen pen;

			switch(scheme)
			{
				case ButtonXP.Schemes.OliveGreen:
					_brush = new LinearGradientBrush(rect
					                                 , Color.FromArgb(246, 241, 224), Color.FromArgb(64, 194, 156, 120), 90.0f);

					pen = new Pen(_brush);
					_brush.Dispose();
					return pen;

				default:
					_brush = new LinearGradientBrush(rect
					                                 , Color.FromArgb(240, 238, 234), Color.FromArgb(64, 175, 168, 142), 90.0f);

					pen = new Pen(_brush);
					_brush.Dispose();
					return pen;
			}
		}

		public static Pen Pen06(ButtonXP.Schemes scheme)
		{
			switch(scheme)
			{
				case ButtonXP.Schemes.OliveGreen:
					return new Pen(Color.FromArgb(194, 209, 143));
				default:
					return new Pen(Color.FromArgb(206, 231, 255));
			}
		}

		public static Pen Pen07(ButtonXP.Schemes scheme)
		{
			switch(scheme)
			{
				case ButtonXP.Schemes.OliveGreen:
					return new Pen(Color.FromArgb(177, 203, 128));
				default:
					return new Pen(Color.FromArgb(188, 212, 246));
			}
		}

		public static Pen Pen08(ButtonXP.Schemes scheme)
		{
			switch(scheme)
			{
				case ButtonXP.Schemes.OliveGreen:
					return new Pen(Color.FromArgb(144, 193, 84));
				default:
					return new Pen(Color.FromArgb(137, 173, 228));
			}
		}

		public static Pen Pen09(ButtonXP.Schemes scheme)
		{
			switch(scheme)
			{
				case ButtonXP.Schemes.OliveGreen:
					return new Pen(Color.FromArgb(168, 167, 102));
				default:
					return new Pen(Color.FromArgb(105, 130, 238));
			}
		}

		public static Pen Pen10(ButtonXP.Schemes scheme)
		{
			switch(scheme)
			{
				case ButtonXP.Schemes.OliveGreen:
					return new Pen(Color.FromArgb(252, 197, 149));
				default:
					return new Pen(Color.FromArgb(255, 240, 207));
			}
		}

		public static Pen Pen11(ButtonXP.Schemes scheme)
		{
			switch(scheme)
			{
				case ButtonXP.Schemes.OliveGreen:
					return new Pen(Color.FromArgb(237, 190, 150));
				default:
					return new Pen(Color.FromArgb(253, 216, 137));
			}
		}

		public static Pen Pen12(ButtonXP.Schemes scheme)
		{
			switch(scheme)
			{
				case ButtonXP.Schemes.OliveGreen:
					return new Pen(Color.FromArgb(227, 145, 79));
				default:
					return new Pen(Color.FromArgb(248, 178, 48));
			}
		}

		public static Pen Pen13(ButtonXP.Schemes scheme)
		{
			switch(scheme)
			{
				case ButtonXP.Schemes.OliveGreen:
					return new Pen(Color.FromArgb(207, 114, 37));
				default:
					return new Pen(Color.FromArgb(229, 151, 0));
			}
		}

		public static Pen Pen14(ButtonXP.Schemes scheme)
		{
			switch(scheme)
			{
				case ButtonXP.Schemes.OliveGreen:
					return new Pen(Color.FromArgb(55, 98, 6));
				default:
					return new Pen(Color.FromArgb(0, 60, 116));
			}
		}

		public static Pen Pen15(ButtonXP.Schemes scheme)
		{
			switch(scheme)
			{
				case ButtonXP.Schemes.OliveGreen:
					return new Pen(Color.FromArgb(109, 138, 77));
				default:
					return new Pen(Color.FromArgb(85, 125, 162));
			}
		}

		public static Pen Pen16(ButtonXP.Schemes scheme)
		{
			switch(scheme)
			{
				case ButtonXP.Schemes.OliveGreen:
					return new Pen(Color.FromArgb(192, 109, 138, 77));
				default:
					return new Pen(Color.FromArgb(192, 85, 125, 162));
			}
		}

		public static Pen Pen17(ButtonXP.Schemes scheme, Rectangle rect)
		{
			LinearGradientBrush _brush;
			Pen pen;

			switch(scheme)
			{
				case ButtonXP.Schemes.OliveGreen:
					_brush = new LinearGradientBrush(rect
					                                 , Color.FromArgb(228, 212, 191), Color.FromArgb(229, 217, 195), 90.0f);
					pen = new Pen(_brush);
					_brush.Dispose();
					return pen;

				default:
					_brush = new LinearGradientBrush(rect
					                                 , Color.FromArgb(216, 212, 203), Color.FromArgb(218, 216, 207), 90.0f);
					pen = new Pen(_brush);
					_brush.Dispose();
					return pen;
			}
		}

		public static Pen Pen18(ButtonXP.Schemes scheme, Rectangle rect)
		{
			LinearGradientBrush _brush;
			Pen pen;

			switch(scheme)
			{
				case ButtonXP.Schemes.OliveGreen:
					_brush = new LinearGradientBrush(rect
					                                 , Color.FromArgb(232, 219, 197), Color.FromArgb(234, 224, 201), 90.0f);
					pen = new Pen(_brush);
					_brush.Dispose();
					return pen;

				default:
					_brush = new LinearGradientBrush(rect
					                                 , Color.FromArgb(221, 218, 209), Color.FromArgb(223, 222, 214), 90.0f);
					pen = new Pen(_brush);
					_brush.Dispose();
					return pen;
			}
		}

		public static Pen Pen19(ButtonXP.Schemes scheme)
		{
			switch(scheme)
			{
				case ButtonXP.Schemes.OliveGreen:
					return new Pen(Color.FromArgb(223, 205, 180));
				default:
					return new Pen(Color.FromArgb(209, 204, 192));
			}
		}

		public static Pen Pen20(ButtonXP.Schemes scheme)
		{
			switch(scheme)
			{
				case ButtonXP.Schemes.OliveGreen:
					return new Pen(Color.FromArgb(231, 217, 195));
				default:
					return new Pen(Color.FromArgb(220, 216, 207));
			}
		}

		public static Pen Pen21(ButtonXP.Schemes scheme)
		{
			switch(scheme)
			{
				case ButtonXP.Schemes.OliveGreen:
					return new Pen(Color.FromArgb(242, 236, 216));
				default:
					return new Pen(Color.FromArgb(234, 233, 227));
			}
		}

		public static Pen Pen22(ButtonXP.Schemes scheme)
		{
			switch(scheme)
			{
				case ButtonXP.Schemes.OliveGreen:
					return new Pen(Color.FromArgb(248, 244, 228));
				default:
					return new Pen(Color.FromArgb(242, 241, 238));
			}
		}

		public static Pen Pen23()
		{
			return new Pen(Color.FromArgb(255, 255, 255));
		}

		public static Pen Pen24()
		{
			return new Pen(Color.FromArgb(172, 171, 189));
		}

		#endregion

		#region Pens (disable state)

		public static Pen _Pen01(ButtonXP.Schemes scheme)
		{
			switch(scheme)
			{
				case ButtonXP.Schemes.OliveGreen:
					return new Pen(Color.FromArgb(202, 196, 184));
				case ButtonXP.Schemes.Silver:
					return new Pen(Color.FromArgb(196, 195, 191));
				default:
					return new Pen(Color.FromArgb(201, 199, 186));
			}
		}

		public static Pen _Pen02(ButtonXP.Schemes scheme)
		{
			switch(scheme)
			{
				case ButtonXP.Schemes.OliveGreen:
					return new Pen(Color.FromArgb(170, 202, 196, 184));
				case ButtonXP.Schemes.Silver:
					return new Pen(Color.FromArgb(170, 196, 195, 191));
				default:
					return new Pen(Color.FromArgb(170, 201, 199, 186));
			}
		}

		#endregion

		#region Constructor

		public ColorManager()
		{
		}

		#endregion
	}
}