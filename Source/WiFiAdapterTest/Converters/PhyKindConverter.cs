using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Windows.Devices.WiFi;

namespace WiFiAdapterTest.Converters;

[ValueConversion(typeof(WiFiPhyKind), typeof(string))]
internal class PhyKindConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is WiFiPhyKind kind)
		{
			return kind switch
			{
				WiFiPhyKind.Ofdm => "a",
				WiFiPhyKind.Hrdsss => "b",
				WiFiPhyKind.Erp => "g",
				WiFiPhyKind.HT => "n",
				WiFiPhyKind.Vht => "ac",
				WiFiPhyKind.Dmg => "ad",
				WiFiPhyKind.HE => "ax",
				WiFiPhyKind.Eht => "be",
				_ => kind.ToString()
			};
		}
		return DependencyProperty.UnsetValue;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}
}