using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using ManagedNativeWifi;

namespace Wifinian.Models.Wlan
{
	public class NativeWifiProfileItem : ProfileItem
	{
		public ProfileType ProfileType { get; }

		private ProfileDocument _document;

		public BssType BssType => _document.BssType;

		public override string AuthenticationString
		{
			get
			{
				if (Authentication == default)
				{
					var match = Regex.Match(_document.Xml, @"<authentication>(?<value>.+)</authentication>");
					if (match.Success)
						return match.Groups["value"].Value;
				}
				return base.AuthenticationString;
			}
		}

		public override bool CanSetOptions =>
			(ProfileType != ProfileType.GroupPolicy) && (BssType == BssType.Infrastructure);

		public override bool IsAutoConnectEnabled
		{
			get => _document.IsAutoConnectEnabled;
			set
			{
				if (_document is null) // The base class's constructor may access before _document is filled.
					return;

				SetPropertyValue((get: () => _document.IsAutoConnectEnabled, set: v => _document.IsAutoConnectEnabled = v), value);
			}
		}

		public override bool IsAutoSwitchEnabled
		{
			get => _document.IsAutoSwitchEnabled;
			set
			{
				if (_document is null) // The base class's constructor may access before _document is filled.
					return;

				SetPropertyValue((get: () => _document.IsAutoSwitchEnabled, set: v => _document.IsAutoSwitchEnabled = v), value);
			}
		}

		public string Xml => _document.Xml;

		#region Constructor

		public NativeWifiProfileItem(
			string name,
			Guid interfaceId,
			string interfaceDescription,
			ProfileType profileType,
			ProfileDocument document,
			int position,
			bool isRadioOn,
			bool isConnected,
			string protocol,
			int signal,
			float band,
			int channel) : base(
				name: name,
				interfaceId: interfaceId,
				interfaceName: null,
				interfaceDescription: interfaceDescription,
				authentication: document.Authentication,
				encryption: document.Encryption,
				isAutoConnectEnabled: false,
				isAutoSwitchEnabled: false,
				position: position,
				isRadioOn: isRadioOn,
				isConnected: isConnected,
				protocol: protocol,
				signal: signal,
				band: band,
				channel: channel)
		{
			this.ProfileType = profileType;
			this._document = document ?? throw new ArgumentNullException(nameof(document));
		}

		#endregion

		public override void Copy(ProfileItem other)
		{
			base.Copy(other);

			if (other is NativeWifiProfileItem item)
			{
				this._document = item._document;
			}
		}
	}
}