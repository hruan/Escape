using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Escape.Data;

namespace Escape.Studies.Manager {
	public interface IAppointmentRepository : IReadOnlyCollection<IAppointmentMessage> {
		void Create(IAppointmentMessage msg);
		void Pending(IAppointmentMessage msg);
		bool Reject(IAppointmentMessage msg);
		bool Accept(IAppointmentMessage msg);
		bool Conflict(IAppointmentMessage msg);
		void Remove(IAppointmentMessage msg);
		IAppointmentMessage GetItem(AppointmentRequest appointment);
		IAppointmentMessage AppointmentByResourceId(string resource);
	}

	public class AppointmentRepository : IAppointmentRepository, IObservable<IAppointmentMessage> {
		private ImmutableDictionary<string, IAppointmentMessage> _messages = ImmutableDictionary.Create<string, IAppointmentMessage>();
		private ImmutableList<IObserver<IAppointmentMessage>> _observers = ImmutableList.Create<IObserver<IAppointmentMessage>>();

		public IDisposable Subscribe(IObserver<IAppointmentMessage> observer) {
			_observers = _observers.Add(observer);
			return new Subscription<IObserver<IAppointmentMessage>>(observer, Unsubscribe);
		}

		public void Create(IAppointmentMessage msg) {
			Console.WriteLine("Manager: Adding: {0}", msg.Appointment.Identifier);
			_messages = _messages.Add(msg.Appointment.Identifier, msg);
			Notify(msg);
		}

		public void Pending(IAppointmentMessage msg) {
			Console.WriteLine("Manager: Pending: {0}", msg.Appointment.Identifier);
			var m = GetItem(msg.Appointment);
			if (m == null) {
				Console.WriteLine("Manager: Pending: {0} missing", msg.Appointment.Identifier);
				return;
			}

			m.State = State.Pending;
		}

		public bool Reject(IAppointmentMessage msg) {
			var m = GetItem(msg.Appointment);
			if (m == null) {
				Console.WriteLine("Manager: Reject: {0} missing", msg.Appointment.Identifier);
				return false;
			}

			Console.WriteLine("Manager: Rejecting: {0}", msg.Appointment.Identifier);
			_messages = _messages.Remove(m.Appointment.Identifier);

			Notify(msg);
			return true;
		}

		public bool Accept(IAppointmentMessage msg) {
			var m = GetItem(msg.Appointment);
			if (m == null) {
				Console.WriteLine("Manager: Accept: {0} missing", msg.Appointment.Identifier);
				return false;
			}

			Console.WriteLine("Manager: Accepting: {0}", msg.Appointment.Identifier);
			_messages = _messages.Remove(m.Appointment.Identifier);

			Notify(msg);
			return true;
		}

		public bool Conflict(IAppointmentMessage msg) {
			var m = GetItem(msg.Appointment);
			if (m == null) {
				Console.WriteLine("Manager: Conflict: {0} missing", msg.Appointment.Identifier);
				return false;
			}

			Console.WriteLine("Manager: Conflict: {0}", msg.Appointment.Identifier);
			m.State = State.Conflict;

			Notify(msg);
			return true;
		}

		private void Unsubscribe(IObserver<IAppointmentMessage> observer) {
			_observers = _observers.Remove(observer);
		}

		private class Subscription<T> : IDisposable {
			private readonly Action<T> _unsubscribe;
			private readonly T _observer;

			internal Subscription(T observer, Action<T> unsubscribe) {
				_observer = observer;
				_unsubscribe = unsubscribe;
			}

			public void Dispose() {
				_unsubscribe(_observer);
			}
		}

		private void Notify(IAppointmentMessage msg) {
			foreach (var o in _observers) {
				var dispatcher = o as IDispatcher<IAppointmentMessage>;
				if (dispatcher != null && (dispatcher.StateFilter & msg.State) != 0) {
					o.OnNext(msg);
				}
			}
		}

		public IAppointmentMessage GetItem(AppointmentRequest appointment) {
			return _messages.ContainsKey(appointment.Identifier) ? _messages[appointment.Identifier] : null;
		}

		public IAppointmentMessage AppointmentByResourceId(string s) {
			return _messages.ContainsKey(s) ? _messages[s] : null;
		}

		public IEnumerator<IAppointmentMessage> GetEnumerator() {
			return _messages.Values.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		internal void Clear() {
			_messages = _messages.Clear();
		}

		public void Remove(IAppointmentMessage item) {
			_messages = _messages.Remove(item.Appointment.Identifier);
		}

		public int Count { get { return _messages.Count; } }
	}
}