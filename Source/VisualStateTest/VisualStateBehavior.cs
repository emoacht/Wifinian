using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Xaml.Behaviors;

namespace VisualStateTest
{
	[TypeConstraint(typeof(FrameworkElement))]
	public class VisualStateBehavior : Behavior<FrameworkElement>
	{
		public int Interval
		{
			get { return (int)GetValue(IntervalProperty); }
			set { SetValue(IntervalProperty, value); }
		}
		public static readonly DependencyProperty IntervalProperty =
			DependencyProperty.Register(
				"Interval",
				typeof(int),
				typeof(VisualStateBehavior),
				new FrameworkPropertyMetadata(3));

		private DispatcherTimer _timer;

		protected override void OnAttached()
		{
			base.OnAttached();

			_timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(Interval) };
			_timer.Tick += (sender, e) => CheckVisualState(this.AssociatedObject);
			_timer.Start();
		}

		protected override void OnDetaching()
		{
			base.OnDetaching();

			_timer?.Stop();
		}

		private static void CheckVisualState(FrameworkElement element)
		{
			var groups = GetVisualStateGroups(element);
			if (groups != null)
			{
				foreach (var group in groups)
					Debug.WriteLine($"Element: {element.Name} -> Group: {group.Name} -> State: {group.CurrentState?.Name}");
			}
		}

		private static IEnumerable<VisualStateGroup> GetVisualStateGroups(FrameworkElement element)
		{
			if (VisualTreeHelper.GetChildrenCount(element) <= 0) // If the ControlTemplate has not been applied yet
				return null;

			foreach (var descendant in GetDescendants(element).OfType<FrameworkElement>())
			{
				var groups = VisualStateManager.GetVisualStateGroups(descendant)?.Cast<VisualStateGroup>();
				if (groups != null)
					return groups;
			}
			return null;
		}

		private static IEnumerable<DependencyObject> GetDescendants(DependencyObject reference)
		{
			if (reference is null)
				yield break;

			var queue = new Queue<DependencyObject>();

			do
			{
				var parent = (queue.Count == 0) ? reference : queue.Dequeue();

				var count = VisualTreeHelper.GetChildrenCount(parent);
				for (int i = 0; i < count; i++)
				{
					var child = VisualTreeHelper.GetChild(parent, i);
					queue.Enqueue(child);

					yield return child;
				}
			}
			while (queue.Count > 0);
		}
	}
}