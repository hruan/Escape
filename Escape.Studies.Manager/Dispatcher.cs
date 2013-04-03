using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Escape.Data;

namespace Escape.Studies.Manager {
	public interface IDispatcher<T> : IObserver<T>, IDisposable {
		State StateFilter { get; }
	}

	public class BlockingQueue<T> : IDisposable {
		private readonly SemaphoreSlim _semaphore;

		private readonly T[] _queue;
		private int _cur, _cap;

		public BlockingQueue(int capacity) {
			_semaphore = new SemaphoreSlim(0, capacity);
			_queue     = new T[capacity];
		}

		public void Enqueue(T item) {
			int prev;
			do {
				prev = _cap;
				if (prev - _cur >= _queue.Length) throw new InvalidOperationException("Queue is full");
			} while (prev != Interlocked.CompareExchange(ref _cap, prev + 1, prev));

			_queue[prev % _queue.Length] = item;
			_semaphore.Release();
		}

		public async Task<T> DequeueAsync(CancellationToken ct) {
			await _semaphore.WaitAsync(ct);

			int prev;
			T   item;
			do {
				prev = _cur;
				item = _queue[prev % _queue.Length];
			} while (prev != Interlocked.CompareExchange(ref _cur, prev + 1, prev));

			return item;
		}

		public void Dispose() {
			if (_semaphore != null) _semaphore.Dispose();
		}
	}

	public class Dispatcher<T>  : IDispatcher<T> where T : IAppointmentMessage {
		private readonly BlockingQueue<T>   _queue;
		private readonly IDispatchClient<T> _client;
		private readonly Action<T>          _completionFunc;

		public State StateFilter { get; private set; }

		public Dispatcher(Action<T> completionFunc, IDispatchClient<T> client, State filter, CancellationToken ct) {
			_queue          = new BlockingQueue<T>(64);
			_completionFunc = completionFunc;
			_client         = client;
			StateFilter     = filter;

			Flush(ct);
		}

		public void OnNext(T value) {
			_queue.Enqueue(value);
		}

		public void OnError(Exception error) {
			throw new NotImplementedException();
		}

		public void OnCompleted() {
			throw new NotImplementedException();
		}

		public void Dispose() {
			_queue.Dispose();
		}

		private async void Flush(CancellationToken ct) {
			while (true) {
				try {
					var item = await _queue.DequeueAsync(ct);
					if (await _client.DispatchAsync(item, ct)) {
						if (_completionFunc != null) {
							_completionFunc(item);
						}
					} else {
							_queue.Enqueue(item);
					}
					ct.ThrowIfCancellationRequested();
				} catch (OperationCanceledException ex) {
					Console.WriteLine("Dispatcher: {0}", ex.Message);
					break;
				}
			}
		}
	}

	public interface IDispatchClient<T> {
		string Url { get; set; }
		Task<bool> DispatchAsync(T request, CancellationToken ct);
	}

	public class HttpDispatcher<T> : IDispatchClient<T> where T : IAppointmentMessage {
		public string Url { get; set; }
		public async Task<bool> DispatchAsync(T request, CancellationToken ct) {
			// Can't serialize an interface; make sure it's an AppointmentMessage
			var item = request as AppointmentMessage;
			if (item == null) return false;

			try {
				var client = new HttpClient();
				var resp = await client.PostAsXmlAsync(Url, item, ct);

				if (resp.IsSuccessStatusCode) return true;
				if (resp.StatusCode == HttpStatusCode.Conflict) {
					Console.WriteLine("Dispatcher: WARN: Dropping conflict appointment");
					return true;
				}

				return false;
			} catch (HttpRequestException ex) {
				Console.Error.WriteLine("Dispatcher: Send error: {0}", ex.Message);
				return false;
			}
		}
	}
}
