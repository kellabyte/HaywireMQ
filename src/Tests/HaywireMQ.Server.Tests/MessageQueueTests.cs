using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using HaywireMQ.Server.Channel;
using HaywireMQ.Server.MessageStore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoFakeItEasy;
using Ploeh.AutoFixture.Kernel;

namespace HaywireMQ.Server.Tests
{
    [TestClass]
    public class MessageQueueTests
    {
        private class TestConventions : CompositeCustomization
        {
            public TestConventions()
                : base(
                    new AutoFakeItEasyCustomization(),
                    new GreedyHaywireServerCustomization())
            {
            }
        }

        private class GreedyHaywireServerCustomization : ICustomization
        {
            public void Customize(IFixture fixture)
            {
                fixture.Customize<HaywireServer>(c =>
                    c.FromFactory(new MethodInvoker(new GreedyConstructorQuery())));

                fixture.Customize<DriverCatalog>(c =>
                    c.FromFactory(new MethodInvoker(new GreedyConstructorQuery())));
            }
        }

        [TestMethod]
        public void Should_do_something()
        {
            // TODO: This test isn't finished.

            var fixture = new Fixture().Customize(new TestConventions());
            var store = fixture.Freeze<IMessageStore>();
            var channel = fixture.Freeze<IMessageChannel>();

            // If you breakpoint the constructor of the DriverCatalog
            // the store and channel get set properly with the fakes.
            var catalog = fixture.Freeze<DriverCatalog>();

            // If here you inspect the catalog.MessageStores or 
            // catalog.MessageChannels the collections are empty. Why?!

            var server = new HaywireServer(catalog);
            server.Start();
        }
    }
}
