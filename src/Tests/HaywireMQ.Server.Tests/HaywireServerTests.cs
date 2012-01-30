using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoFakeItEasy;

namespace HaywireMQ.Server.Tests
{
    /// <summary>
    /// Summary description for UnitTest1
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
        [ExpectedException(typeof(ArgumentNullException))]
        public void Should_exception_when_passed_null_MessageStore()
        {
            var target = new HaywireServer(null);
        }
    }
}
