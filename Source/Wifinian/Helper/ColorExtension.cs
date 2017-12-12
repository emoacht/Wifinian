using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using static System.Math;

namespace Wifinian.Helper
{
	public static class ColorExtension
	{
		public static HsbColor ToAhsb(this Color source) =>
			HsbColor.FromArgb(source);

		public static Color ToBrightened(this Color source, float factor)
		{
			if (factor <= 0F)
				throw new ArgumentOutOfRangeException(nameof(factor));

			var bridgeColor = HsbColor.FromArgb(source);
			bridgeColor.B = Min(1F, bridgeColor.B * factor);

			return bridgeColor.ToArgb();
		}
	}
}