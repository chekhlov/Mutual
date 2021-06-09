using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive.Linq;

namespace Mutual.Helpers.Collections
{
	public static class ObservableCollectionEx
	{
		public static IDisposable Subscribe<T>(this ObservableCollection<T> collection,  IObserver<NotifyCollectionChangedEventArgs> observer)
		{
			// Создаем наблюдателя из события
			var observable = Observable.FromEvent<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
				a => new NotifyCollectionChangedEventHandler(new Action<object, NotifyCollectionChangedEventArgs>((s, e) => a(e))),			
				handler  => collection.CollectionChanged += handler,
				handler  => collection.CollectionChanged -= handler);
			return observable.Subscribe(observer);
		}		
	}
}