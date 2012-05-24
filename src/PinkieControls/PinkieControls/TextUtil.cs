using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace PinkieControls
{
	public class TextUtil
	{
		public static Size GetTextSize(Graphics graphics, string text, Font font, Size size)
		{
			if(text.Length == 0) return Size.Empty;

			StringFormat format = new StringFormat();
			format.FormatFlags = StringFormatFlags.FitBlackBox; //MeasureTrailingSpaces;

			RectangleF layoutRect = new System.Drawing.RectangleF(0, 0, size.Width, size.Height);
			CharacterRange[] chRange = {new CharacterRange(0, text.Length)};
			Region[] regs = new Region[1];

			format.SetMeasurableCharacterRanges(chRange);

			regs = graphics.MeasureCharacterRanges(text, font, layoutRect, format);
			Rectangle rect = Rectangle.Round(regs[0].GetBounds(graphics));

			return new Size(rect.Width, rect.Height);
		}

		private TextUtil()
		{
		}
	}
}