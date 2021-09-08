using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using ScreenFrame.Movers;
using Wifinian.ViewModels;

namespace Wifinian.Views
{
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
			_mover.EscapeKeyDown += OnCloseTriggered;

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
}