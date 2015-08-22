using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
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

		public ReactiveCommand AddMemberCommand { get; }
		public ReactiveCommand RemoveMemberCommand { get; }
		public ReactiveCommand ClearMemberCommand { get; }

		private void AddMember(string name, bool isLong, bool isSelected)
		{
			if (string.IsNullOrWhiteSpace(name))
				return;

			if (!Members.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
				Members.Add(new MemberViewModel(name, isSelected) { IsLong = isLong });
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

			AddMemberCommand = new ReactiveCommand();
			AddMemberCommand.Subscribe(_ => AddMember(AddMemberName.Value, AddMemberIsLong.Value, AddMemberIsSelected.Value));

			RemoveMemberCommand = new ReactiveCommand();
			RemoveMemberCommand.Subscribe(_ => RemoveMember(RemoveMemberName.Value));

			ClearMemberCommand = Members.ObserveProperty(x => x.Count).Select(x => 0 < x).ToReactiveCommand();
			ClearMemberCommand.Subscribe(_ => ClearMember());

			#endregion

			PopulateMembers();

			// IsAllLong: Original
			IsAllLong = Members
				.ObserveElementProperty(x => x.IsLong)
				.Select(_ => Members.All(x => x.IsLong))
				.ToReactiveProperty();

			// IsAllLong: Alternative
			var membersNotLong = Members
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
			var membersSelected = new List<MemberViewModel>();

			var elementPropertyChanged = Members
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

			var collectionChanged = Members
				.CollectionChangedAsObservable()
				.Do(x =>
				{
					switch (x.Action)
					{
						case NotifyCollectionChangedAction.Add:
						case NotifyCollectionChangedAction.Remove:
						case NotifyCollectionChangedAction.Replace:
							if (x.OldItems != null)
							{
								foreach (var oldItem in x.OldItems)
								{
									var instance = oldItem as MemberViewModel;
									if (instance != null)
									{
										membersSelected.Remove(instance);
									}
								}
							}
							if (x.NewItems != null)
							{
								foreach (var newItem in x.NewItems)
								{
									var instance = newItem as MemberViewModel;
									if ((instance != null) &&
										instance.IsSelected.Value &&
										!membersSelected.Contains(instance))
									{
										membersSelected.Add(instance);
									}
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
	}
}