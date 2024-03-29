﻿using System.Windows;
using System.Windows.Controls;

namespace Wifinian.Views.Controls;

[TemplateVisualState(Name = "Normal", GroupName = "CommonStates")]
[TemplateVisualState(Name = "MouseOver", GroupName = "CommonStates")]
[TemplateVisualState(Name = "Pressed", GroupName = "CommonStates")]
[TemplateVisualState(Name = "Disabled", GroupName = "CommonStates")]
[TemplateVisualState(Name = "Checked", GroupName = "CommonStates")]
public class SimpleToggleButton : Button
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
			typeof(SimpleToggleButton),
			new PropertyMetadata(false));

	#endregion

	public override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		//this.LayoutUpdated += (sender, e) => UpdateState(false);
	}

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