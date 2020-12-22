using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

namespace Wifinian.Views.Behaviors
{
	[TypeConstraint(typeof(ListBox))]
	public class ListBoxHeightBehavior : Behavior<ListBox>
	{
		#region Property

		public double ParentHeight
		{
			get { return (double)GetValue(ParentHeightProperty); }
			set { SetValue(ParentHeightProperty, value); }
		}
		public static readonly DependencyProperty ParentHeightProperty =
			DependencyProperty.Register(
				"ParentHeight",
				typeof(double),
				typeof(ListBoxHeightBehavior),
				new PropertyMetadata(
					0D,
					(d, e) => ((ListBoxHeightBehavior)d).AdjustHeight((double)e.NewValue)));

		#endregion

		private ScrollViewer _scrollHost;

		protected override void OnAttached()
		{
			base.OnAttached();

			this.AssociatedObject.Loaded += OnLoaded;
		}

		protected override void OnDetaching()
		{
			base.OnDetaching();

			this.AssociatedObject.Loaded -= OnLoaded;

			if (_scrollHost is not null)
				_scrollHost.ScrollChanged -= OnScrollChanged;
		}

		private void OnLoaded(object sender, EventArgs e)
		{
			// Get internal ScrollHost property.
			if (!TryGetNonPublicPropertyValue(this.AssociatedObject, "ScrollHost", out ScrollViewer scrollHost))
				return;

			_scrollHost = scrollHost;
			_scrollHost.ScrollChanged += OnScrollChanged;
			AdjustHeight(ParentHeight);
		}

		private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
		{
			AdjustHeight(ParentHeight);
		}

		private void AdjustHeight(double parentHeight)
		{
			if (_scrollHost is null)
				return;

			this.AssociatedObject.Height = Math.Min(_scrollHost.ExtentHeight, parentHeight);
		}

		private static bool TryGetNonPublicPropertyValue<TInstance, TValue>(TInstance instance, string propertyName, out TValue fieldValue)
		{
			var fieldInfo = typeof(TInstance).GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Instance);
			if (fieldInfo?.GetValue(instance) is TValue value)
			{
				fieldValue = value;
				return true;
			}
			fieldValue = default;
			return false;
		}
	}
}