using System;
using static System.Math;
using ManagedNativeWifi;

using Wifinian.Common;

namespace Wifinian.Models.Wlan;

public class ProfileItem : BindableBase
{
	/// <summary>
	/// Profile ID
	/// </summary>
	public string Id => _id ??= Name + InterfaceId.ToString();
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
	/// Authentication method in string
	/// </summary>
	public virtual string AuthenticationString => Authentication.ToString();

	/// <summary>
	/// Encryption type (TKIP, AES ...) of associated wireless LAN
	/// </summary>
	public virtual EncryptionType Encryption { get; }

	/// <summary>
	/// Whether options for this profile can be set
	/// </summary>
	public virtual bool CanSetOptions => true;

	/// <summary>
	/// Whether automatic connection for this profile is enabled
	/// </summary>
	public virtual bool IsAutoConnectEnabled
	{
		get => _isAutoConnectEnabled;
		set => SetProperty(ref _isAutoConnectEnabled, value);
	}
	private bool _isAutoConnectEnabled;

	/// <summary>
	/// Whether automatic switch for this profile is enabled
	/// </summary>
	public virtual bool IsAutoSwitchEnabled
	{
		get => _isAutoSwitchEnabled;
		set => SetProperty(ref _isAutoSwitchEnabled, value);
	}
	private bool _isAutoSwitchEnabled;

	/// <summary>
	/// Position of this profile in preference order
	/// </summary>
	public int Position
	{
		get => _position;
		set => SetProperty(ref _position, value, normalize: v => Max(v, 0));
	}
	private int _position;

	/// <summary>
	/// Count of positions in preference order
	/// </summary>
	public int PositionCount
	{
		get => _positionCount;
		set => SetProperty(ref _positionCount, value);
	}
	private int _positionCount;

	/// <summary>
	/// Whether the radio of associated wireless interface is on
	/// </summary>
	public bool IsRadioOn
	{
		get => _isRadioOn;
		set => SetProperty(ref _isRadioOn, value);
	}
	private bool _isRadioOn;

	/// <summary>
	/// Whether associated wireless interface is connected to associated wireless LAN
	/// </summary>
	public bool IsConnected
	{
		get => _isConnected;
		set => SetProperty(ref _isConnected, value);
	}
	private bool _isConnected;

	/// <summary>
	/// Protocol of associated wireless LAN
	/// </summary>
	public string Protocol
	{
		get => _protocol;
		set => SetProperty(ref _protocol, value);
	}
	private string _protocol;

	/// <summary>
	/// Signal quality (0-100) of associated wireless LAN
	/// </summary>
	public int Signal
	{
		get => _signal;
		set => SetProperty(ref _signal, value, 0, 100);
	}
	private int _signal;

	/// <summary>
	/// Frequency band (GHz) of associated wireless LAN
	/// </summary>
	public float Band
	{
		get => _band;
		set => SetProperty(ref _band, value);
	}
	private float _band;

	/// <summary>
	/// Channel of associated wireless LAN
	/// </summary>
	public int Channel
	{
		get => _channel;
		set => SetProperty(ref _channel, value);
	}
	private int _channel;

	/// <summary>
	/// Whether this profile is set to be target
	/// </summary>
	public bool IsTarget { get; set; }

	/// <summary>
	/// Last time when fails to connect to wireless LAN
	/// </summary>
	public DateTime LastFailureTime { get; set; }

	#region Constructor

	public ProfileItem(
		string name,
		Guid interfaceId,
		string interfaceName,
		string interfaceDescription,
		AuthenticationMethod authentication,
		EncryptionType encryption,
		bool isAutoConnectEnabled,
		bool isAutoSwitchEnabled,
		int position,
		bool isRadioOn,
		bool isConnected,
		string protocol,
		int signal,
		float band,
		int channel)
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
		this.IsAutoConnectEnabled = isAutoConnectEnabled;
		this.IsAutoSwitchEnabled = isAutoSwitchEnabled;
		this.Position = position;
		this.IsRadioOn = isRadioOn;
		this.IsConnected = isConnected;
		this.Protocol = protocol;
		this.Signal = signal;
		this.Band = band;
		this.Channel = channel;
	}

	#endregion

	/// <summary>
	/// Copies changeable values from other instance.
	/// </summary>
	/// <param name="other">Other instance</param>
	public virtual void Copy(ProfileItem other)
	{
		if ((this.IsConnected != other.IsConnected) ||
			(this.Signal != other.Signal))
		{
			LastFailureTime = default;
		}

		this.IsAutoConnectEnabled = other.IsAutoConnectEnabled;
		this.IsAutoSwitchEnabled = other.IsAutoSwitchEnabled;
		this.Position = other.Position;
		this.IsRadioOn = other.IsRadioOn;
		this.IsConnected = other.IsConnected;
		this.Protocol = other.Protocol;
		this.Signal = other.Signal;
		this.Band = other.Band;
		this.Channel = other.Channel;
	}
}