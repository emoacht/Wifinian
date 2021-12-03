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
		private const string AvailableKey = "Profile.Background.AvailableColor";
		private const string AvailableSelectedKey = "Profile.Background.AvailableSelectedColor";
		private const string ConnectedKey = "Profile.Background.ConnectedColor";
		private const string ConnectedSelectedKey = "Profile.Background.ConnectedSelectedColor";

		public static void AdjustResourceColors(ResourceDictionary resources)
		{
			const float factor = 1.08F;

			AdjustResourceColor(AvailableKey, AvailableSelectedKey);
			AdjustResourceColor(ConnectedKey, ConnectedSelectedKey);

			void AdjustResourceColor(string sourceKey, string targetKey)
			{
				if (resources.Contains(sourceKey))
				{
					var sourceColor = (Color)resources[sourceKey];
					resources[targetKey] = sourceColor.ToBrightened(factor);
				}
			}
		}
	}
}