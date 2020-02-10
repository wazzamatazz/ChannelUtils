using System;
using System.Threading.Channels;

namespace Jaahas.ChannelUtils {

    /// <summary>
    /// Wraps a <see cref="ChannelWriter{T}"/> with an <see cref="IObserver{T}"/>, allowing an 
    /// <see cref="IObservable{T}"/> to be republished to a <see cref="Channel{T}"/>.
    /// </summary>
    /// <typeparam name="T">
    ///   The observer item type.
    /// </typeparam>
    public class ChannelPublisher<T> : IObserver<T> {

        /// <summary>
        /// The channel to publish notifications to.
        /// </summary>
        private readonly ChannelWriter<T> _channel;


        /// <summary>
        /// Creates a new <see cref="ChannelPublisher{T}"/> object.
        /// </summary>
        /// <param name="channel">
        ///   The channel to publish received messages to.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null"/>.
        /// </exception>
        public ChannelPublisher(ChannelWriter<T> channel) {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
        }


        /// <inheritdoc/>
        public void OnCompleted() {
            _channel.TryComplete();
        }


        /// <inheritdoc/>
        public void OnError(Exception error) {
            _channel.TryComplete(error);
        }


        /// <inheritdoc/>
        public void OnNext(T value) {
            _channel.TryWrite(value);
        }

    }
}
