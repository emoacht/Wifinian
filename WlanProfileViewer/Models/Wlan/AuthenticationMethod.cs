
namespace WlanProfileViewer.Models.Wlan
{
	/// <summary>
	/// Authentication method to be used to connect to wireless LAN
	/// </summary>
	/// <remarks>
	/// https://msdn.microsoft.com/en-us/library/windows/desktop/ms706933.aspx
	/// </remarks>
	public enum AuthenticationMethod
	{
		None = 0,

		/// <summary>
		/// Open 802.11 authentication
		/// </summary>
		Open,

		/// <summary>
		/// Shared 802.11 authentication
		/// </summary>
		Shared,

		/// <summary>
		/// WPA-Enterprise 802.11 authentication
		/// </summary>
		/// <remarks>WPA in profile XML</remarks>
		WPA_Enterprise,

		/// <summary>
		/// WPA-Personal 802.11 authentication
		/// </summary>
		/// <remarks>WPAPSK in profile XML</remarks>
		WPA_Personal,

		/// <summary>
		/// WPA2-Enterprise 802.11 authentication
		/// </summary>
		/// <remarks>WPA2 in profile XML</remarks>
		WPA2_Enterprise,

		/// <summary>
		/// WPA2-Personal 802.11 authentication
		/// </summary>
		/// <remarks>WPA2PSK in profile XML</remarks>
		WPA2_Personal
	}
}