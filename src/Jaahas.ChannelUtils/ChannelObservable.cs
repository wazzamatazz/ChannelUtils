using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Jaahas.ChannelUtils {

    /// <summary>
    /// Wraps a <see cref="ChannelReader{T}"/> in an <see cref="IObservable{T}"/>.
    /// </summary>
    /// <typeparam name="T">
    ///   The channel item type.
    /// </typeparam>
    public sealed class ChannelObservable<T> : IObservable<T>, IDisposable {

        /// <summary>
        /// Indicates if the observable has been disposed.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// The underlying channel reader.
        /// </summary>
        private readonly ChannelReader<T> _channel;

        /// <summary>
        /// Fires when the <see cref="ChannelObservable{T}"/> is being disposed.
        /// </summary>
        private readonly CancellationTokenSource _disposedTokenSource = new CancellationTokenSource();

        /// <summary>
        /// The current subscriptions.
        /// </summary>
        private readonly HashSet<ChannelObservableSubscription<T>> _subscriptions = new HashSet<ChannelObservableSubscription<T>>();

        /// <summary>
        /// Lock for accessing <see cref="_subscriptions"/>.
        /// </summary>
        private readonly ReaderWriterLockSlim _subscriptionsLock = new ReaderWriterLockSlim();


        /// <summary>
        /// Creates a new <see cref="ChannelObservable{T}"/> object.
        /// </summary>
        /// <param name="channel">
        ///   The channel reader to wrap.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null"/>.
        /// </exception>
        public ChannelObservable(ChannelReader<T> channel) {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            _ = Task.Run(async () => { 
                try {
                    while (await _channel.WaitToReadAsync(_disposedTokenSource.Token).ConfigureAwait(false)) {
                        if (!_channel.TryRead(out var item)) {
                            continue;
                        }

                        OnNext(item);
                    }
                }
                catch (OperationCanceledException) { }
                catch (ChannelClosedException) { }
                catch (Exception e) {
                    OnError(e);
                }
                finally {
                    OnCompleted();
                }
            });
        }


        /// <inheritdoc/>
        public IDisposable Subscribe(IObserver<T> observer) {
            if (_disposed) {
                var result = new ChannelObservableSubscription<T>(observer, null);
                ((IObserver<T>) result).OnCompleted();
                return result;
            }

            if (observer == null) {
                throw new ArgumentNullException(nameof(observer));
            }

            _subscriptionsLock.EnterWriteLock();
            try {
                var result = new ChannelObservableSubscription<T>(observer, Unsubscribe);
                _subscriptions.Add(result);

                return result;
            }
            finally {
                _subscriptionsLock.ExitWriteLock();
            }
        }


        /// <summary>
        /// Provides all observers with new data.
        /// </summary>
        /// <param name="item">
        ///   The published value.
        /// </param>
        private void OnNext(T item) {
            ChannelObservableSubscription<T>[] subscriptions;

            _subscriptionsLock.EnterReadLock();
            try {
                subscriptions = _subscriptions.ToArray();
            }
            finally {
                _subscriptionsLock.ExitReadLock();
            }

            foreach (var subscription in subscriptions) {
                ((IObserver<T>) subscription).OnNext(item);
            }
        }


        /// <summary>
        /// Notifies all observers that the provider has experienced an error condition.
        /// </summary>
        /// <param name="error">
        ///   The error.
        /// </param>
        private void OnError(Exception error) {
            ChannelObservableSubscription<T>[] subscriptions;

            _subscriptionsLock.EnterReadLock();
            try {
                subscriptions = _subscriptions.ToArray();
            }
            finally {
                _subscriptionsLock.ExitReadLock();
            }

            foreach (var subscription in subscriptions) {
                ((IObserver<T>) subscription).OnError(error);
            }
        }


        /// <summary>
        /// Notifies all observers that the provider has finished sending notifications.
        /// </summary>
        private void OnCompleted() {
            ChannelObservableSubscription<T>[] subscriptions;

            _subscriptionsLock.EnterReadLock();
            try {
                subscriptions = _subscriptions.ToArray();
            }
            finally {
                _subscriptionsLock.ExitReadLock();
            }

            foreach (var subscription in subscriptions) {
                ((IObserver<T>) subscription).OnCompleted();
            }
        }


        /// <summary>
        /// Removes the specified subscription from the list of current observers.
        /// </summary>
        /// <param name="subscription">
        ///   The subscription.
        /// </param>
        private void Unsubscribe(ChannelObservableSubscription<T> subscription) {
            if (_disposed) {
                return;
            }

            _subscriptionsLock.EnterWriteLock();
            try {
                _subscriptions.Remove(subscription);
            }
            finally {
                _subscriptionsLock.ExitWriteLock();
            }
        }


        /// <summary>
        /// Disposes of the <see cref="ChannelObservable{T}"/>.
        /// </summary>
        public void Dispose() {
            if (_disposed) {
                return;
            }

            _disposed = true;

            _disposedTokenSource.Cancel();

            _subscriptionsLock.EnterWriteLock();
            try {
                foreach (var subscription in _subscriptions.ToArray()) {
                    ((IObserver<T>) subscription).OnCompleted();
                }
            }
            finally {
                _subscriptions.Clear();
                _subscriptionsLock.ExitWriteLock();
            }

            _subscriptionsLock.Dispose();
        }
    }
}
