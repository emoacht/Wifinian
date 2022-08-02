using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Reactive.Bindings.Extensions;

using Wifinian.Common;

namespace Wifinian.Models
{
	/// <summary>
	/// Persistent settings
	/// </summary>
	public class Settings : DisposableBase
	{
		public static Settings Current => _current.Value;
		private static readonly Lazy<Settings> _current = new Lazy<Settings>(() => new Settings());

		private Settings()
		{ }

		#region Settings

		private const int MinInterval = 10;
		private const int MaxInterval = 30;

		public int RescanInterval
		{
			get => _rescanInterval;
			set => SetProperty(ref _rescanInterval, value, MinInterval, MaxInterval);
		}
		private int _rescanInterval = 30; // Default

		public void IncrementRescanInterval() =>
			RescanInterval = IncrementLoop(RescanInterval, MinInterval, MaxInterval);

		private const int MinThreshold = 50;
		private const int MaxThreshold = 90;

		public int SignalThreshold
		{
			get => _signalThreshold;
			set => SetProperty(ref _signalThreshold, value, MinThreshold, MaxThreshold);
		}
		private int _signalThreshold = 50; // Default

		public void IncrementSignalThreshold() =>
			SignalThreshold = IncrementLoop(SignalThreshold, MinThreshold, MaxThreshold);

		private static int IncrementLoop(int value, int min, int max)
		{
			var buffer = (value / 10 + 1) * 10;
			return (buffer <= max) ? buffer : min;
		}

		public bool EngagesPriority
		{
			get => _engagesPriority;
			set => SetProperty(ref _engagesPriority, value);
		}
		private bool _engagesPriority;

		public bool ShowsAvailable
		{
			get => _showsAvailable;
			set => SetProperty(ref _showsAvailable, value);
		}
		private bool _showsAvailable;

		public Size MainWindowSize
		{
			get => _mainWindowSize;
			set => SetProperty(ref _mainWindowSize, value);
		}
		private Size _mainWindowSize;

		#endregion

		internal void Initiate()
		{
			Load(this);

			this.PropertyChangedAsObservable()
				.Throttle(TimeSpan.FromMilliseconds(100))
				.Subscribe(_ => Save(this))
				.AddTo(this.Subscription);
		}

		#region Load/Save

		private const string SettingsFileName = "settings.xml";

		private static void Load<T>(T instance) where T : class
		{
			try
			{
				AppDataService.Load(instance, SettingsFileName);
			}
			catch (Exception ex)
			{
				Trace.WriteLine("Failed to load settings." + Environment.NewLine
					+ ex);
			}
		}

		private static void Save<T>(T instance) where T : class
		{
			try
			{
				AppDataService.Save(instance, SettingsFileName);
			}
			catch (Exception ex)
			{
				Trace.WriteLine("Failed to save settings." + Environment.NewLine
					+ ex);
			}
		}

		#endregion
	}
}