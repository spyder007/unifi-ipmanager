using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using unifi.ipmanager.Controllers;
using unifi.ipmanager.Models;
using unifi.ipmanager.Models.DTO;
using unifi.ipmanager.Models.Unifi;
using unifi.ipmanager.Services;

namespace unifi.ipmanager.tests
{
    public class ClientControllerTests
    {
        private UnifiControllerOptions? _testOptions = new();

        [SetUp]
        public void Setup()
        {
            _testOptions = new UnifiControllerOptions
            {
                Url = "https://localhost/",
                Username = "test",
                Password = "1234"
            };
        }

        [Test]
        public void GetFixedClientsTest()
        {
            var logMock = new Mock<ILogger<ClientController>>();

            var unifiMock = new Mock<IUnifiService>();

            unifiMock.Setup(service => service.GetAllFixedClients().Result).Returns(new ServiceResult<List<UniClient>>
            {
                Success = true,
                Data = new List<UniClient>
                {
                    { new () { Id = "test" } }
                }
            });

            var controller = new ClientController(unifiMock.Object, logMock.Object);

            var result = controller.Get().Result;

            Assert.Multiple(() =>
            {
                Assert.That(result.Value, Is.Not.Null);
                Assert.That(result.Value?.Success, Is.EqualTo(true));
                Assert.That(result.Value?.Data, Has.Exactly(1).Items);
            });
        }
    }
}