using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Wifinian.Models
{
	internal class StartupService
	{
		private const string Argument = "/startup";

		/// <summary>
		/// Whether this instance is presumed to have started on sign in
		/// </summary>
		/// <returns>True if started on sign in</returns>
		public static bool IsStartedOnSignIn()
		{
			// Check the command-line arguments.
			return Environment.GetCommandLineArgs().Skip(1).Contains(Argument);
		}

		/// <summary>
		/// Whether this instance can be registered in startup
		/// </summary>
		/// <returns>True if can be registered</returns>
		public static bool CanRegister() => true;

		/// <summary>
		/// Whether this instance is registered in startup
		/// </summary>
		/// <returns>True if registered</returns>
		public static bool IsRegistered() => RegistryIsAdded();

		/// <summary>
		/// Registers this instance to startup.
		/// </summary>
		public static void Register() => RegistryAdd();

		/// <summary>
		/// Unregisters this instance from startup.
		/// </summary>
		public static void Unregister() => RegistryRemove();

		#region Registry

		private const string Run = @"Software\Microsoft\Windows\CurrentVersion\Run";
		private static readonly string _path = $"{Assembly.GetExecutingAssembly().Location} {Argument}";

		private static bool RegistryIsAdded()
		{
			using (var key = Registry.CurrentUser.OpenSubKey(Run, false))
			{
				var existingValue = key.GetValue(ProductInfo.Title) as string;
				return string.Equals(existingValue, _path, StringComparison.OrdinalIgnoreCase);
			}
		}

		private static bool RegistryAdd()
		{
			if (RegistryIsAdded())
				return true;

			using (var key = Registry.CurrentUser.OpenSubKey(Run, true))
			{
				key.SetValue(ProductInfo.Title, _path, RegistryValueKind.String);
			}
			return true;
		}

		private static void RegistryRemove()
		{
			using (var key = Registry.CurrentUser.OpenSubKey(Run, true))
			{
				if (!key.GetValueNames().Contains(ProductInfo.Title)) // The content of value doesn't matter.
					return;

				key.DeleteValue(ProductInfo.Title, false);
			}
		}

		#endregion
	}
}
