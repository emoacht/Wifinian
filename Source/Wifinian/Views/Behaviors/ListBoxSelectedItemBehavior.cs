using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

namespace Wifinian.Views.Behaviors
{
	[TypeConstraint(typeof(ListBox))]
	public class ListBoxSelectedItemBehavior : Behavior<ListBox>
	{
		#region Property

		public bool IsSelectionSwitched
		{
			get { return (bool)GetValue(IsSelectionSwitchedProperty); }
			set { SetValue(IsSelectionSwitchedProperty, value); }
		}
		public static readonly DependencyProperty IsSelectionSwitchedProperty =
			DependencyProperty.Register(
				"IsSelectionSwitched",
				typeof(bool),
				typeof(ListBoxSelectedItemBehavior),
				new PropertyMetadata(false, OnIsSelectionSwitched));

		#endregion

		protected override void OnAttached()
		{
			base.OnAttached();

			this.AssociatedObject.SelectionChanged += OnSelectionChanged;
		}

		protected override void OnDetaching()
		{
			base.OnDetaching();

			this.AssociatedObject.SelectionChanged -= OnSelectionChanged;
		}

		private static void OnIsSelectionSwitched(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((ListBoxSelectedItemBehavior)d).AssociatedObject.SelectedIndex = -1; // Not selected
		}

		private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var listBox = (ListBox)sender;

			// Keep selected item always single while SelectionMode is Multiple.
			if (listBox.SelectedItems.Count > 1)
			{
				var lastSelectedItem = e.AddedItems.Cast<object>().LastOrDefault() ??
					listBox.SelectedItems[0]; // Fallback

				listBox.SelectedItems.Cast<object>()
					.Where(x => x != lastSelectedItem)
					.ToList()
					.ForEach(x => listBox.SelectedItems.Remove(x));
			}

			if ((listBox.SelectedIndex == -1) || (listBox.SelectedItem is null))
				return;

			var item = listBox.ItemContainerGenerator.ContainerFromItem(listBox.SelectedItem) as FrameworkElement;
			item?.BringIntoView();
		}
	}
}