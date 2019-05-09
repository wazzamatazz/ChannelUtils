using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jaahas.ChannelUtils.Tests {

    [TestClass]
    public class MulticastChannelTests {

        [TestMethod]
        public void NullSourceChannelReader_ShouldThrowConstructorException() {
            Assert.ThrowsException<ArgumentNullException>(() => new MulticastChannel<object>(null));
        }


        [TestMethod]
        public async Task DestinationChannel_ShouldCompleteWhenMasterChannelCompletes() {
            var masterChannel = Channel.CreateUnbounded<int>();
            var destinationChannel = Channel.CreateUnbounded<int>();

            using (var multicast = new MulticastChannel<int>(masterChannel)) {
                multicast.AddDestination(destinationChannel);

                var destinationReadTask = Task.Run(async () => {
                    var total = 0;

                    while (await destinationChannel.Reader.WaitToReadAsync().ConfigureAwait(false)) {
                        if (destinationChannel.Reader.TryRead(out var item)) {
                            total += item;
                        }
                    }

                    return total;
                });

                var expectedTotal = 0;
                for (var i = 1; i <= 5; i++) {
                    expectedTotal += i;
                    masterChannel.Writer.TryWrite(i);
                }

                masterChannel.Writer.TryComplete();

                var actualTotal = await destinationReadTask.ConfigureAwait(false);
                Assert.AreEqual(expectedTotal, actualTotal);
            }
        }


        [TestMethod]
        public async Task DestinationChannels_ShouldReceiveSameItems() {
            var masterChannel = Channel.CreateUnbounded<int>();
            var destinationChannel1 = Channel.CreateUnbounded<int>();
            var destinationChannel2 = Channel.CreateUnbounded<int>();

            using (var multicast = new MulticastChannel<int>(masterChannel)) {
                multicast.AddDestination(destinationChannel1);
                multicast.AddDestination(destinationChannel2);

                var destinationReadTask1 = Task.Run(async () => {
                    var total = 0;

                    while (await destinationChannel1.Reader.WaitToReadAsync().ConfigureAwait(false)) {
                        if (destinationChannel1.Reader.TryRead(out var item)) {
                            total += item;
                        }
                    }

                    return total;
                });
                var destinationReadTask2 = Task.Run(async () => {
                    var total = 0;

                    while (await destinationChannel2.Reader.WaitToReadAsync().ConfigureAwait(false)) {
                        if (destinationChannel2.Reader.TryRead(out var item)) {
                            total += item;
                        }
                    }

                    return total;
                });

                var expectedTotal = 0;
                for (var i = 1; i <= 5; i++) {
                    expectedTotal += i;
                    masterChannel.Writer.TryWrite(i);
                }

                masterChannel.Writer.TryComplete();

                var actualTotal1 = await destinationReadTask1.ConfigureAwait(false);
                Assert.AreEqual(expectedTotal, actualTotal1);

                var actualTotal2 = await destinationReadTask2.ConfigureAwait(false);
                Assert.AreEqual(expectedTotal, actualTotal2);
            }
        }

    }

}
