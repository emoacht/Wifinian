using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ManagedNativeWifi;

namespace WlanProfileViewer.Models.Wlan
{
	public class NativeWifiProfileItem : ProfileItem
	{
		public ProfileType ProfileType { get; }

		private readonly ProfileDocument _document;

		public override AuthenticationMethod Authentication => _document.Authentication;
		public override EncryptionType Encryption => _document.Encryption;

		public override bool IsAutoConnectionEnabled
		{
			get => _document.IsAutoConnectionEnabled;
			set
			{
				if (_document == null)
					return;

				_document.IsAutoConnectionEnabled = value;
				RaisePropertyChanged();
			}
		}

		public override bool IsAutoSwitchEnabled
		{
			get => _document.IsAutoSwitchEnabled;
			set
			{
				if (_document == null)
					return;

				_document.IsAutoSwitchEnabled = value;
				RaisePropertyChanged();
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
			int signal,
			bool isConnected) : base(
				name: name,
				interfaceId: interfaceId,
				interfaceName: null,
				interfaceDescription: interfaceDescription,
				authentication: default(AuthenticationMethod),
				encryption: default(EncryptionType),
				isAutoConnectionEnabled: false,
				isAutoSwitchEnabled: false,
				position: position,
				signal: signal,
				isConnected: isConnected)
		{
			if (document == null)
				throw new ArgumentNullException(nameof(document));

			this.ProfileType = profileType;
			this._document = document;
		}

		#endregion
	}
}