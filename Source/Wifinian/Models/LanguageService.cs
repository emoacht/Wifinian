using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Wifinian.Models
{
	internal static class LanguageService
	{
		#region Content

		// Button
		public static string Rush => GetContentValue();
		public static string Engage => GetContentValue();
		public static string Organize => GetContentValue();
		public static string Up => GetContentValue();
		public static string Down => GetContentValue();
		public static string Delete => GetContentValue();
		public static string AutoConnect => GetContentValue();
		public static string AutoSwitch => GetContentValue();
		public static string OK => GetContentValue();
		public static string Cancel => GetContentValue();

		// Link
		public static string ProjectSite => GetContentValue(fallback: Properties.Resources.ProjectSite);
		public static string License => Properties.Resources.License;

		// Menu
		public static string StartSignIn => GetContentValue();
		public static string ShowAvailable => GetContentValue();
		public static string Close => GetContentValue();

		// Message
		public static string NotWorkable => GetContentValue();
		public static string RecordException => GetContentValue();

		#endregion

		private static Dictionary<string, string> _languageContent;

		private static string GetContentValue([CallerMemberName] string key = null, string fallback = null)
		{
			return _languageContent.TryGetValue(key, out string value)
				? value
				: fallback ?? key;
		}

		public static async Task InitializeAsync()
		{
			var culture = CultureInfo.CurrentUICulture;
			var content = await Task.Run(() => RetrieveLanguageContentFromFile(culture)).ConfigureAwait(false);
#if !DEBUG
			content ??= RetrieveLanguageContentFromResources(culture);
#endif
			content += Environment.NewLine + RetrieveLanguageContentFromResources(new CultureInfo("en")); // fallback

			_languageContent = ParseLanguageContent(content);
		}

		#region Base

		private static readonly Regex _languageFilePattern = new("^language[._](?<name>[a-z]{2,3}(|[-_][a-zA-Z]+)(|[-_][a-zA-Z]+))(|[._]txt)$"); // e.g. language.en.txt or language_en
		private const char Delimiter = '=';

		private static string RetrieveLanguageContentFromFile(CultureInfo culture)
		{
			var folderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			if (string.IsNullOrEmpty(folderPath))
				return null;

			var sources = Directory.GetFiles(folderPath)
				.Select(x => (match: _languageFilePattern.Match(Path.GetFileName(x)), filePath: x))
				.Where(x => x.match.Success)
				.ToDictionary(x => x.match.Groups["name"].Value, x => x.filePath);
			if (sources.Count == 0)
				return null;

			var buffer = culture;
			while (true)
			{
				if (sources.TryGetValue(buffer.Name, out string filePath))
					return File.ReadAllText(filePath);

				buffer = buffer.Parent;
				if (buffer == CultureInfo.InvariantCulture)
					return null;
			}
		}

		private static string RetrieveLanguageContentFromResources(CultureInfo culture)
		{
			var sources = Properties.Resources.ResourceManager.GetResourceSet(CultureInfo.InvariantCulture, true, true)
				.Cast<DictionaryEntry>()
				.Select(x => (match: _languageFilePattern.Match(x.Key.ToString()), x.Value))
				.Where(x => x.match.Success)
				.ToDictionary(x => x.match.Groups["name"].Value.Replace('_', '-'), x => x.Value.ToString());

			var buffer = culture;
			while (true)
			{
				if (sources.TryGetValue(buffer.Name, out string content))
					return content;

				buffer = buffer.Parent;
				if (buffer == CultureInfo.InvariantCulture)
					return null;
			}
		}

		private static Dictionary<string, string> ParseLanguageContent(string source)
		{
			return source.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
				.Select(x => x.Split(new[] { Delimiter }, 2))
				.Select(x => x.Select(y => y.Trim()).Where(y => !string.IsNullOrEmpty(y)).ToArray())
				.Where(x => x.Length == 2) // Both key and value are not empty.
				.Select(x => (key: x[0], value: x[1]))
				.GroupBy(x => x.key)
				.ToDictionary(x => x.Key, x => x.FirstOrDefault().value);
		}

		#endregion
	}
}