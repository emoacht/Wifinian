﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Wifinian.Views.Controls;

[TemplateVisualState(Name = "MovingOff", GroupName = "MovingStates")]
[TemplateVisualState(Name = "MovingOn", GroupName = "MovingStates")]
public class MovingButton : Button
{
	#region Property

	public bool IsRunning
	{
		get { return (bool)GetValue(IsRunningProperty); }
		set { SetValue(IsRunningProperty, value); }
	}
	public static readonly DependencyProperty IsRunningProperty =
		DependencyProperty.Register(
			"IsRunning",
			typeof(bool),
			typeof(MovingButton),
			new PropertyMetadata(
				false,
				async (d, e) => await ((MovingButton)d).ManageStateAsync()));

	#endregion

	private TimeSpan _duration = TimeSpan.FromSeconds(1D);

	public override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		var movingDuration = this.TryFindResource("MovingDuration");
		if (movingDuration is Duration buffer)
			_duration = buffer.TimeSpan;
	}

	private object _blocker = new();
	private bool _isRunning = false;

	private async Task ManageStateAsync()
	{
		if (Interlocked.Exchange(ref _blocker, null) is null)
			return;

		try
		{
			while (true)
			{
				if (_isRunning != IsRunning)
				{
					_isRunning = !_isRunning;
					UpdateState(true, _isRunning);
				}

				if (!_isRunning)
					break;

				await Task.Delay(_duration);
			}
		}
		finally
		{
			Interlocked.Exchange(ref _blocker, new object());
		}
	}

	private void UpdateState(bool useTransitions, bool isRunning)
	{
		var state = isRunning ? "MovingOn" : "MovingOff";

		VisualStateManager.GoToState(this, state, useTransitions);
	}
}