using System;
using System.Diagnostics;
using System.Windows;

using Wifinian.Helper;

namespace Wifinian.Models;

internal class LocationInfo
{
	public static bool PromptOpenLocation(Exception ex)
	{
		if ((ex is UnauthorizedAccessException or { InnerException: UnauthorizedAccessException })
			&& OsVersion.Is11Build26100OrGreater)
		{
			if (MessageBox.Show(
				LanguageService.OpenLocation,
				ProductInfo.Title,
				MessageBoxButton.YesNo,
				MessageBoxImage.Exclamation,
				MessageBoxResult.Yes) is MessageBoxResult.Yes)
			{
				Process.Start("explorer.exe", "ms-settings:privacy-location");
				return true;
			}
		}
		return false;
	}
}