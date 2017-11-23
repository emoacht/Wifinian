using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Math;

using ManagedNativeWifi;
using WlanProfileViewer.Common;

namespace WlanProfileViewer.Models.Wlan
{
	public class ProfileItem : BindableBase
	{
		/// <summary>
		/// Profile ID
		/// </summary>
		public string Id => _id ?? (_id = Name + InterfaceId.ToString());
		private string _id;

		/// <summary>
		/// Profile name
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Wireless interface ID
		/// </summary>
		public Guid InterfaceId { get; }

		/// <summary>
		/// Wireless interface name (only for Netsh)
		/// </summary>
		public string InterfaceName { get; }

		/// <summary>
		/// Wireless interface description (wireless LAN adapter name)
		/// </summary>
		public string InterfaceDescription { get; }

		/// <summary>
		/// Authentication method (WPA, WPA2 ...) of associated wireless LAN
		/// </summary>
		public virtual AuthenticationMethod Authentication { get; }

		/// <summary>
		/// Encryption type (TKIP, AES ...) of associated wireless LAN
		/// </summary>
		public virtual EncryptionType Encryption { get; }

		/// <summary>
		/// Whether automatic connection for this profile is enabled
		/// </summary>
		public virtual bool IsAutoConnectionEnabled
		{
			get => _isAutoConnectionEnabled;
			set => SetPropertyValue(ref _isAutoConnectionEnabled, value);
		}
		private bool _isAutoConnectionEnabled;

		/// <summary>
		/// Whether automatic switch for this profile is enabled
		/// </summary>
		public virtual bool IsAutoSwitchEnabled
		{
			get => _isAutoSwitchEnabled;
			set => SetPropertyValue(ref _isAutoSwitchEnabled, value);
		}
		private bool _isAutoSwitchEnabled;

		/// <summary>
		/// Position of this profile in preference order
		/// </summary>
		public int Position
		{
			get => _position;
			set
			{
				var floored = Max(value, 0);
				SetPropertyValue(ref _position, floored);
			}
		}
		private int _position;

		/// <summary>
		/// Count of positions in preference order
		/// </summary>
		public int PositionCount
		{
			get => _positionCount;
			set => SetPropertyValue(ref _positionCount, value);
		}
		private int _positionCount;

		/// <summary>
		/// Signal quality (0-100) of associated wireless LAN
		/// </summary>
		public int Signal
		{
			get => _signal;
			set
			{
				var clamped = Max(Min(value, 100), 0);
				SetPropertyValue(ref _signal, clamped);
			}
		}
		private int _signal;

		/// <summary>
		/// Whether associated wireless interface is connected to associated wireless LAN
		/// </summary>
		public bool IsConnected
		{
			get => _isConnected;
			set => SetPropertyValue(ref _isConnected, value);
		}
		private bool _isConnected;

		/// <summary>
		/// Whether this profile is set to be target
		/// </summary>
		public bool IsTarget { get; set; }

		#region Constructor

		public ProfileItem(
			string name,
			Guid interfaceId,
			string interfaceName,
			string interfaceDescription,
			AuthenticationMethod authentication,
			EncryptionType encryption,
			bool isAutoConnectionEnabled,
			bool isAutoSwitchEnabled,
			int position,
			int signal,
			bool isConnected)
		{
			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			if (interfaceId == Guid.Empty)
				throw new ArgumentException(nameof(interfaceId));

			this.Name = name;
			this.InterfaceId = interfaceId;
			this.InterfaceName = interfaceName;
			this.InterfaceDescription = interfaceDescription;
			this.Authentication = authentication;
			this.Encryption = encryption;
			this.IsAutoConnectionEnabled = isAutoConnectionEnabled;
			this.IsAutoSwitchEnabled = isAutoSwitchEnabled;
			this.Position = position;
			this.Signal = signal;
			this.IsConnected = isConnected;
		}

		#endregion

		/// <summary>
		/// Copies changeable values from other instance.
		/// </summary>
		/// <param name="other">Other instance</param>
		public virtual void Copy(ProfileItem other)
		{
			this.IsAutoConnectionEnabled = other.IsAutoConnectionEnabled;
			this.IsAutoSwitchEnabled = other.IsAutoSwitchEnabled;
			this.Position = other.Position;
			this.Signal = other.Signal;
			this.IsConnected = other.IsConnected;
		}
	}
}