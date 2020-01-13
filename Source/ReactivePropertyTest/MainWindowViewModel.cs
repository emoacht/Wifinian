using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Helpers;

namespace ReactivePropertyTest
{
	public class MainWindowViewModel : BindableBase
	{
		#region Test

		public ReactiveProperty<string> AddMemberName { get; }
		public ReactiveProperty<bool> AddMemberIsLong { get; }
		public ReactiveProperty<bool> AddMemberIsSelected { get; }
		public ReactiveProperty<string> RemoveMemberName { get; }

		public AsyncReactiveCommand AddMemberCommand { get; }
		public ReactiveCommand RemoveMemberCommand { get; }
		public ReactiveCommand ClearMemberCommand { get; }

		private async Task AddMember(string name, bool isLong, bool isSelected)
		{
			if (string.IsNullOrWhiteSpace(name))
				return;

			if (!Members.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
				Members.Add(new MemberViewModel(name, isSelected) { IsLong = isLong });
			
			await Task.Delay(TimeSpan.FromSeconds(1));
		}

		private void RemoveMember(string name)
		{
			if (string.IsNullOrWhiteSpace(name))
				return;

			var member = Members.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
			if (member != null)
				Members.Remove(member);
		}

		private void ClearMember()
		{
			Members.Clear();
		}

		#endregion

		public ObservableCollection<MemberViewModel> Members { get; } = new ObservableCollection<MemberViewModel>();

		public ReactiveProperty<bool> IsAllLong { get; }
		public ReactiveProperty<bool> IsAnySelected { get; }
		
		public MainWindowViewModel()
		{
			#region Test

			AddMemberName = new ReactiveProperty<string>(string.Empty);
			AddMemberIsLong = new ReactiveProperty<bool>(false);
			AddMemberIsSelected = new ReactiveProperty<bool>(false);
			RemoveMemberName = new ReactiveProperty<string>(string.Empty);

			AddMemberCommand = new AsyncReactiveCommand();
			AddMemberCommand.Subscribe(_ => AddMember(AddMemberName.Value, AddMemberIsLong.Value, AddMemberIsSelected.Value));

			RemoveMemberCommand = new ReactiveCommand();
			RemoveMemberCommand.Subscribe(_ => RemoveMember(RemoveMemberName.Value));

			ClearMemberCommand = Members.ObserveProperty(x => x.Count).Select(x => 0 < x).ToReactiveCommand();
			ClearMemberCommand.Subscribe(_ => ClearMember());

			#endregion

			PopulateMembers();

			Members
				.ObserveElementProperty(x => x.IsLong)
				.Where(x => x.Value)
				.Subscribe(x => ShowName(x.Instance));

			Members
				.ObserveElementObservableProperty(x => x.IsSelected)
				.Where(x => x.Value)
				.Subscribe(x => ShowName(x.Instance));

			// IsAllLong: Original
			IsAllLong = Members
				.ObserveElementProperty(x => x.IsLong)
				.Select(_ => Members.All(x => x.IsLong))
				.ToReactiveProperty();

			// IsAllLong: Alternative
			IFilteredReadOnlyObservableCollection<MemberViewModel> membersNotLong = Members
				.ToFilteredReadOnlyObservableCollection(x => !x.IsLong);

			IsAllLong = membersNotLong
				.CollectionChangedAsObservable()
				.Select(_ => Members.Any() && (0 == membersNotLong.Count))
				.ToReactiveProperty();

			// IsAnySelected: Original
			IsAnySelected = Members
				.ObserveElementObservableProperty(x => x.IsSelected)
				.Select(_ => Members.Any(x => x.IsSelected.Value))
				.ToReactiveProperty();

			// IsAnySelected: Alternative 1
			List<MemberViewModel> membersSelected = new List<MemberViewModel>();

			IObservable<bool> elementPropertyChanged = Members
				.ObserveElementObservableProperty(x => x.IsSelected)
				.Do(x =>
				{
					if (!x.Value)
					{
						membersSelected.Remove(x.Instance);
					}
					else if (!membersSelected.Contains(x.Instance))
					{
						membersSelected.Add(x.Instance);
					}
				})
				.Select(_ => 0 < membersSelected.Count);

			IObservable<bool> collectionChanged = Members
				.CollectionChangedAsObservable()
				.Where(x => x.Action != NotifyCollectionChangedAction.Move)
				.Do(x =>
				{
					switch (x.Action)
					{
						case NotifyCollectionChangedAction.Add:
						case NotifyCollectionChangedAction.Remove:
						case NotifyCollectionChangedAction.Replace:
							if (x.OldItems != null)
							{
								foreach (var instance in x.OldItems.Cast<MemberViewModel>())
								{
									membersSelected.Remove(instance);
								}
							}
							if (x.NewItems != null)
							{
								foreach (var instance in x.NewItems.Cast<MemberViewModel>())
								{
									if (membersSelected.Contains(instance))
										continue;

									if (instance.IsSelected.Value)
										membersSelected.Add(instance);
								}
							}
							break;
						case NotifyCollectionChangedAction.Reset:
							membersSelected.Clear();
							break;
					}
				})
				.Select(_ => 0 < membersSelected.Count);

			IsAnySelected = Observable.Merge(elementPropertyChanged, collectionChanged)
				.ToReactiveProperty();

			// IsAnySelected: Alternative 2
			IsAnySelected = Members
				.ObserveElementBooleanObservableProperty(x => x.IsSelected)
				.Select(x => 0 < x.Count)
				.ToReactiveProperty();

			// IsAnySelected: Alternative 3
			IsAnySelected = Members
				.ObserveElementFilteredObservableProperty(x => x.IsSelected, x => x)
				.Select(x => 0 < x.Count)
				.ToReactiveProperty();
		}

		private void PopulateMembers()
		{
			Members.Add(new MemberViewModel("Agano") { IsLong = true });
			Members.Add(new MemberViewModel("Noshiro") { IsLong = true });
			Members.Add(new MemberViewModel("Yahagi") { IsLong = true });
			Members.Add(new MemberViewModel("Sakawa") { IsLong = false });
		}

		private void ShowName(MemberViewModel member)
		{
			Debug.WriteLine($"{member.Name} is changed.");
		}
	}
}