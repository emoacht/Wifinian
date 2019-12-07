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
	/// HSB Color
	/// </summary>
	public struct HsbColor
	{
		#region Property

		/// <summary>
		/// Alpha
		/// </summary>
		public byte A { get; set; }

		/// <summary>
		/// Hue
		/// </summary>
		public float H
		{
			get => _h;
			set
			{
				if ((value < 0F) || (360F <= value))
					throw new ArgumentOutOfRangeException(nameof(value), value, "The range of hue: 0.0 <= value < 360.0");

				_h = value;
			}
		}
		private float _h;

		/// <summary>
		/// Saturation
		/// </summary>
		public float S
		{
			get => _s;
			set
			{
				if ((value < 0F) || (1F < value))
					throw new ArgumentOutOfRangeException(nameof(value), value, "The range of saturation: 0.0 <= value <= 1.0");

				_s = value;
			}
		}
		private float _s;

		/// <summary>
		/// Brightness
		/// </summary>
		public float B
		{
			get => _b;
			set
			{
				if ((value < 0F) || (1F < value))
					throw new ArgumentOutOfRangeException(nameof(value), value, "The range of brightness: 0.0 <= value <= 1.0");

				_b = value;
			}
		}
		private float _b;

		#endregion

		#region Convert

		public static HsbColor FromRgb(float hue, float saturation, float brightness) =>
			FromArgb(255, hue, saturation, brightness);

		public static HsbColor FromArgb(byte alpha, float hue, float saturation, float brightness) =>
			new HsbColor { A = alpha, H = hue, S = saturation, B = brightness };

		public static HsbColor FromArgb(Color source)
		{
			float fr = source.R / 255F;
			float fg = source.G / 255F;
			float fb = source.B / 255F;

			float max = Max(Max(fr, fg), fb);
			float min = Min(Min(fr, fg), fb);

			float c = max - min;

			float h;
			if (c == 0F)
			{
				h = 0F;
			}
			else if (max == fr)
			{
				h = (fg - fb) / c;
			}
			else if (max == fg)
			{
				h = 2f + (fb - fr) / c;
			}
			else
			{
				h = 4f + (fr - fg) / c;
			}
			h *= 60F;
			if (h < 0F)
			{
				h += 360F;
			}

			float s;
			if (max == 0F)
			{
				s = 0F;
			}
			else
			{
				s = c / max;
			}

			float b = max;

			return new HsbColor { A = source.A, H = h, S = s, B = b };
		}

		public static Color ToArgb(HsbColor source)
		{
			float fr = source.B;
			float fg = source.B;
			float fb = source.B;

			if (0F < source.S)
			{
				float h = source.H / 60F;
				int i = (int)Floor(h);
				float f = h - i;

				float p = source.B * (1F - source.S);
				float q = source.B * (1F - source.S * f);
				float t = source.B * (1F - source.S * (1F - f));

				switch (i)
				{
					case 0:
						fg = t;
						fb = p;
						break;
					case 1:
						fr = q;
						fb = p;
						break;
					case 2:
						fr = p;
						fb = t;
						break;
					case 3:
						fr = p;
						fg = q;
						break;
					case 4:
						fr = t;
						fg = p;
						break;
					case 5:
						fg = p;
						fb = q;
						break;
					default:
						throw new InvalidOperationException(); // This must not happen.
				}
			}

			byte r = (byte)Max(Min(Round(fr * 255F), 255), 0);
			byte g = (byte)Max(Min(Round(fg * 255F), 255), 0);
			byte b = (byte)Max(Min(Round(fb * 255F), 255), 0);

			return Color.FromArgb(source.A, r, g, b);
		}

		public Color ToArgb() => ToArgb(this);

		#endregion

		#region Other

		public override string ToString() => $"{{A={A},H={H},S={S},B={B}}}";

		#endregion
	}
}