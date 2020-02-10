using System;

namespace Jaahas.ChannelUtils {

    /// <summary>
    /// Represents a subscription on a <see cref="ChannelObservable{T}"/>.
    /// </summary>
    /// <typeparam name="T">
    ///   The observable item type.
    /// </typeparam>
    public sealed class ChannelObservableSubscription<T> : IObserver<T>, IDisposable {

        /// <summary>
        /// Indicates if the subscription has been disposed.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// The underlying observer to publish messages to.
        /// </summary>
        private readonly IObserver<T> _observer;

        /// <summary>
        /// Indicates if the observable has notified that it has completed (with or without an 
        /// error condition).
        /// </summary>
        private bool _isCompleted;

        /// <summary>
        /// A callback to invoke when the subscription is disposed.
        /// </summary>
        private readonly Action<ChannelObservableSubscription<T>> _onDisposed;


        /// <summary>
        /// Creates a new <see cref="ChannelObservableSubscription{T}"/> object.
        /// </summary>
        /// <param name="observer">
        ///   The underlying observer for the subscription.
        /// </param>
        /// <param name="onDisposed">
        ///   A callback to invoke when the subscription is disposed.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="observer"/> is <see langword="null"/>.
        /// </exception>
        internal ChannelObservableSubscription(IObserver<T> observer, Action<ChannelObservableSubscription<T>> onDisposed) {
            _observer = observer ?? throw new ArgumentNullException(nameof(observer));
            _onDisposed = onDisposed;
        }


        /// <inheritdoc/>
        void IObserver<T>.OnCompleted() {
            if (!_isCompleted) {
                _isCompleted = true;
                _observer.OnCompleted();
            }
        }


        /// <inheritdoc/>
        void IObserver<T>.OnError(Exception error) {
            if (!_isCompleted) {
                _isCompleted = true;
                _observer.OnError(error);
            }
        }


        /// <inheritdoc/>
        void IObserver<T>.OnNext(T value) {
            if (_isCompleted) {
                return;
            }

            _observer.OnNext(value);
        }


        /// <inheritdoc/>
        public void Dispose() {
            if (_disposed) {
                return;
            }

            _disposed = true;
            _onDisposed?.Invoke(this);
        }
    }
}
