using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Wifinian.Models
{
	public static class ProductInfo
	{
		public static Version Version { get; } = Assembly.GetExecutingAssembly().GetName().Version;

		public static string Title { get; } = GetAttribute<AssemblyTitleAttribute>(Assembly.GetExecutingAssembly()).Title;

		private static TAttribute GetAttribute<TAttribute>(Assembly assembly) where TAttribute : Attribute =>
			(TAttribute)Attribute.GetCustomAttribute(assembly, typeof(TAttribute));
	}
}