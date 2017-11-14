using System;
using System.Diagnostics;
using System.Reactive.Linq;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace ReactivePropertyTest
{
	public class MemberViewModel : BindableBase
	{
		public string Name { get; }

		public bool IsLong
		{
			get { return _isLong; }
			set { SetPropertyValue(ref _isLong, value); }
		}
		private bool _isLong;

		public ReactiveProperty<bool> IsSelected { get; }

		#region Test

		public bool IsSelectedValue
		{
			get { return _isSelectedValue; }
			set { SetPropertyValue(ref _isSelectedValue, value); }
		}
		private bool _isSelectedValue;

		public IObservable<bool> IsSelectedCopy { get; }

		#endregion

		public MemberViewModel(string name, bool isSelected = false)
		{
			this.Name = name;

			IsSelected = new ReactiveProperty<bool>(isSelected);

			#region Test

			IsSelectedCopy = this.ObserveProperty(x => x.IsSelectedValue);
			IsSelectedCopy.Subscribe(x => Debug.WriteLine($"IsSelectedCopy {x}"));

			IsSelected.Subscribe(x => IsSelectedValue = x);

			#endregion
		}
	}
}