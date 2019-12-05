using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Xaml.Behaviors;

namespace Wifinian.Views.Behaviors
{
	[TypeConstraint(typeof(FrameworkElement))]
	public class FrameworkElementCenterBehavior : Behavior<FrameworkElement>
	{
		#region Property

		/// <summary>
		/// Target FrameworkElement to be used for centering
		/// </summary>
		public FrameworkElement Target
		{
			get { return (FrameworkElement)GetValue(TargetProperty); }
			set { SetValue(TargetProperty, value); }
		}
		public static readonly DependencyProperty TargetProperty =
			DependencyProperty.Register(
				"Target",
				typeof(FrameworkElement),
				typeof(FrameworkElementCenterBehavior),
				new PropertyMetadata(null));

		#endregion

		protected override void OnAttached()
		{
			base.OnAttached();

			// Default location must be left-top corner.
			this.AssociatedObject.HorizontalAlignment = HorizontalAlignment.Left;
			this.AssociatedObject.VerticalAlignment = VerticalAlignment.Top;

			this.AssociatedObject.Loaded += OnLoaded;

			// Add handler to any event which is suitable for triggering centering.
			this.AssociatedObject.IsEnabledChanged += OnIsEnabledChanged;
		}

		protected override void OnDetaching()
		{
			base.OnDetaching();

			this.AssociatedObject.Loaded -= OnLoaded;

			// Remove added handler.
			this.AssociatedObject.IsEnabledChanged -= OnIsEnabledChanged;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			Center();
		}

		private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (!(bool)e.NewValue)
				return;

			Center();
		}

		/// <summary>
		/// Makes associated FrameworkElement at the center of target FrameworkElement.
		/// </summary>
		private void Center()
		{
			if (Target is null)
				return;

			var targetLocation = Target.PointToScreen(default);
			var currentLocation = this.AssociatedObject.PointToScreen(default);

			var desiredLocationX = targetLocation.X + (Target.ActualWidth - this.AssociatedObject.ActualWidth) / 2D;
			var desiredLocationY = targetLocation.Y + (Target.ActualHeight - this.AssociatedObject.ActualHeight) / 2D;

			var desiredMargin = new Thickness(
				this.AssociatedObject.Margin.Left + (desiredLocationX - currentLocation.X),
				this.AssociatedObject.Margin.Top + (desiredLocationY - currentLocation.Y),
				0,
				0);

			this.AssociatedObject.Margin = desiredMargin;
		}
	}
}