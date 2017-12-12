using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Wifinian.Models
{
	internal static class LanguageService
	{
		public static string ProjectSite => GetContentValue() ?? Properties.Resources.ProjectSite;
		public static string RecordException => GetContentValue() ?? "Exception";

		#region Base

		private static string _languageName;
		private static Dictionary<string, string> _languageContent;
		private const char Delimiter = '=';

		private static string GetContentValue([CallerMemberName] string key = null)
		{
			var languageName = CultureInfo.CurrentUICulture.Parent.Name; // Language name only
			if (string.IsNullOrEmpty(languageName))
				return null;

			if (_languageName != languageName)
			{
				_languageName = languageName;
				_languageContent = RetrieveLanguageContent(languageName);
			}
			return _languageContent.TryGetValue(key, out string value) ? value : null;
		}

		private static Dictionary<string, string> RetrieveLanguageContent(string languageName)
		{
			var sources = Properties.Resources.ResourceManager.GetResourceSet(CultureInfo.InvariantCulture, true, true)
				.Cast<DictionaryEntry>()
				.Where(x => x.Key.ToString().StartsWith("language_")) // language_en, language_ja, etc.
				.ToDictionary(x => x.Key.ToString().Substring(9), x => x.Value.ToString()); // 9 means "language_".

			var source = sources.ContainsKey(languageName)
				? sources[languageName]
				: sources["en"]; // language_en as fallback

			return source.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
				.Select(x => x.Split(new[] { Delimiter }, 2))
				.Select(x => x.Select(y => y.Trim()).Where(y => !string.IsNullOrEmpty(y)).ToArray())
				.Where(x => x.Length == 2) // Both key and value are not empty.
				.ToDictionary(x => x[0], x => x[1]);
		}

		#endregion
	}
}