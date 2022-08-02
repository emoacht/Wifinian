﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Wifinian.Common
{
	public abstract class BindableBase : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual bool SetProperty<T>(ref T storage, T value, T min, T max, [CallerMemberName] string propertyName = null)
			where T : struct, IComparable<T>
		{
			if (0 <= min.CompareTo(max))
				throw new ArgumentException($"{nameof(min)} must be smaller than {nameof(max)}.");

			T normalize(T x) => (x.CompareTo(min) <= 0)
				? min
				: (0 <= x.CompareTo(max))
					? max
					: x;

			return SetProperty(ref storage, value, normalize, propertyName);
		}

		protected virtual bool SetProperty<T>(ref T storage, T value, Func<T, T> normalize, [CallerMemberName] string propertyName = null)
		{
			if (normalize is null)
				throw new ArgumentNullException(nameof(normalize));

			return SetProperty(ref storage, normalize.Invoke(value), propertyName);
		}

		protected virtual bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
		{
			if (EqualityComparer<T>.Default.Equals(storage, value))
				return false;

			storage = value;
			OnPropertyChanged(propertyName);
			return true;
		}

		protected virtual bool SetProperty<T>((Func<T> get, Action<T> set) accessor, T value, [CallerMemberName] string propertyName = null)
		{
			if (EqualityComparer<T>.Default.Equals(accessor.get.Invoke(), value))
				return false;

			accessor.set.Invoke(value);
			OnPropertyChanged(propertyName);
			return true;
		}

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}