using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WlanProfileViewer.Views.Controls
{
	[TemplateVisualState(Name = "Normal", GroupName = "CommonStates")]
	[TemplateVisualState(Name = "NormalSelected", GroupName = "CommonStates")]
	[TemplateVisualState(Name = "Available", GroupName = "CommonStates")]
	[TemplateVisualState(Name = "AvailableSelected", GroupName = "CommonStates")]
	[TemplateVisualState(Name = "Connected", GroupName = "CommonStates")]
	[TemplateVisualState(Name = "ConnectedSelected", GroupName = "CommonStates")]
	[TemplateVisualState(Name = "ConfigMode", GroupName = "CommonStates")]
	[TemplateVisualState(Name = "ConfigModeSelected", GroupName = "CommonStates")]
	public class ProfileContentControl : ContentControl
	{
		#region Property

		public bool IsSelected
		{
			get { return (bool)GetValue(IsSelectedProperty); }
			set { SetValue(IsSelectedProperty, value); }
		}
		public static readonly DependencyProperty IsSelectedProperty =
			DependencyProperty.Register(
				"IsSelected",
				typeof(bool),
				typeof(ProfileContentControl),
				new PropertyMetadata(false));

		public bool IsAvailable
		{
			get { return (bool)GetValue(IsAvailableProperty); }
			set { SetValue(IsAvailableProperty, value); }
		}
		public static readonly DependencyProperty IsAvailableProperty =
			DependencyProperty.Register(
				"IsAvailable",
				typeof(bool),
				typeof(ProfileContentControl),
				new PropertyMetadata(false));

		public bool IsConnected
		{
			get { return (bool)GetValue(IsConnectedProperty); }
			set { SetValue(IsConnectedProperty, value); }
		}
		public static readonly DependencyProperty IsConnectedProperty =
			DependencyProperty.Register(
				"IsConnected",
				typeof(bool),
				typeof(ProfileContentControl),
				new PropertyMetadata(false));

		public bool IsConfigMode
		{
			get { return (bool)GetValue(IsConfigModeProperty); }
			set { SetValue(IsConfigModeProperty, value); }
		}
		public static readonly DependencyProperty IsConfigModeProperty =
			DependencyProperty.Register(
				"IsConfigMode",
				typeof(bool),
				typeof(ProfileContentControl),
				new PropertyMetadata(false));

		#endregion

		public ProfileContentControl()
		{
			this.Loaded += OnLoaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			UpdateState(true);
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);

			if ((e.Property == IsMouseOverProperty) ||
				(e.Property == IsSelectedProperty) ||
				(e.Property == IsAvailableProperty) ||
				(e.Property == IsConnectedProperty) ||
				(e.Property == IsConfigModeProperty))
				UpdateState(true);
		}

		protected virtual void UpdateState(bool useTransitions)
		{
			VisualStateManager.GoToState(this, GetVisualStateName(), useTransitions);
		}

		private string GetVisualStateName()
		{
			if (IsConfigMode && IsSelected)
			{
				return "ConfigMode" +
					(IsMouseOver ? "Selected" : string.Empty);
			}
			else
			{
				return (IsConnected ? "Connected" : (IsAvailable ? "Available" : "Normal")) +
					((IsSelected || IsMouseOver) ? "Selected" : string.Empty);
			}
		}
	}
}