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
			get => _isLong;
			set => SetPropertyValue(ref _isLong, value);
		}
		private bool _isLong;

		public ReactivePropertySlim<bool> IsSelected { get; }

		public MemberViewModel(string name, bool isSelected = false)
		{
			this.Name = name;

			IsSelected = new ReactivePropertySlim<bool>(isSelected);
			IsSelected.Subscribe(x => Debug.WriteLine($"IsSelected: {x}"));
		}
	}
}