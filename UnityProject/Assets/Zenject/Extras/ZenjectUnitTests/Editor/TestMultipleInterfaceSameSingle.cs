using System;
using System.Collections.Generic;
using Zenject;
using NUnit.Framework;
using ModestTree;
using Assert=ModestTree.Assert;

namespace Zenject.Tests
{
    [TestFixture]
    public class TestMultipleInterfaceSameSingle : TestWithContainer
    {
        private interface ITest1
        {
        }

        private interface ITest2
        {
        }

        private class Test1 : ITest1, ITest2
        {
        }

        [Test]
        public void TestCase1()
        {
            Container.Bind<ITest1>().ToSingle<Test1>();
            Container.Bind<ITest2>().ToSingle<Test1>();

            Assert.That(Container.ValidateResolve<ITest1>().IsEmpty());
            var test1 = Container.Resolve<ITest1>();
            Assert.That(Container.ValidateResolve<ITest2>().IsEmpty());
            var test2 = Container.Resolve<ITest2>();

            Assert.That(ReferenceEquals(test1, test2));
        }
    }
}


