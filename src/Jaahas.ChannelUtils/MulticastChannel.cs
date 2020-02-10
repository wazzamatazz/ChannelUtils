using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;

namespace Jaahas.ChannelUtils {

    /// <summary>
    /// Reads from a <see cref="Channel{T}"/> and rebroadcasts the read items to multiple subscribed channels.
    /// </summary>
    /// <typeparam name="T">
    ///   The item type for the channel.
    /// </typeparam>
    public class MulticastChannel<T> : IDisposable {

        /// <summary>
        /// Flags if the object has been disposed.
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// The observable that will multicast values from the input channel.
        /// </summary>
        private readonly IObservable<T> _observable;

        /// <summary>
        /// Holds the subscribed channels.
        /// </summary>
        private readonly ConcurrentDictionary<ChannelWriter<T>, IDisposable> _subscriptions = new ConcurrentDictionary<ChannelWriter<T>, IDisposable>();

        /// <summary>
        /// Gets the number of subscribers to the <see cref="MulticastChannel{T}"/>.
        /// </summary>
        public int Count {
            get {
                return _subscriptions.Count;
            }
        }


        /// <summary>
        /// Creates a new <see cref="MulticastChannel{T}"/> object.
        /// </summary>
        /// <param name="source">
        ///   The source channel to read from.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source"/> is <see langword="null"/>.
        /// </exception>
        public MulticastChannel(ChannelReader<T> source) {
            _observable = source.ToObservable();
        }


        /// <summary>
        /// Creates a new <see cref="MulticastChannel{T}"/> object.
        /// </summary>
        /// <param name="source">
        ///   The source channel to read from.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source"/> is <see langword="null"/>.
        /// </exception>
        public MulticastChannel(Channel<T> source) : this(source?.Reader) { }


        /// <summary>
        /// Adds a destination to the <see cref="MulticastChannel{T}"/>.
        /// </summary>
        /// <param name="writer">
        ///   The destination channel.
        /// </param>
        /// <returns>
        ///   <see langword="true" /> if the destination was successfully added, or <see langword="false"/>
        ///   otherwise (e.g. if the <see cref="MulticastChannel{T}"/> has been disposed).
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="writer"/> is <see langword="null"/>.
        /// </exception>
        public bool AddDestination(ChannelWriter<T> writer) {
            if (writer == null) {
                throw new ArgumentNullException(nameof(writer));
            }

            if (_isDisposed) {
                return false;
            }

            _subscriptions.GetOrAdd(writer, ch => _observable.Subscribe(ch.ToObserver()));

            return true;
        }


        /// <summary>
        /// Adds a destination to the <see cref="MulticastChannel{T}"/>.
        /// </summary>
        /// <param name="channel">
        ///   The destination channel.
        /// </param>
        /// <returns>
        ///   <see langword="true" /> if the destination was successfully added, or <see langword="false"/>
        ///   otherwise (e.g. if the <see cref="MulticastChannel{T}"/> has been disposed).
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null"/>.
        /// </exception>
        public bool AddDestination(Channel<T> channel) {
            return AddDestination(channel?.Writer);
        }


        /// <summary>
        /// Removes a destination from the <see cref="MulticastChannel{T}"/>.
        /// </summary>
        /// <param name="writer">
        ///   The destination to remove.
        /// </param>
        /// <returns>
        ///   <see langword="true" /> if the destination was successfully remove, or <see langword="false"/>
        ///   otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="writer"/> is <see langword="null"/>.
        /// </exception>
        public bool RemoveDestination(ChannelWriter<T> writer) {
            if (writer == null) {
                throw new ArgumentNullException(nameof(writer));
            }

            if (_isDisposed) {
                return false;
            }

            if (_subscriptions.TryRemove(writer, out var subscription)) {
                subscription.Dispose();
                writer.TryComplete();
                return true;
            }

            return false;
        }


        /// <summary>
        /// Removes a destination from the <see cref="MulticastChannel{T}"/>.
        /// </summary>
        /// <param name="channel">
        ///   The destination to remove.
        /// </param>
        /// <returns>
        ///   <see langword="true" /> if the destination was successfully remove, or <see langword="false"/>
        ///   otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null"/>.
        /// </exception>
        public bool RemoveDestination(Channel<T> channel) {
            return RemoveDestination(channel?.Writer);
        }


        /// <summary>
        /// Disposes of the <see cref="MulticastChannel{T}"/>.
        /// </summary>
        public void Dispose() {
            if (_isDisposed) {
                return;
            }

            _isDisposed = true;

            foreach (var item in _subscriptions.ToArray()) {
                item.Value.Dispose();
                item.Key.TryComplete();
            }

            _subscriptions.Clear();
            
        }
    }
}
