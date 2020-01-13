using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
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
			set => SetPropertyValue(ref _rescanInterval, value, MinInterval, MaxInterval);
		}
		private int _rescanInterval = 30; // Default

		public void IncrementRescanInterval() =>
			RescanInterval = IncrementLoop(RescanInterval, MinInterval, MaxInterval);

		private const int MinThreshold = 50;
		private const int MaxThreshold = 90;

		public int SignalThreshold
		{
			get => _signalThreshold;
			set => SetPropertyValue(ref _signalThreshold, value, MinThreshold, MaxThreshold);
		}
		private int _signalThreshold = 50; // Default

		public void IncrementSignalThreshold() =>
			SignalThreshold = IncrementLoop(SignalThreshold, MinThreshold, MaxThreshold);

		private static int IncrementLoop(int value, int min, int max)
		{
			var buffer = (value / 10 + 1) * 10;
			return (buffer <= max) ? buffer : min;
		}

		public Size MainWindowSize
		{
			get => _mainWindowSize;
			set => SetPropertyValue(ref _mainWindowSize, value);
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
		private static readonly string _settingsFilePath = Path.Combine(FolderService.AppDataFolderPath, SettingsFileName);

		private static void Load<T>(T instance) where T : class
		{
			var fileInfo = new FileInfo(_settingsFilePath);
			if (!fileInfo.Exists || (fileInfo.Length == 0))
				return;

			try
			{
				using (var fs = new FileStream(_settingsFilePath, FileMode.Open, FileAccess.Read))
				{
					var serializer = new XmlSerializer(typeof(T));
					var loaded = (T)serializer.Deserialize(fs);

					typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
						.Where(x => x.CanWrite)
						.ToList()
						.ForEach(x => x.SetValue(instance, x.GetValue(loaded)));
				}
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
				FolderService.AssureAppDataFolder();

				using (var fs = new FileStream(_settingsFilePath, FileMode.Create, FileAccess.Write))
				{
					var serializer = new XmlSerializer(typeof(T));
					serializer.Serialize(fs, instance);
				}
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