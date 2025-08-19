using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Unifi.IpManager.Controllers;
using Unifi.IpManager.Options;
using Unifi.IpManager.Models.DTO;
using Unifi.IpManager.Models.Unifi;
using Unifi.IpManager.Services;

namespace Unifi.IpManager.Tests.ControllerTests
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
                Data = new List<UniClient> { new() { Id = "test" } }
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

        [Test]
        public void PostNewClientTest()
        {
            var logMock = new Mock<ILogger<ClientController>>();
            var unifiMock = new Mock<IUnifiService>();
            var newRequest = new NewClientRequest {
                Name = "testclient",
                MacAddress = "00:11:22:33:44:55",
                IpAddress = "192.168.1.100",
                SyncDns = true,
                StaticIp = true,
                Network = "LAN"
            };
            var expectedClient = new UniClient { Id = "newid" };
            unifiMock.Setup(service => service.CreateClient(newRequest).Result).Returns(new ServiceResult<UniClient>
            {
                Success = true,
                Data = expectedClient
            });
            var controller = new ClientController(unifiMock.Object, logMock.Object);
            var result = controller.Post(newRequest).Result;
            Assert.Multiple(() =>
            {
                Assert.That(result.Value, Is.Not.Null);
                Assert.That(result.Value?.Success, Is.True);
                Assert.That(result.Value?.Data, Is.EqualTo(expectedClient));
            });
        }

        [Test]
        public void PutEditClientTest()
        {
            var logMock = new Mock<ILogger<ClientController>>();
            var unifiMock = new Mock<IUnifiService>();
            var mac = "00:11:22:33:44:55";
            var editRequest = new EditClientRequest { Name = "edited" };
            unifiMock.Setup(service => service.UpdateClient(mac, editRequest).Result).Returns(new ServiceResult
            {
                Success = true
            });
            var controller = new ClientController(unifiMock.Object, logMock.Object);
            var result = controller.Put(mac, editRequest).Result;
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value?.Success, Is.True);
        }

        [Test]
        public void DeleteClientTest()
        {
            var logMock = new Mock<ILogger<ClientController>>();
            var unifiMock = new Mock<IUnifiService>();
            var mac = "00:11:22:33:44:55";
            unifiMock.Setup(service => service.DeleteClient(mac).Result).Returns(new ServiceResult
            {
                Success = true
            });
            var controller = new ClientController(unifiMock.Object, logMock.Object);
            var result = controller.DeleteClient(mac).Result;
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value?.Success, Is.True);
        }

        [Test]
        public void ProvisionClientTest()
        {
            var logMock = new Mock<ILogger<ClientController>>();
            var unifiMock = new Mock<IUnifiService>();
            var provisionRequest = new ProvisionRequest {
                Name = "provclient",
                StaticIp = true,
                SyncDns = true
            };
            var expectedClient = new UniClient { Id = "provId" };
            unifiMock.Setup(service => service.ProvisionNewClient(provisionRequest).Result).Returns(new ServiceResult<UniClient>
            {
                Success = true,
                Data = expectedClient
            });
            var controller = new ClientController(unifiMock.Object, logMock.Object);
            var result = controller.ProvisionClient(provisionRequest).Result;
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value?.Success, Is.True);
            Assert.That(result.Value?.Data, Is.EqualTo(expectedClient));
        }

    }
}
