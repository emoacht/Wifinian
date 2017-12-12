using System;
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

		protected virtual bool SetPropertyValue<T>(ref T storage, T value, T min, T max, [CallerMemberName] string propertyName = null)
			where T : struct, IComparable<T>
		{
			if (0 <= min.CompareTo(max))
				throw new ArgumentException($"{nameof(min)} must be smaller than {nameof(max)}.");

			var buff = (value.CompareTo(min) <= 0)
				? min
				: (0 <= value.CompareTo(max))
					? max
					: value;

			return SetPropertyValue(ref storage, buff, propertyName);
		}

		protected virtual bool SetPropertyValue<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
		{
			if (EqualityComparer<T>.Default.Equals(storage, value))
				return false;

			storage = value;
			RaisePropertyChanged(propertyName);
			return true;
		}

		protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null) =>
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}