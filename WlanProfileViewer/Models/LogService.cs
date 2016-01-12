using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using WlanProfileViewer.Views;

namespace WlanProfileViewer.Models
{
	internal class LogService
	{
		#region Exception

		private const string _exceptionFileName = "exception.log";

		public static void RecordException(object sender, Exception exception)
		{
			var content = $"[Date: {DateTime.Now} Sender: {sender}]" + Environment.NewLine
				+ exception + Environment.NewLine + Environment.NewLine;

			RecordAppData(_exceptionFileName, content);
			RecordDesktop(_exceptionFileName, content);
		}

		private static void RecordAppData(string fileName, string content)
		{
			try
			{
				PrepareFolderAppData();

				var filePathAppData = Path.Combine(FolderPathAppData, fileName);

				UpdateText(filePathAppData, content);
			}
			catch (Exception ex)
			{
				Trace.WriteLine($"Failed to record log to AppData.\r\n{ex}");
			}
		}

		private static void RecordDesktop(string fileName, string content)
		{
			var result = MessageBox.Show(LanguageService.Content("RecordException"), MainWindow.ProductTitle, MessageBoxButton.YesNo, MessageBoxImage.Error, MessageBoxResult.Yes);
			if (result != MessageBoxResult.Yes)
				return;

			try
			{
				var filePathDesktop = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
					fileName);

				UpdateText(filePathDesktop, content);
			}
			catch (Exception ex)
			{
				Trace.WriteLine($"Failed to record log to Desktop.\r\n{ex}");
			}
		}

		private static void UpdateText(string filePath, string newContent)
		{
			string oldContent = null;

			if (File.Exists(filePath) && (File.GetLastWriteTime(filePath) > DateTime.Now.AddDays(-1)))
			{
				using (var sr = new StreamReader(filePath, Encoding.UTF8))
					oldContent = sr.ReadToEnd();
			}

			using (var sw = new StreamWriter(filePath, false, Encoding.UTF8)) // BOM will be emitted.
				sw.Write(string.Join(Environment.NewLine, GetLastLines(oldContent, "[Date:", 9).Reverse()) + newContent);
		}

		private static IEnumerable<string> GetLastLines(string source, string sectionHeader, int sectionCount)
		{
			if (string.IsNullOrEmpty(source))
				yield break;

			var lines = source.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
			int count = 0;

			foreach (var line in lines.Reverse())
			{
				yield return line;

				if (!line.StartsWith(sectionHeader))
					continue;

				if (++count >= sectionCount)
					yield break;
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