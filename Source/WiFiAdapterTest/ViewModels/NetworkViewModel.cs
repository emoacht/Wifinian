using System;
using Windows.Devices.WiFi;
using Windows.Networking.Connectivity;

namespace WiFiAdapterTest.ViewModels;

public class NetworkViewModel : ViewModelBase
{
	public Guid AdapterId { get; }
	public WiFiAvailableNetwork Network { get; }

	public bool IsConnected
	{
		get => _isConnected;
		set => SetProperty(ref _isConnected, value);
	}
	private bool _isConnected;

	public string Ssid => Network.Ssid;
	public string Bssid => Network.Bssid;
	public WiFiNetworkKind NetworkKind => Network.NetworkKind;
	public WiFiPhyKind PhyKind => Network.PhyKind;
	public int Frequency => Network.ChannelCenterFrequencyInKilohertz / 1_000;
	public NetworkAuthenticationType Authentication => Network.SecuritySettings.NetworkAuthenticationType;
	public NetworkEncryptionType Encryption => Network.SecuritySettings.NetworkEncryptionType;
	public double Rssi => Network.NetworkRssiInDecibelMilliwatts;
	public byte SignalBars => Network.SignalBars;

	public NetworkViewModel(Guid adapterId, WiFiAvailableNetwork network, bool isConnected)
	{
		this.AdapterId = adapterId;
		this.Network = network ?? throw new ArgumentNullException(nameof(network));
		this.IsConnected = isConnected;
	}
}