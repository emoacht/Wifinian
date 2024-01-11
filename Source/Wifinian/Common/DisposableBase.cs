using System;
using System.Reactive.Disposables;

namespace Wifinian.Common;

public abstract class DisposableBase : BindableBase, IDisposable
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