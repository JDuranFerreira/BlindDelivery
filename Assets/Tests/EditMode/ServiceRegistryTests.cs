using System;
using NUnit.Framework;
using Template.Core.Services;
using Template.Tests.EditMode.Fakes;

namespace Template.Tests.EditMode
{
    public class ServiceRegistryTests
    {
        ServiceRegistry registry;

        [SetUp]
        public void SetUp() => registry = new ServiceRegistry();

        [Test]
        public void Register_PreservesOrder()
        {
            registry.Register(new FakeService<TagOne>("save"));
            registry.Register(new FakeService<TagTwo>("settings"));
            registry.Register(new OtherFakeService("audio"));

            Assert.That(registry.Count, Is.EqualTo(3));
            Assert.That(registry.Services[0].Name, Is.EqualTo("save"));
            Assert.That(registry.Services[1].Name, Is.EqualTo("settings"));
            Assert.That(registry.Services[2].Name, Is.EqualTo("audio"));
        }

        [Test]
        public void Get_ReturnsTheRegisteredInstance_ByConcreteType()
        {
            FakeService<TagOne> service = new FakeService<TagOne>("save");
            registry.Register(service);

            Assert.That(registry.Get<FakeService<TagOne>>(), Is.SameAs(service));
        }

        [Test]
        public void Get_ReturnsTheRegisteredInstance_ByInterfaceSeam()
        {
            FakeService<TagOne> service = new FakeService<TagOne>("save");
            registry.Register(service);

            Assert.That(registry.Get<IFakeSeam>(), Is.SameAs(service));
        }

        [Test]
        public void Get_UnknownType_ThrowsWithATypedMessage()
        {
            InvalidOperationException error =
                Assert.Throws<InvalidOperationException>(() => registry.Get<OtherFakeService>());

            Assert.That(error.Message, Does.Contain(nameof(OtherFakeService)));
        }

        [Test]
        public void TryGet_UnknownType_ReturnsFalse()
        {
            Assert.That(registry.TryGet<OtherFakeService>(out OtherFakeService found), Is.False);
            Assert.That(found, Is.Null);
        }

        [Test]
        public void Register_SameConcreteTypeTwice_Throws()
        {
            registry.Register(new FakeService<TagOne>("save"));

            Assert.Throws<InvalidOperationException>(
                () => registry.Register(new FakeService<TagOne>("save")));
            Assert.That(registry.Count, Is.EqualTo(1));
        }

        [Test]
        public void Register_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => registry.Register(null));
        }
    }
}
