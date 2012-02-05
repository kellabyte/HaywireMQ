using System;
using System.Collections.Generic;
using System.Linq;
using FakeItEasy;
using HaywireMQ.Server.Channel;
using HaywireMQ.Server.MessageStore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoFakeItEasy;
using Ploeh.AutoFixture.Kernel;

namespace HaywireMQ.Server.Tests
{
    /// <summary>
    /// Tests for HaywireServer
    /// </summary>
    [TestClass]
    public class HaywireServerTests
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
            }
        }

        [TestMethod]
        public void Should_use_default_messagestore_when_started_without_modulecatalog()
        {
            var fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());            
            var sut = fixture.CreateAnonymous<HaywireServer>();
            sut.Start();

            Assert.IsInstanceOfType(sut.MessageStore, typeof(InMemoryMessageStore));
        }

        [TestMethod]
        public void Should_use_default_messagechannel_when_started_without_modulecatalog()
        {
            var fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());
            var sut = fixture.CreateAnonymous<HaywireServer>();
            sut.Start();

            Assert.IsInstanceOfType(sut.MessageChannel, typeof(InMemoryMessageChannel));
        }

        [TestMethod]
        public void Should_use_messagestore_from_modulecatalog_when_started()
        {
            var fixture = new Fixture().Customize(new TestConventions());
            var catalog = fixture.Freeze<DriverCatalog>();
            fixture.AddManyTo(catalog.MessageStores, 1);
            fixture.AddManyTo(catalog.MessageChannels, 1);
            var sut = fixture.CreateAnonymous<HaywireServer>();

            sut.Start();

            Assert.AreEqual(catalog.MessageStores.Single(), sut.MessageStore);
        }

        [TestMethod]
        public void Should_use_messagechannel_from_modulecatalog_when_started()
        {
            var fixture = new Fixture().Customize(new TestConventions());
            var catalog = fixture.Freeze<DriverCatalog>();
            fixture.AddManyTo(catalog.MessageStores, 1);
            fixture.AddManyTo(catalog.MessageChannels, 1);
            var sut = fixture.CreateAnonymous<HaywireServer>();

            sut.Start();

            Assert.AreEqual(catalog.MessageChannels.Single(), sut.MessageChannel);
        }

        [TestMethod]
        public void Should_have_1_messagequeue_when_started_with_1_queue_in_messagestore()
        {
            var fixture = new Fixture().Customize(new TestConventions());
            var catalog = fixture.Freeze<DriverCatalog>();
            fixture.AddManyTo(catalog.MessageStores, 1);
            fixture.AddManyTo(catalog.MessageChannels, 1);
            List<string> ids = fixture.CreateMany<string>(1).ToList();
            A.CallTo(() => catalog.MessageStores.Single().GetQueues()).Returns(ids);
            var sut = fixture.CreateAnonymous<HaywireServer>();

            sut.Start();

            Assert.AreEqual(1, sut.MessageQueues.Count);
        }

        [TestMethod]
        public void Should_have_same_messagequeue_as_messagestore_when_started()
        {
            var fixture = new Fixture().Customize(new TestConventions());
            var catalog = fixture.Freeze<DriverCatalog>();
            fixture.AddManyTo(catalog.MessageStores, 1);
            fixture.AddManyTo(catalog.MessageChannels, 1);
            List<string> ids = fixture.CreateMany<string>(1).ToList();
            A.CallTo(() => catalog.MessageStores.Single().GetQueues()).Returns(ids);
            var sut = fixture.CreateAnonymous<HaywireServer>();

            sut.Start();

            Assert.AreEqual(ids.First(), sut.MessageQueues[0].Id);
        }
    }
}