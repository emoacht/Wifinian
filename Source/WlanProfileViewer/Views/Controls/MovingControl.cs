using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WlanProfileViewer.Views.Controls
{
	[TemplateVisualState(Name = "MovingOff", GroupName = "MovingStates")]
	[TemplateVisualState(Name = "MovingOn", GroupName = "MovingStates")]
	public class MovingControl : Control
	{
		public MovingControl()
		{ }

		static MovingControl()
		{
			UIElement.IsEnabledProperty.OverrideMetadata(
				typeof(MovingControl),
				new UIPropertyMetadata(
					false,
					async (d, e) =>
					{
						if (!(bool)e.NewValue)
							return;

						await ((MovingControl)d).ManageStateAsync();
					}));
		}

		private TimeSpan _duration = TimeSpan.FromSeconds(1D);

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			var movingDuration = this.TryFindResource("MovingDuration");
			if (movingDuration is Duration)
				_duration = ((Duration)movingDuration).TimeSpan;
		}

		private object _blocker = new object();
		private CancellationTokenSource _source;

		private async Task ManageStateAsync()
		{
			while (Interlocked.Exchange(ref _blocker, null) == null)
			{
				if (_source?.IsCancellationRequested == false)
					_source.Cancel();

				await Task.Delay(TimeSpan.FromMilliseconds(10));
			}

			if (_source?.IsCancellationRequested != false)
				_source = new CancellationTokenSource();

			UpdateState(true, true);

			try
			{
				await Task.Delay(_duration, _source.Token);
			}
			catch (TaskCanceledException)
			{
			}
			finally
			{
				UpdateState(true, false);

				Interlocked.Exchange(ref _blocker, new object());
			}
		}

		private void UpdateState(bool useTransitions, bool isRunning)
		{
			var state = isRunning ? "MovingOn" : "MovingOff";

			VisualStateManager.GoToState(this, state, useTransitions);
		}
	}
}