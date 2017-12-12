using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Wifinian.Views.Controls
{
	[TemplateVisualState(Name = "Normal", GroupName = "CommonStates")]
	[TemplateVisualState(Name = "MouseOver", GroupName = "CommonStates")]
	[TemplateVisualState(Name = "Pressed", GroupName = "CommonStates")]
	[TemplateVisualState(Name = "Disabled", GroupName = "CommonStates")]
	[TemplateVisualState(Name = "Checked", GroupName = "CommonStates")]
	public class PlainToggleButton : Button
	{
		#region Property

		public bool IsChecked
		{
			get { return (bool)GetValue(IsCheckedProperty); }
			set { SetValue(IsCheckedProperty, value); }
		}
		public static readonly DependencyProperty IsCheckedProperty =
			DependencyProperty.Register(
				"IsChecked",
				typeof(bool),
				typeof(PlainToggleButton),
				new FrameworkPropertyMetadata(false));

		#endregion

		protected override void OnClick()
		{
			OnToggle();

			base.OnClick();
		}

		protected virtual void OnToggle()
		{
			IsChecked = !IsChecked;
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);

			if ((e.Property == IsMouseOverProperty) ||
				(e.Property == IsKeyboardFocusedProperty) ||
				(e.Property == IsPressedProperty) ||
				(e.Property == IsEnabledProperty) ||
				(e.Property == IsCheckedProperty))
				UpdateState(true);
		}

		protected virtual void UpdateState(bool useTransitions)
		{
			VisualStateManager.GoToState(this, GetVisualStateName(), useTransitions);
		}

		private string GetVisualStateName()
		{
			if (!IsEnabled)
				return "Disabled";

			if (IsChecked)
				return "Checked";

			if (IsPressed)
				return "Pressed";

			if (IsMouseOver)
				return "MouseOver";

			return "Normal";
		}
	}
}