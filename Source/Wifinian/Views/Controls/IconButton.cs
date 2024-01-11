using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Wifinian.Views.Controls;

public class IconButton : Button
{
	public Geometry IconData
	{
		get { return (Geometry)GetValue(IconDataProperty); }
		set { SetValue(IconDataProperty, value); }
	}
	public static readonly DependencyProperty IconDataProperty =
		DependencyProperty.Register(
			"IconData",
			typeof(Geometry),
			typeof(IconButton),
			new PropertyMetadata(null));
}