using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Jaahas.ChannelUtils {

    /// <summary>
    /// Reads from a <see cref="Channel{T}"/> and rebroadcasts the read items to multiple subscribers.
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
        /// Fires when the object is disposed.
        /// </summary>
        private readonly CancellationTokenSource _disposedTokenSource = new CancellationTokenSource();

        /// <summary>
        /// The channel to read from.
        /// </summary>
        private readonly ChannelReader<T> _reader;

        /// <summary>
        /// The channels to rebroadcast to.
        /// </summary>
        private readonly List<ChannelWriter<T>> _writers = new List<ChannelWriter<T>>();

        /// <summary>
        /// Gets the number of subscribers to the <see cref="MulticastChannel{T}"/>.
        /// </summary>
        public int Count {
            get {
                lock (_writers) {
                    return _writers.Count;
                }
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
            _reader = source ?? throw new ArgumentNullException(nameof(source));
            _ = Run(_disposedTokenSource.Token);
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
        /// Reads and republishes from the <see cref="_reader"/> until it completes, or the canellation 
        /// token fires.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token that will fire when the task should complete.
        /// </param>
        /// <returns>
        ///   A task that will read items from <see cref="_reader"/> and republish them to the 
        ///   registered <see cref="_writers"/>.
        /// </returns>
        private async Task Run(CancellationToken cancellationToken) {
            try {
                while (await _reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                    if (!_reader.TryRead(out var item)) {
                        continue;
                    }

                    lock (_writers) {
                        foreach (var writer in _writers) {
                            writer.TryWrite(item);
                        }
                    }
                }

                lock (_writers) {
                    foreach (var writer in _writers) {
                        writer.TryComplete();
                    }
                }
            }
            catch (Exception e) {
                lock (_writers) {
                    foreach (var writer in _writers) {
                        writer.TryComplete(e);
                    }
                }
            }
        }


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

            lock (_writers) {
                if (_isDisposed || _disposedTokenSource.IsCancellationRequested) {
                    return false;
                }

                _writers.Add(writer);
                // If the master channel has already completed, immediately mark the writer as completed.
                if (_reader.Completion.IsCompleted) {
                    writer.TryComplete();
                }
                return true;
            }
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

            lock (_writers) {
                if (_isDisposed || _disposedTokenSource.IsCancellationRequested) {
                    return false;
                }

                return _writers.Remove(writer);
            }
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

            _disposedTokenSource.Cancel();
            _disposedTokenSource.Dispose();
            lock (_writers) {
                foreach (var writer in _writers) {
                    writer.TryComplete();
                }
                _writers.Clear();
            }
            _isDisposed = true;
        }
    }
}
