using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

using Wifinian.Helper;

namespace Wifinian.Views
{
	public class ThemeService
	{
		private const string NormalKey = "Profile.Background.NormalColor";
		private const string NormalSelectedKey = "Profile.Background.NormalSelectedColor";
		private const string AvailableKey = "Profile.Background.AvailableColor";
		private const string AvailableSelectedKey = "Profile.Background.AvailableSelectedColor";
		private const string ConnectedKey = "Profile.Background.ConnectedColor";
		private const string ConnectedSelectedKey = "Profile.Background.ConnectedSelectedColor";

		public static void AdjustResourceColors(ResourceDictionary resources)
		{
			const float factor = 1.08F;

			AdjustResourceColor(resources, NormalKey, NormalSelectedKey, factor);
			AdjustResourceColor(resources, AvailableKey, AvailableSelectedKey, factor);
			AdjustResourceColor(resources, ConnectedKey, ConnectedSelectedKey, factor);
		}

		private static void AdjustResourceColor(ResourceDictionary resources, string sourceKey, string targetKey, float factor)
		{
			if (!resources.Contains(sourceKey))
				return;

			var sourceColor = (Color)resources[sourceKey];
			var targetColor = sourceColor.ToBrightened(factor);

			if (resources.Contains(targetKey))
			{
				resources[targetKey] = targetColor;
			}
			else
			{
				resources.Add(targetKey, targetColor);
			}
		}
	}
}