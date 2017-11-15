using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

namespace WlanProfileViewer.Common
{
	public abstract class DisposableBase : BindableBase
	{
		protected CompositeDisposable Subscription => _subscription.Value;
		private readonly Lazy<CompositeDisposable> _subscription = new Lazy<CompositeDisposable>(() => new CompositeDisposable());

		#region Dispose

		private bool _disposed = false;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (_disposed)
				return;

			if (disposing)
			{
				if (_subscription.IsValueCreated)
					_subscription.Value.Dispose();
			}

			_disposed = true;
		}

		#endregion
	}
}