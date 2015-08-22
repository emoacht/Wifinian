using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using static System.Math;

using WlanProfileViewer.Common;

namespace WlanProfileViewer.Models
{
	/// <summary>
	/// This application's settings
	/// </summary>
	public class Settings : BindableBase
	{
		public static Settings Current { get; } = new Settings();

		public WindowPlacement.WINDOWPLACEMENT? Placement { get; set; }

		#region Settings

		private const int _intervalMin = 4;
		private const int _intervalMax = 16;

		public int AutoReloadInterval
		{
			get { return _autoReloadInterval; }
			set
			{
				var clamped = Max(Min(value, _intervalMax), _intervalMin);
				SetProperty(ref _autoReloadInterval, clamped);
			}
		}
		private int _autoReloadInterval = 8; // Default

		#endregion

		#region Load/Save

		public static bool IsLoaded { get; private set; }

		private const string _settingsFileName = "settings.xml";
		private static readonly string _settingsFilePath = Path.Combine(FolderPathAppData, _settingsFileName);

		public static void Load()
		{
			try
			{
				if (File.Exists(_settingsFilePath))
				{
					using (var fs = new FileStream(_settingsFilePath, FileMode.Open, FileAccess.Read))
					{
						var serializer = new XmlSerializer(typeof(Settings));
						var loaded = (Settings)serializer.Deserialize(fs);

						typeof(Settings)
							.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
							.Where(x => x.CanWrite)
							.ToList()
							.ForEach(x => x.SetValue(Current, x.GetValue(loaded)));
					}
				}
			}
			catch (Exception ex)
			{
				throw new Exception("Failed to load settings.", ex);
			}

			IsLoaded = true;
		}

		public static void Save()
		{
			if (!IsLoaded)
				return;

			try
			{
				PrepareFolderAppData();

				using (var fs = new FileStream(_settingsFilePath, FileMode.Create, FileAccess.Write))
				{
					var serializer = new XmlSerializer(typeof(Settings));
					serializer.Serialize(fs, Current);
				}
			}
			catch (Exception ex)
			{
				throw new Exception("Failed to save settings.", ex);
			}
		}

		#endregion

		#region Prepare

		private static string FolderPathAppData
		{
			get
			{
				if (_folderPathAppData == null)
				{
					var pathAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
					if (string.IsNullOrEmpty(pathAppData)) // This should not happen.
						throw new DirectoryNotFoundException();

					_folderPathAppData = Path.Combine(pathAppData, Assembly.GetExecutingAssembly().GetName().Name);
				}
				return _folderPathAppData;
			}
		}
		private static string _folderPathAppData;

		private static void PrepareFolderAppData()
		{
			if (!Directory.Exists(FolderPathAppData))
				Directory.CreateDirectory(FolderPathAppData);
		}

		#endregion
	}
}