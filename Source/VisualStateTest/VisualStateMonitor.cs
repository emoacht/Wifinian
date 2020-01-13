using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace VisualStateTest
{
	public class VisualStateMonitor : DependencyObject
	{
		public static double GetInterval(DependencyObject obj)
		{
			return (double)obj.GetValue(IntervalProperty);
		}
		public static void SetInterval(DependencyObject obj, double value)
		{
			obj.SetValue(IntervalProperty, value);
		}
		public static readonly DependencyProperty IntervalProperty =
			DependencyProperty.RegisterAttached(
				"Interval",
				typeof(double),
				typeof(VisualStateMonitor),
				new FrameworkPropertyMetadata(0D, OnIntervalChanged));

		private static void OnIntervalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (!(d is FrameworkElement element))
				return;

			var interval = (double)e.NewValue;
			if (interval <= 0D)
				return;

			Task.Run(async () =>
			{
				while (true)
				{
					element.Dispatcher.Invoke(() => CheckVisualState(element));

					await Task.Delay(TimeSpan.FromSeconds(interval));
				}
			});
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