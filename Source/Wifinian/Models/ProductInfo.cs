using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Wifinian.Models
{
	public static class ProductInfo
	{
		private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();

		public static Version Version { get; } = _assembly.GetName().Version;

		public static string Product { get; } = _assembly.GetAttribute<AssemblyProductAttribute>().Product;

		public static string Title { get; } = _assembly.GetAttribute<AssemblyTitleAttribute>().Title;

		public static string StartupTaskId => GetAppSettings();

		private static TAttribute GetAttribute<TAttribute>(this Assembly assembly) where TAttribute : Attribute =>
			(TAttribute)Attribute.GetCustomAttribute(assembly, typeof(TAttribute));

		private static string GetAppSettings([CallerMemberName] string key = null) =>
			ConfigurationManager.AppSettings[key];
	}
}