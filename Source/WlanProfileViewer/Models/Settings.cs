using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Reactive.Bindings.Extensions;

using WlanProfileViewer.Common;

namespace WlanProfileViewer.Models
{
	/// <summary>
	/// This application's settings
	/// </summary>
	public class Settings : DisposableBase
	{
		public static Settings Current => _current.Value;
		private static readonly Lazy<Settings> _current = new Lazy<Settings>(() => new Settings());

		private Settings()
		{ }

		#region Settings

		private const int IntervalMin = 10;
		private const int IntervalMax = 50;

		public int RescanInterval
		{
			get => _rescanInterval;
			set => SetPropertyValue(ref _rescanInterval, value, IntervalMin, IntervalMax);
		}
		private int _rescanInterval = 30; // Default

		public void IncrementRescanInterval() =>
			RescanInterval = IncrementLoop(RescanInterval, IntervalMin, IntervalMax);

		private const int ThresholdMin = 10;
		private const int ThresholdMax = 90;

		public int SignalThreshold
		{
			get => _signalThreshold;
			set => SetPropertyValue(ref _signalThreshold, value, ThresholdMin, ThresholdMax);
		}
		private int _signalThreshold = 50; // Default

		public void IncrementSignalThreshold() =>
			SignalThreshold = IncrementLoop(SignalThreshold, ThresholdMin, ThresholdMax);

		private int IncrementLoop(int value, int min, int max)
		{
			var buff = (value / 10 + 1) * 10;
			return (buff <= max) ? buff : min;
		}

		public double MainWindowHeight
		{
			get => _mainWindowHeight;
			set => SetPropertyValue(ref _mainWindowHeight, value);
		}
		private double _mainWindowHeight;

		#endregion

		public void Initiate()
		{
			Load(this);

			this.PropertyChangedAsObservable()
				.Throttle(TimeSpan.FromSeconds(1))
				.Subscribe(_ => Save(this))
				.AddTo(this.Subscription);
		}

		#region Load/Save

		private const string _settingsFileName = "settings.xml";
		private static readonly string _settingsFilePath = Path.Combine(FolderService.FolderAppDataPath, _settingsFileName);

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

					typeof(T)
						.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
						.Where(x => x.CanWrite)
						.ToList()
						.ForEach(x => x.SetValue(instance, x.GetValue(loaded)));
				}
			}
			catch (Exception ex)
			{
				try
				{
					File.Delete(_settingsFilePath);
				}
				catch
				{ }

				throw new Exception("Failed to load settings.", ex);
			}
		}

		private static void Save<T>(T instance) where T : class
		{
			try
			{
				FolderService.AssureFolderAppData();

				using (var fs = new FileStream(_settingsFilePath, FileMode.Create, FileAccess.Write))
				{
					var serializer = new XmlSerializer(typeof(T));
					serializer.Serialize(fs, instance);
				}
			}
			catch (Exception ex)
			{
				throw new Exception("Failed to save settings.", ex);
			}
		}

		#endregion
	}
}