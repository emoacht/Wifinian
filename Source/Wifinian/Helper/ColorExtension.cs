using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using static System.Math;

namespace Wifinian.Helper
{
	/// <summary>
	/// Extension methods for <see cref="System.Windows.Media.Color"/>
	/// </summary>
	public static class ColorExtension
	{
		public static HsbColor ToAhsb(this Color source) =>
			HsbColor.FromArgb(source);

		/// <summary>
		/// Gets Color brightened by a specified factor.
		/// </summary>
		/// <param name="source">Source Color</param>
		/// <param name="factor">Factor</param>
		/// <returns>Brightened Color</returns>
		public static Color ToBrightened(this Color source, float factor)
		{
			if (factor <= 0F)
				throw new ArgumentOutOfRangeException(nameof(factor), factor, "The factor must be positive.");

			var bridgeColor = HsbColor.FromArgb(source);
			bridgeColor.B = Min(1F, bridgeColor.B * factor);

			return bridgeColor.ToArgb();
		}

		/// <summary>
		/// Gets Color equivalent to translucent Color with white background.
		/// </summary>
		/// <param name="source">Source Color</param>
		/// <returns>Opaque Color</returns>
		public static Color ToOpaque(this Color source)
		{
			var alpha = source.A;
			byte Compute(byte value) => (byte)(byte.MaxValue - (byte.MaxValue - value) * alpha / (float)byte.MaxValue);

			return Color.FromRgb(Compute(source.R), Compute(source.G), Compute(source.B));
		}
	}
}