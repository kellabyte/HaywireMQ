using System;
using FakeItEasy;
using HaywireMQ.Server.Store;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoFakeItEasy;
using Ploeh.AutoFixture.Kernel;

namespace HaywireMQ.Server.Tests
{
    [TestClass]
    public class InMemoryMessageStoreTests
    {
        private class TestConventions : CompositeCustomization
        {
            public TestConventions()
                : base(
                    new AutoFakeItEasyCustomization(),
                    new TestsCustomization())
            {
            }
        }

        private class TestsCustomization : ICustomization
        {
            public void Customize(IFixture fixture)
            {
                fixture.Customize<InMemoryMessageStore>(c =>
                    c.FromFactory(new MethodInvoker(new GreedyConstructorQuery())));
            }
        }

        [TestMethod]
        public void Queue_should_exist_when_created()
        {
            var sut = new InMemoryMessageStore();
            var queueName = "test";
            sut.CreateQueue(queueName);

            var exists = sut.QueueExists(queueName);
            Assert.AreEqual<bool>(true, exists);
        }

        [TestMethod]
        public void Should_store_queue_when_queue_created()
        {
            var sut = new InMemoryMessageStore();
            var queueName = "test";
            sut.CreateQueue(queueName);

            var queues = sut.GetQueues();
            Assert.AreEqual<string>(queueName, queues[0]);
        }

        [TestMethod]
        public void Sequence_should_be_1_when_first_sequence_requested()
        {            
            var fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());
            var queueName = "test";
            var sut = fixture.Freeze<InMemoryMessageStore>();
            sut.CreateQueue(queueName);
            var sequence = sut.GetNextSequence(queueName);

            Assert.AreEqual<ulong>(1, sequence);
        }

        [TestMethod]
        public void Sequences_should_be_in_sequential_order()
        {            
            var fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());            
            var queueName = "test";
            var sut = fixture.Freeze<InMemoryMessageStore>();
            sut.CreateQueue(queueName);

            for (ulong i=1; i<10; i++)
            {
                var sequence = sut.GetNextSequence(queueName);
                Assert.AreEqual<ulong>(i, sequence);
            }
        }

        [TestMethod]
        public void Sequence_should_not_change_for_queue_not_changing()
        {            
            var fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());
            var queueName1 = "test1";
            var queueName2 = "test2";
            var sut = fixture.Freeze<InMemoryMessageStore>();
            sut.CreateQueue(queueName1);
            sut.CreateQueue(queueName2);

            for (ulong i = 1; i < 10; i++)
            {
                var sequence = sut.GetNextSequence(queueName2);
            }
            Assert.AreEqual<ulong>(1, sut.GetNextSequence(queueName1));
        }

        [TestMethod]
        public void Message_should_be_retrieved_when_stored()
        {            
            var fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());
            var queueName = "test";
            var sut = fixture.Freeze<InMemoryMessageStore>();
            sut.CreateQueue(queueName);
            var seq = sut.GetNextSequence(queueName);
            var msg = new Message(seq);

            sut.StoreMessage(queueName, msg);
            var msg2 = sut.GetMessage(queueName, seq);

            Assert.AreEqual<ulong>(msg.Id, msg2.Id);
        }
    }
}
