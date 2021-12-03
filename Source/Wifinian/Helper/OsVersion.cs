﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Wifinian.Helper
{
	/// <summary>
	/// OS version information
	/// </summary>
	internal static class OsVersion
	{
		/// <summary>
		/// Whether OS is Windows 7 (6.1) or greater
		/// </summary>
		public static bool Is7OrGreater => IsEqualToOrGreaterThan(6, 1);

		/// <summary>
		/// Whether OS is Windows 8 (6.2) or greater
		/// </summary>
		public static bool Is8OrGreater => IsEqualToOrGreaterThan(6, 2);

		/// <summary>
		/// Whether OS is Windows 8.1 (6.3) or greater
		/// </summary>
		public static bool Is8Point1OrGreater => IsEqualToOrGreaterThan(6, 3);

		/// <summary>
		/// Whether OS is Windows 10 (10.0.10240) or greater
		/// </summary>
		public static bool Is10OrGreater => IsEqualToOrGreaterThan(10, 0, 10240);

		/// <summary>
		/// Whether OS is Windows 11 (10.0.22000) or greater
		/// </summary>
		public static bool Is11OrGreater => IsEqualToOrGreaterThan(10, 0, 22000);

		#region Cache

		private static readonly Dictionary<string, bool> _cache = new();
		private static readonly object _lock = new();

		private static bool IsEqualToOrGreaterThan(int major, int minor = 0, int build = 0, [CallerMemberName] string propertyName = null)
		{
			lock (_lock)
			{
				if (!_cache.TryGetValue(propertyName, out bool value))
				{
					value = (new Version(major, minor, build) <= Environment.OSVersion.Version);
					_cache[propertyName] = value;
				}
				return value;
			}
		}

		#endregion
	}
}