
namespace WlanProfileViewer.Models.Wlan
{
	/// <summary>
	/// Data encryption type to be used to connect to wireless LAN
	/// </summary>
	/// <remarks>
	/// https://msdn.microsoft.com/en-us/library/windows/desktop/ms706969.aspx
	/// </remarks>
	public enum EncryptionType
	{
		None = 0,

		/// <summary>
		/// WEP encryption for WEP
		/// </summary>
		WEP,

		/// <summary>
		/// TKIP encryption for WPA/WPA2
		/// </summary>
		TKIP,

		/// <summary>
		/// AES encryption for WPA/WPA2
		/// </summary>
		/// <remarks>CCMP in Netsh</remarks>
		AES
	}
}