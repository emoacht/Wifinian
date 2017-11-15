using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WlanProfileViewer.Models
{
	/// <summary>
	/// This application's AppData folder
	/// </summary>
	internal static class FolderService
	{
		public static string FolderAppDataPath
		{
			get
			{
				if (_folderPathAppData == null)
				{
					var pathAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
					if (string.IsNullOrEmpty(pathAppData)) // This should not happen.
						throw new DirectoryNotFoundException();

					_folderPathAppData = Path.Combine(pathAppData, Assembly.GetExecutingAssembly().GetName().Name);
				}
				return _folderPathAppData;
			}
		}
		private static string _folderPathAppData;

		public static void AssureFolderAppData()
		{
			if (!Directory.Exists(FolderAppDataPath))
				Directory.CreateDirectory(FolderAppDataPath);
		}
	}
}