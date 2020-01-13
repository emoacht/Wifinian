using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Wifinian.Models
{
	internal class LogService
	{
		private const string OperationFileName = "operation.log";
		private const string ExceptionFileName = "exception.log";

		private const string HeaderStart = "[Date:";
		private static string ComposeHeader() => $"{HeaderStart} {DateTime.Now} Ver: {ProductInfo.Version}]";

		/// <summary>
		/// Records operation log to AppData.
		/// </summary>
		/// <param name="content">Content</param>
		/// <remarks>
		/// A log file of previous dates will be overridden.
		/// </remarks>
		public static void RecordOperation(string content)
		{
			content = ComposeHeader() + Environment.NewLine
				+ content + Environment.NewLine + Environment.NewLine;

			RecordToAppData(OperationFileName, content);
		}

		/// <summary>
		/// Records exception log to AppData and Desktop.
		/// </summary>
		/// <param name="exception">Exception</param>
		/// <remarks>
		/// The log file will be appended with new content as long as one day has not yet passed
		/// since last write. Otherwise, the log file will be overwritten.
		/// </remarks>
		public static void RecordException(Exception exception)
		{
			var content = ComposeHeader() + Environment.NewLine
				+ exception.ToString() + Environment.NewLine + Environment.NewLine;

			RecordToAppData(ExceptionFileName, content);

			if (MessageBox.Show(
				LanguageService.RecordException,
				ProductInfo.Title,
				MessageBoxButton.YesNo,
				MessageBoxImage.Error,
				MessageBoxResult.Yes) != MessageBoxResult.Yes)
				return;

			RecordToDesktop(ExceptionFileName, content, 10);
		}

		#region Helper

		private static void RecordToAppData(string fileName, string content, int maxCount = 1)
		{
			try
			{
				FolderService.AssureAppDataFolder();

				var appDataFilePath = Path.Combine(
					FolderService.AppDataFolderPath,
					fileName);

				UpdateText(appDataFilePath, content, maxCount);
			}
			catch (Exception ex)
			{
				Trace.WriteLine("Failed to record log to AppData." + Environment.NewLine
					+ ex);
			}
		}

		private static void RecordToDesktop(string fileName, string content, int maxCount = 1)
		{
			try
			{
				var desktopFilePath = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
					fileName);

				UpdateText(desktopFilePath, content, maxCount);
			}
			catch (Exception ex)
			{
				Trace.WriteLine("Failed to record log to Desktop." + Environment.NewLine
					+ ex);
			}
		}

		private static void SaveText(string filePath, string content)
		{
			using (var sw = new StreamWriter(filePath, false, Encoding.UTF8)) // BOM will be emitted.
				sw.Write(content);
		}

		private static void UpdateText(string filePath, string newContent, int maxCount)
		{
			string oldContent = null;

			if ((1 < maxCount) && File.Exists(filePath) && (File.GetLastWriteTime(filePath) > DateTime.Now.AddDays(-1)))
			{
				using (var sr = new StreamReader(filePath, Encoding.UTF8))
					oldContent = sr.ReadToEnd();

				oldContent = TruncateSections(oldContent, HeaderStart, maxCount - 1);
			}

			SaveText(filePath, oldContent + newContent);
		}

		private static string TruncateSections(string source, string sectionHeader, int sectionCount)
		{
			if (string.IsNullOrEmpty(sectionHeader))
				throw new ArgumentNullException(nameof(sectionHeader));
			if (sectionCount <= 0)
				throw new ArgumentOutOfRangeException(nameof(sectionCount), sectionCount, "The count must be greater than 0.");

			if (string.IsNullOrEmpty(source))
				return string.Empty;

			var separator = Environment.NewLine + sectionHeader;
			int foundIndex = source.Length - 1;
			int startIndex = 0;

			for (int i = sectionCount; i > 0; i--)
			{
				foundIndex = source.LastIndexOf(separator, foundIndex, StringComparison.Ordinal);
				if (foundIndex < 0)
				{
					if (source.StartsWith(sectionHeader, StringComparison.Ordinal))
					{
						startIndex = 0;
					}
					break;
				}
				startIndex = foundIndex + Environment.NewLine.Length;
			}
			return source.Substring(startIndex);
		}

		#endregion
	}
}