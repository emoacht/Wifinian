using System;
using System.Diagnostics;
using System.Reactive.Linq;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace ReactivePropertyTest
{
	public class MemberViewModel : BindableBase
	{
		public string Name { get; private set; }

		public bool IsLong
		{
			get { return _isLong; }
			set { SetProperty(ref _isLong, value); }
		}
		private bool _isLong;

		public ReactiveProperty<bool> IsSelected { get; set; }

		public bool IsSelectedValue
		{
			get { return _isSelectedValue; }
			set { SetProperty(ref _isSelectedValue, value); }
		}
		private bool _isSelectedValue;

		public IObservable<bool> IsSelectedCopy { get; set; }

		public MemberViewModel(string name, bool isSelected = false)
		{
			this.Name = name;

			IsSelectedCopy = this.ObserveProperty(x => x.IsSelectedValue);
			IsSelectedCopy.Subscribe(x => Debug.WriteLine($"IsSelectedCopy {x}"));

			IsSelected = new ReactiveProperty<bool>(isSelected);
			IsSelected.Subscribe(x => IsSelectedValue = x);
		}
	}
}