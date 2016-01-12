using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace IconImage
{
	public partial class AppIcon : UserControl
	{
		public AppIcon()
		{
			InitializeComponent();

			this.Loaded += OnLoaded;
		}

		private Dictionary<string, double> _strokeThicknesses;

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			_strokeThicknesses = this.Resources.OfType<DictionaryEntry>()
				.Where(x => x.Value is double)
				.Where(x => x.Key.ToString().ToLower().Contains("strokethickness"))
				.ToDictionary(x => x.Key.ToString(), x => (double)x.Value);

			if (!_strokeThicknesses.Any())
				return;

			var parentGrid = VisualTreeHelper.GetParent(this) as Grid;
			if (parentGrid != null)
			{
				this.SetBinding(
					ScaleFactorProperty,
					new Binding("RenderTransform.ScaleX") { Source = parentGrid });
			}
		}

		public double ScaleFactor
		{
			get { return (double)GetValue(ScaleFactorProperty); }
			set { SetValue(ScaleFactorProperty, value); }
		}
		public static readonly DependencyProperty ScaleFactorProperty =
			DependencyProperty.Register(
				"ScaleFactor",
				typeof(double),
				typeof(AppIcon),
				new PropertyMetadata(1D, OnScaleFactorChanged));

		private static void OnScaleFactorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((AppIcon)d).AdjustStrokeThicknesses((double)e.NewValue);
		}

		private const double _strokeThicknessValueMin = 1.05;

		private void AdjustStrokeThicknesses(double factor)
		{
			foreach (var strokeThickness in _strokeThicknesses)
			{
				var strokeThicknessValue = strokeThickness.Value * factor;
				if (strokeThicknessValue < _strokeThicknessValueMin)
				{
					this.Resources[strokeThickness.Key] = _strokeThicknessValueMin / factor;
				}
			}
		}
	}
}