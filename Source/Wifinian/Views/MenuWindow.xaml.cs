using System;
using System.ComponentModel;
using System.Windows;

using ScreenFrame.Movers;
using Wifinian.ViewModels;

namespace Wifinian.Views;

public partial class MenuWindow : Window
{
	private readonly FloatWindowMover _mover;
	private MenuWindowViewModel ViewModel => (MenuWindowViewModel)this.DataContext;

	internal MenuWindow(AppController controller, Point pivot)
	{
		InitializeComponent();

		this.DataContext = new MenuWindowViewModel(controller);

		_mover = new FloatWindowMover(this, pivot);
		_mover.AppDeactivated += OnCloseTriggered;

		controller.WindowPainter.Add(this);
	}

	#region Close

	private bool _isClosing = false;

	private void OnCloseTriggered(object sender, EventArgs e)
	{
		if (!_isClosing && this.IsLoaded)
			this.Close();
	}

	protected override void OnDeactivated(EventArgs e)
	{
		base.OnDeactivated(e);

		if (!_isClosing)
			this.Close();
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		if (!e.Cancel)
		{
			_isClosing = true;
			ViewModel.Dispose();
		}

		base.OnClosing(e);
	}

	#endregion
}