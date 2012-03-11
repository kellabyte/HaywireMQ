using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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
    public class InMemoryMessageChannelTests
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

                fixture.Customize<InMemoryMessageChannel>(c =>
                    c.FromFactory(new MethodInvoker(new GreedyConstructorQuery())));
            }
        }

        [TestMethod]
        public void Should_return_queue_when_created()
        {
            //var resetEvent = new ManualResetEvent(false);
            var fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());
            var msg = fixture.CreateAnonymous<Message>();
            var sut = fixture.Freeze<InMemoryMessageChannel>();

            object msg2 = null;

            int count = 1;
            var watch = new Stopwatch();
            watch.Start();

            for (int i = 0; i < count; i++)
            {
                sut.BeginReceive(TimeSpan.FromSeconds(1), ar =>
                                                               {
                                                                   msg2 = sut.EndReceive(ar);
                                                                   //resetEvent.Set();
                                                               },
                                 null);

                sut.Send(sut.QueueId, msg);
                //resetEvent.WaitOne(TimeSpan.FromSeconds(5));
            }

            watch.Stop();
            //int rate = count / (int)watch.Elapsed.TotalSeconds;

            Assert.AreSame(msg, msg2);
        }
    }
}
