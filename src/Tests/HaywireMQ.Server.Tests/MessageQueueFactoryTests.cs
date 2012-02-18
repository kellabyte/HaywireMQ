using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using FakeItEasy;
using HaywireMQ.Server.Channel;
using HaywireMQ.Server.Store;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoFakeItEasy;
using Ploeh.AutoFixture.Kernel;

namespace HaywireMQ.Server.Tests
{
    [TestClass]
    public class MessageQueueFactoryTests
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

                fixture.Customize<InMemoryMessageChannel>(c =>
                    c.FromFactory(new MethodInvoker(new GreedyConstructorQuery())));

                fixture.Customize<MessageQueueFactory>(c =>
                    c.FromFactory(new MethodInvoker(new GreedyConstructorQuery())));
            }
        }

        [TestMethod]
        public void Should_return_queue_when_created()
        {
            var queueName = "test";
            var fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());
            var store = fixture.Freeze<IMessageStore>();
            var sut = fixture.Freeze<MessageQueueFactory>();

            A.CallTo(() => store.CreateQueue(queueName)).Returns(true);

            var queue = sut.Create(queueName);
            Assert.AreEqual<string>(queueName, queue.Id);
        }
    }
}
