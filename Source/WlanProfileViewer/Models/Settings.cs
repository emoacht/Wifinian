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
		public static Settings Current { get; } = new Settings();

		#region Settings

		private const int _intervalMin = 4;
		private const int _intervalMax = 16;

		public int AutoRescanInterval
		{
			get => _autoRescanInterval;
			set => SetPropertyValue(ref _autoRescanInterval, value, _intervalMin, _intervalMax);
		}
		private int _autoRescanInterval = 8; // Default

		#endregion

		public void Initiate()
		{
			Load(Current);

			Current.PropertyChangedAsObservable()
				.Throttle(TimeSpan.FromSeconds(1))
				.Subscribe(_ => Save(Current))
				.AddTo(Current.Subscription);
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