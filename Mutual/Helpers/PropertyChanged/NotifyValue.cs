using System;
using System.ComponentModel;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Mutual.Helpers
{
	// <summary>
	// правильный вариант использования
	// объявить поле
	// public NotifyValue<int> F { get; set; }
	//
	// инициализировать в конструкторе
	// F = new NotifyValie<int>();
	//
	// добавить подписку для вычисления
	// F1.CombineLatest(F2, (x, y) => x + y).Subscribe(F);
	// </summary>

	public interface IValue
	{
		object Value { get; set; }
	}

	public class NotifyValue<T> : BaseNotify, IObservable<T>, IValue
	{
		private T value;

		public NotifyValue()
		{
		}

		public NotifyValue(T value)
		{
			this.value = value;
		}

		object IValue.Value {
			get { return this.value; }
			set { this.value = (T) value; }
		}

		public T Value {
			get {
				return value;
			}
			set {
				if (Equals(this.value, value))
					return;

				this.value = value;
				OnPropertyChanged("HasValue");
				OnPropertyChanged();
			}
		}

		public bool HasValue => !Equals(value, default(T));


		public void Mute(T value)
		{
			this.value = value;
		}

		public static implicit operator T(NotifyValue<T> value)
		{
			return value.value;
		}

		public IDisposable Subscribe(IObserver<T> observer)
		{
			return this.ToObservable().Merge(Observable.Return(Value)).Subscribe(observer);
		}

		public override string ToString()
		{
			if (Equals(value, null))
				return String.Empty;
			return value.ToString();
		}

		/// <summary>
		/// если внутри NotifyValue лежит список, то метод не даст ни какого результата
		/// </summary>
		public void Refresh()
		{
			OnPropertyChanged("Value");
		}
	}
}