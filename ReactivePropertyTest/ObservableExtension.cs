using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace ReactivePropertyTest
{
	public static class ObservableExtension
	{
		public static IObservable<IList<TElement>> ObserveElementBooleanObservableProperty<TElement>(
			this ObservableCollection<TElement> source,
			Expression<Func<TElement, IObservable<bool>>> propertySelector)
			where TElement : class
		{
			var instanceCache = new List<TElement>();
			var propertySelectorDelegate = propertySelector.Compile();

			var elementPropertyChanged = source
				.ObserveElementObservableProperty(propertySelector)
				.Do(x =>
				{
					if (!x.Value)
					{
						instanceCache.Remove(x.Instance);
					}
					else if (!instanceCache.Contains(x.Instance))
					{
						instanceCache.Add(x.Instance);
					}
				})
				.Select(_ => instanceCache);

			var collectionChanged = source
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
								foreach (var instance in x.OldItems.Cast<TElement>())
								{
									instanceCache.Remove(instance);
								}
							}
							if (x.NewItems != null)
							{
								// This route is not really necessary because if the element value of
								// IObservable<bool> is true, ObserveElementObservableProperty method
								// will handle it.

								foreach (var instance in x.NewItems.Cast<TElement>())
								{
									if (instanceCache.Contains(instance))
										continue;

									// If no element is sent from the sequence, nothing will happen.
									propertySelectorDelegate(instance)
										.Where(y => y)
										.Subscribe(_ => instanceCache.Add(instance))
										.Dispose();
								}
							}
							break;
						case NotifyCollectionChangedAction.Reset:
							instanceCache.Clear();
							break;
					}
				})
				.Select(_ => instanceCache);

			return Observable.Merge(elementPropertyChanged, collectionChanged);
		}

		public static IObservable<IList<TElement>> ObserveElementFilteredObservableProperty<TElement, TProperty>(
			this ObservableCollection<TElement> source,
			Expression<Func<TElement, IObservable<TProperty>>> propertySelector,
			Func<TProperty, bool> filter)
			where TElement : class
		{
			var instanceCache = new List<TElement>();
			var propertySelectorDelegate = propertySelector.Compile();

			var elementPropertyChanged = source
				.ObserveElementObservableProperty(propertySelector)
				.Do(x =>
				{
					if (!filter(x.Value))
					{
						instanceCache.Remove(x.Instance);
					}
					else if (!instanceCache.Contains(x.Instance))
					{
						instanceCache.Add(x.Instance);
					}
				})
				.Select(_ => instanceCache);

			var collectionChanged = source
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
								foreach (var instance in x.OldItems.Cast<TElement>())
								{
									instanceCache.Remove(instance);
								}
							}
							if (x.NewItems != null)
							{
								// This route is not really necessary because if the return value of
								// Func<TProperty, bool> is true, ObserveElementObservableProperty method
								// will handle it.

								foreach (var instance in x.NewItems.Cast<TElement>())
								{
									if (instanceCache.Contains(instance))
										continue;

									// If no element is sent from the sequence, nothing will happen.
									propertySelectorDelegate(instance)
										.Where(y => filter(y))
										.Subscribe(_ => instanceCache.Add(instance))
										.Dispose();
								}
							}
							break;
						case NotifyCollectionChangedAction.Reset:
							instanceCache.Clear();
							break;
					}
				})
				.Select(_ => instanceCache);

			return Observable.Merge(elementPropertyChanged, collectionChanged);
		}
	}
}