using System;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Wifinian.Models;

namespace Wifinian.Test
{
	[TestClass]
	public class LanguageServiceTest
	{
		[TestMethod]
		public void TestLanguageFiles()
		{
			Assert.IsTrue(TestLanguageFilePattern("language.en.txt", "en"));
			Assert.IsTrue(TestLanguageFilePattern("language.ja.txt", "ja"));
			Assert.IsTrue(TestLanguageFilePattern("language.ja-JP.txt", "ja-JP"));
			Assert.IsTrue(TestLanguageFilePattern("language.zh-Hans-HK.txt", "zh-Hans-HK"));

			Assert.IsTrue(TestLanguageFilePattern("language_en", "en"));
			Assert.IsTrue(TestLanguageFilePattern("language_ja", "ja"));
			Assert.IsTrue(TestLanguageFilePattern("language_ja_JP", "ja_JP"));
			Assert.IsTrue(TestLanguageFilePattern("language_zh_Hans_HK", "zh_Hans_HK"));
		}

		#region Base

		private static Regex _languageFilePattern;

		[ClassInitialize]
		public static void InitializeLanguageFilePattern(TestContext context)
		{
			var @class = new PrivateType(typeof(LanguageService));
			_languageFilePattern = @class.GetStaticField("_languageFilePattern") as Regex;
		}

		private static bool TestLanguageFilePattern(string source, string expectedName)
		{
			var match = _languageFilePattern.Match(source);
			if (!match.Success)
				return false;

			var actualName = match.Groups["name"].Value;
			return string.Equals(expectedName, actualName, StringComparison.Ordinal);
		}

		#endregion
	}
}