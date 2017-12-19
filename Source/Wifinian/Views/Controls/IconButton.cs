using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Wifinian.Views.Controls
{
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
}