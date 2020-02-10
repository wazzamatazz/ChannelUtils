using System;
using System.Threading.Channels;

namespace Jaahas.ChannelUtils {

    /// <summary>
    /// Extensions for <see cref="Channel{T}"/>.
    /// </summary>
    public static class ChannelExtensions {

        /// <summary>
        /// Converts a <see cref="ChannelReader{T}"/> to an <see cref="IObservable{T}"/>.
        /// </summary>
        /// <typeparam name="T">
        ///   The channel item type.
        /// </typeparam>
        /// <param name="channel">
        ///   The channel reader.
        /// </param>
        /// <returns>
        ///   An <see cref="IObservable{T}"/> that can be used to multicast items published to the channel.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null"/>.
        /// </exception>
        public static IObservable<T> ToObservable<T>(this ChannelReader<T> channel) {
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            return new ChannelObservable<T>(channel);
        }


        /// <summary>
        /// Converts a <see cref="ChannelWriter{T}"/> to an <see cref="IObserver{T}"/>.
        /// </summary>
        /// <typeparam name="T">
        ///   The channel item type.
        /// </typeparam>
        /// <param name="channel">
        ///   The channel writer.
        /// </param>
        /// <returns>
        ///   An <see cref="IObserver{T}"/> that can be used to receive values from an 
        ///   <see cref="IObservable{T}"/> and republish them to the <paramref name="channel"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null"/>.
        /// </exception>
        public static IObserver<T> ToObserver<T>(this ChannelWriter<T> channel) {
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            return new ChannelPublisher<T>(channel);
        }

    }
}
