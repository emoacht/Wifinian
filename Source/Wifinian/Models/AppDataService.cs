using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace Wifinian.Models;

internal static class AppDataService
{
	public static string FolderPath
	{
		get
		{
			if (_folderPath is null)
			{
				var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
				if (string.IsNullOrEmpty(appDataPath)) // This should not happen.
					throw new DirectoryNotFoundException();

				_folderPath = Path.Combine(appDataPath, ProductInfo.Product);
			}
			return _folderPath;
		}
	}
	private static string _folderPath;

	public static string EnsureFolderPath()
	{
		if (!Directory.Exists(FolderPath))
			Directory.CreateDirectory(FolderPath);

		return FolderPath;
	}

	public static void Load<T>(T instance, string fileName) where T : class
	{
		var filePath = Path.Combine(FolderPath, fileName);
		var fileInfo = new FileInfo(filePath);
		if (!fileInfo.Exists || (fileInfo.Length == 0))
			return;

		using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);

		var type = instance.GetType(); // GetType method works in derived class.
		var serializer = new XmlSerializer(type);
		var loaded = (T)serializer.Deserialize(fs);

		type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
			.Where(x => x.CanWrite)
			.ToList()
			.ForEach(x => x.SetValue(instance, x.GetValue(loaded)));
	}

	public static void Save<T>(T instance, string fileName) where T : class
	{
		EnsureFolderPath();
		var filePath = Path.Combine(FolderPath, fileName);

		using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);

		var type = instance.GetType(); // GetType method works in derived class.
		var serializer = new XmlSerializer(type);
		serializer.Serialize(fs, instance);
	}
}