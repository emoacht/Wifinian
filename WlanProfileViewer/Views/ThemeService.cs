using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

using WlanProfileViewer.Helper;

namespace WlanProfileViewer.Views
{
	public class ThemeService
	{
		private const string _normalKey = "Profile.Normal.BackgroundColor";
		private const string _normalSelectedKey = "Profile.Normal.Selected.BackgroundColor";
		private const string _availableKey = "Profile.Available.BackgroundColor";
		private const string _availableSelectedKey = "Profile.Available.Selected.BackgroundColor";
		private const string _connectedKey = "Profile.Connected.BackgroundColor";
		private const string _connectedSelectedKey = "Profile.Connected.Selected.BackgroundColor";
		private const string _configModeKey = "Profile.ConfigMode.BackgroundColor";
		private const string _configModeSelectedKey = "Profile.ConfigMode.Selected.BackgroundColor";

		public static void AdjustResourceColors(ResourceDictionary resources)
		{
			const float factor = 1.08F;

			AdjustResourceColor(resources, _normalKey, _normalSelectedKey, factor);
			AdjustResourceColor(resources, _availableKey, _availableSelectedKey, factor);
			AdjustResourceColor(resources, _connectedKey, _connectedSelectedKey, factor);
			AdjustResourceColor(resources, _configModeKey, _configModeSelectedKey, factor);
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