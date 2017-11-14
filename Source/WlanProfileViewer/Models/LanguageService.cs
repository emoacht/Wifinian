using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WlanProfileViewer.Models
{
	internal static class LanguageService
	{
		public static string ProjectSite =>
			Content("ProjectSite") ?? Properties.Resources.ProjectSite;

		public static string Content(string key)
		{
			CheckLanguageContent();

			return _languageContent.ContainsKey(key) ? _languageContent[key] : null;
		}

		#region Base

		private static string _languageName;
		private static Dictionary<string, string> _languageContent;
		private const char _delimiter = '=';

		private static void CheckLanguageContent()
		{
			var languageName = CultureInfo.CurrentUICulture.Parent.Name; // Language name only
			if (_languageName == languageName)
				return;

			_languageName = languageName;
			_languageContent = RetrieveLanguageContent(languageName);
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
				.Select(x => x.Split(new[] { _delimiter }, 2))
				.Select(x => x.Select(y => y.Trim()).Where(y => !string.IsNullOrEmpty(y)).ToArray())
				.Where(x => x.Length == 2) // Both key and value are not empty.
				.ToDictionary(x => x[0], x => x[1]);
		}

		#endregion
	}
}