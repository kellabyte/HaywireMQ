using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using FakeItEasy;
using HaywireMQ.Server.Channel;
using HaywireMQ.Server.MessageStore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoFakeItEasy;

namespace HaywireMQ.Server.Tests
{
    /// <summary>
    /// Tests for HaywireServer
    /// </summary>
    [TestClass]
    public class HaywireServerTests
    {
        private IFixture fixture;

        public HaywireServerTests()
        {
        }

        [TestInitialize]
        public void Initialize()
        {
            fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());
        }

        [TestMethod]
        public void Should_use_defaults_without_ModuleCatalog()
        {
            // Given
            var target = new HaywireServer();

            // When
            target.Start();

            // Then
            Assert.AreEqual<Type>(target.MessageStore.GetType(), typeof(InMemoryMessageStore));
            Assert.AreEqual<Type>(target.MessageChannel.GetType(), typeof(InMemoryMessageChannel));
        }

        [TestMethod]
        public void Should_use_ModuleCatalog()
        {
            // Given
            var catalog = new ModuleCatalog();
            var messageStore = fixture.CreateAnonymous<IMessageStore>();
            var messageChannel = fixture.CreateAnonymous<IMessageChannel>();
            catalog.MessageStores.Add(messageStore);
            catalog.MessageChannels.Add(messageChannel);
            var target = new HaywireServer(catalog);

            // When
            target.Start();

            // Then
            Assert.AreEqual<Type>(target.MessageStore.GetType(), messageStore.GetType());
            Assert.AreEqual<Type>(target.MessageChannel.GetType(), messageChannel.GetType());
        }

        [TestMethod]
        public void Should_create_MessageQueue()
        {
            // Given
            var catalog = new ModuleCatalog();
            var messageStore = fixture.CreateAnonymous<IMessageStore>();
            var messageChannel = fixture.CreateAnonymous<IMessageChannel>();
            catalog.MessageStores.Add(messageStore);
            catalog.MessageChannels.Add(messageChannel);
            var target = new HaywireServer(catalog);

            List<string> ids = new List<string>() {"test"};

            A.CallTo(() => messageStore.GetQueues()).Returns(ids);

            // When
            target.Start();

            // Then
            A.CallTo(() => messageStore.GetQueues()).MustHaveHappened();
            Assert.AreEqual<int>(target.MessageQueues.Count, 1);
            Assert.AreEqual<string>(target.MessageQueues[0].Id, "test");
        }
    }
}
