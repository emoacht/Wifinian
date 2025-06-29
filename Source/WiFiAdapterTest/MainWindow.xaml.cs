using System;
using System.Windows;

using WiFiAdapterTest.ViewModels;

namespace WiFiAdapterTest;

public partial class MainWindow : Window
{
	internal MainWindowViewModel ViewModel => (MainWindowViewModel)this.DataContext;

	public MainWindow()
	{
		InitializeComponent();
	}

	protected override async void OnSourceInitialized(EventArgs e)
	{
		base.OnSourceInitialized(e);

		await ViewModel.InitializeAsync();
	}

	private async void Update_Click(object sender, RoutedEventArgs e)
	{
		await ViewModel.UpdateNetworksAsync(true);
	}

	private async void Connect_Click(object sender, RoutedEventArgs e)
	{
		await ViewModel.ConnectNetworkAsync();
	}

	private void Disconnect_Click(object sender, RoutedEventArgs e)
	{
		ViewModel.DisconnectNetwork();
	}
}