﻿using System;
using System.Diagnostics;
using System.Reactive.Linq;
using Reactive.Bindings;

namespace ReactivePropertyTest;

public class MemberViewModel : BindableBase
{
	public string Name { get; }

	public bool IsLong
	{
		get => _isLong;
		set => SetProperty(ref _isLong, value);
	}
	private bool _isLong;

	public ReactivePropertySlim<bool> IsSelected { get; }

	public MemberViewModel(string name, bool isSelected = false)
	{
		this.Name = name ?? throw new ArgumentNullException(nameof(name));

		IsSelected = new ReactivePropertySlim<bool>(isSelected);
		IsSelected.Subscribe(x => Debug.WriteLine($"IsSelected: {x}"));
	}
}