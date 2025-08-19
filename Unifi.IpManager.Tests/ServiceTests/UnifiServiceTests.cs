using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Unifi.IpManager.Models.DTO;
using Unifi.IpManager.Models.Unifi;
using Unifi.IpManager.Options;
using Unifi.IpManager.Services;

namespace Unifi.IpManager.Tests.ServiceTests;

public class UnifiServiceTests
{
    private Mock<IUnifiClient> _unifiClientMock = null!;
    private Mock<IIpService> _ipServiceMock = null!;
    private IOptions<UnifiControllerOptions> _options = null!;
    private UnifiService _service = null!;
    private UnifiControllerOptions _unifiOptions = null!;
    private Url _baseApiUrlV1 = null!;
    private string _siteId = null!;

    [SetUp]
    public void Setup()
    {
        _unifiClientMock = new Mock<IUnifiClient>();
        _ipServiceMock = new Mock<IIpService>();
        _unifiOptions = new UnifiControllerOptions
        {
            Url = "https://test.com",
            Username = "test",
            Password = "test",
            DnsZone = "local",
            Site = "default"
        };
        _options = Microsoft.Extensions.Options.Options.Create(_unifiOptions);
        _baseApiUrlV1 = "https://test.com/api/v1";
        _siteId = "default";

        _unifiClientMock.Setup(c => c.BaseApiUrlV1).Returns(_baseApiUrlV1);
        _unifiClientMock.Setup(c => c.SiteId).Returns(_siteId);

        _service = new UnifiService(_options, _ipServiceMock.Object, _unifiClientMock.Object);
    }

    [Test]
    public async Task GetAllNetworks_ReturnsNetworks()
    {
        // Arrange
        var networks = new List<UnifiNetwork>
        {
            new() { Id = "1", Name = "LAN", SiteId = "default" },
            new() { Id = "2", Name = "GUEST", SiteId = "default" }
        };

        _unifiClientMock.Setup(c => c.ExecuteRequest(
            It.IsAny<Url>(),
            It.IsAny<Func<IFlurlRequest, Task<UniResponse<List<UnifiNetwork>>>>>(),
            false))
            .ReturnsAsync(new ServiceResult<List<UnifiNetwork>> { Success = true, Data = networks });

        // Act
        var result = await _service.GetAllNetworks();

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Has.Count.EqualTo(2));
        Assert.That(result.Data.Any(n => n.Name == "LAN"), Is.True);
        Assert.That(result.Data.Any(n => n.Name == "GUEST"), Is.True);
    }

    [Test]
    public async Task GetAllFixedClients_ReturnsCombinedClients()
    {
        // Arrange
        var fixedClients = new List<UniClient>
        {
            new() { Id = "1", Name = "Client1", Mac = "00:11:22:33:44:55", UseFixedIp = true, FixedIp = "192.168.1.100" }
        };
        var devices = new List<UniDevice>
        {
            new() { Id = "2", Name = "Device1", Mac = "00:11:22:33:44:66", Network = new UniNetworkConfig { Ip = "192.168.1.101", Netmask = "255.255.255.0", Type = "static" } }
        };

        _unifiClientMock.SetupSequence(c => c.ExecuteRequest(
            It.IsAny<Url>(),
            It.IsAny<Func<IFlurlRequest, Task<UniResponse<List<UniClient>>>>>(),
            false))
            .ReturnsAsync(new ServiceResult<List<UniClient>> { Success = true, Data = fixedClients });

        _unifiClientMock.Setup(c => c.ExecuteRequest(
            It.IsAny<Url>(),
            It.IsAny<Func<IFlurlRequest, Task<UniResponse<List<UniDevice>>>>>(),
            false))
            .ReturnsAsync(new ServiceResult<List<UniDevice>> { Success = true, Data = devices });

        _ipServiceMock.Setup(i => i.GetIpGroupForAddress("192.168.1.100")).Returns("Group1");
        _ipServiceMock.Setup(i => i.GetIpGroupForAddress("192.168.1.101")).Returns("Group1");

        // Act
        var result = await _service.GetAllFixedClients();

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Has.Count.EqualTo(2));
        Assert.That(result.Data.Any(c => c.Name == "Client1"), Is.True);
        Assert.That(result.Data.Any(c => c.Name == "Device1"), Is.True);
    }

    [Test]
    public async Task GetAllFixedClients_GetAllFixedIpClientsFails()
    {
        _unifiClientMock.Setup(c => c.ExecuteRequest(
            It.IsAny<Url>(),
            It.IsAny<Func<IFlurlRequest, Task<UniResponse<List<UniClient>>>>>(),
            false))
            .ThrowsAsync(new Exception("Flurl Error"));

        // Act
        var result = await _service.GetAllFixedClients();

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Errors, Contains.Item("Flurl Error"));
    }

    [Test]
    public async Task GetAllFixedClients_GetDevicesAsUniClientFails()
    {
        // Arrange
        var fixedClients = new List<UniClient>
        {
            new() { Id = "1", Name = "Client1", Mac = "00:11:22:33:44:55", UseFixedIp = true, FixedIp = "192.168.1.100" }
        };

        _unifiClientMock.SetupSequence(c => c.ExecuteRequest(
            It.IsAny<Url>(),
            It.IsAny<Func<IFlurlRequest, Task<UniResponse<List<UniClient>>>>>(),
            false))
            .ReturnsAsync(new ServiceResult<List<UniClient>> { Success = true, Data = fixedClients });

        _unifiClientMock.Setup(c => c.ExecuteRequest(
            It.IsAny<Url>(),
            It.IsAny<Func<IFlurlRequest, Task<UniResponse<List<UniDevice>>>>>(),
            false))
            .ThrowsAsync(new Exception("Flurl Error"));

        _ipServiceMock.Setup(i => i.GetIpGroupForAddress("192.168.1.100")).Returns("Group1");
        _ipServiceMock.Setup(i => i.GetIpGroupForAddress("192.168.1.101")).Returns("Group1");

        // Act
        var result = await _service.GetAllFixedClients();

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Errors, Contains.Item("Flurl Error"));
    }


    [Test]
    public async Task CreateClient_Success()
    {
        // Arrange
        var newClientRequest = new NewClientRequest
        {
            MacAddress = "00:11:22:33:44:55",
            Name = "TestClient",
            Hostname = "testclient",
            IpAddress = "192.168.1.100",
            SyncDns = true,
            StaticIp = true,
            Network = "LAN"
        };

        var networks = new List<UnifiNetwork>
        {
            new() { Id = "network1", Name = "LAN" }
        };

        var createdClient = new UniClient
        {
            Id = "newclient1",
            Name = "TestClient",
            Mac = "00:11:22:33:44:55"
        };

        // Mock the mac address check - Data = false indicates MAC does not exist.
        _unifiClientMock.Setup(c => c.ExecuteRequest(
            It.Is<Url>(u => u.ToString().Contains("user") && u.QueryParams.Any(q => q.Name == "mac")),
            It.IsAny<Func<IFlurlRequest, Task<bool>>>(),
            false))
            .ReturnsAsync(new ServiceResult<bool> { Success = true, Data = false });

        _unifiClientMock.Setup(c => c.ExecuteRequest(
            It.Is<Url>(u => u.ToString().Contains("networkconf")),
            It.IsAny<Func<IFlurlRequest, Task<UniResponse<List<UnifiNetwork>>>>>(),
            false))
            .ReturnsAsync(new ServiceResult<List<UnifiNetwork>> { Success = true, Data = networks });

        _unifiClientMock.Setup(c => c.ExecuteRequest(
            It.Is<Url>(u => u.ToString().Contains("user")),
            It.IsAny<Func<IFlurlRequest, Task<UniClient>>>(),
            true))
            .ReturnsAsync(new ServiceResult<UniClient> { Success = true, Data = createdClient });

        // Act
        var result = await _service.CreateClient(newClientRequest);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.Name, Is.EqualTo("TestClient"));
    }

    [Test]
    public async Task CreateClient_ClientAlreadyExists()
    {
        // Arrange
        var newClientRequest = new NewClientRequest
        {
            MacAddress = "00:11:22:33:44:55",
            Name = "TestClient",
            Hostname = "testclient",
            IpAddress = "192.168.1.100",
            SyncDns = true,
            StaticIp = true,
            Network = "LAN"
        };

        _unifiClientMock.Setup(c => c.ExecuteRequest(
            It.IsAny<Url>(),
            It.IsAny<Func<IFlurlRequest, Task<bool>>>(),
            false))
            .ReturnsAsync(new ServiceResult<bool> { Success = true, Data = true });

        // Act
        var result = await _service.CreateClient(newClientRequest);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Errors, Contains.Item("Client with Mac Address 00:11:22:33:44:55 already exists."));
    }

    [Test]
    public async Task UpdateClient_Success()
    {
        // Arrange
        var mac = "00:11:22:33:44:55";
        var editRequest = new EditClientRequest
        {
            Name = "UpdatedClient",
            Hostname = "updatedclient"
        };

        var existingClient = new UniClient
        {
            Id = "client1",
            Name = "OldClient",
            Mac = mac,
            Noted = true,
            Note = "{\"dns_hostname\": \"oldhost\"}"
        };

        _unifiClientMock.Setup(c => c.ExecuteRequest(
            It.Is<Url>(u => u.ToString().Contains("user") && u.QueryParams.Any(q => q.Name == "mac")),
            It.IsAny<Func<IFlurlRequest, Task<UniResponse<List<UniClient>>>>>(),
            false))
            .ReturnsAsync(new ServiceResult<List<UniClient>> { Success = true, Data = new List<UniClient> { existingClient } });

        _unifiClientMock.Setup(c => c.ExecuteRequest(
            It.Is<Url>(u => u.ToString().Contains("user/client1")),
            It.IsAny<Func<IFlurlRequest, Task<UniResponse<List<UniClient>>>>>(),
            true))
            .ReturnsAsync(new ServiceResult<List<UniClient>> { Success = true, Data = new List<UniClient>() });

        // Act
        var result = await _service.UpdateClient(mac, editRequest);

        // Assert
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task ProvisionNewClient_Success()
    {
        // Arrange
        var provisionRequest = new ProvisionRequest
        {
            Name = "ProvisionedClient",
            HostName = "provisioned",
            StaticIp = true,
            SyncDns = true,
            Network = "LAN",
            Group = "TestGroup"
        };

        var existingClients = new List<UniClient>
        {
            new() { Mac = "00:11:22:33:44:77" }
        };

        var networks = new List<UnifiNetwork>
        {
            new() { Id = "network1", Name = "LAN" }
        };

        var provisionedClient = new UniClient
        {
            Id = "provisioned1",
            Name = "ProvisionedClient"
        };

        _unifiClientMock.SetupSequence(c => c.ExecuteRequest(
            It.IsAny<Url>(),
            It.IsAny<Func<IFlurlRequest, Task<UniResponse<List<UniClient>>>>>(),
            false))
            .ReturnsAsync(new ServiceResult<List<UniClient>> { Success = true, Data = existingClients });

        _unifiClientMock.Setup(c => c.ExecuteRequest(
            It.IsAny<Url>(),
            It.IsAny<Func<IFlurlRequest, Task<UniResponse<List<UniDevice>>>>>(),
            false))
            .ReturnsAsync(new ServiceResult<List<UniDevice>> { Success = true, Data = new List<UniDevice>() });

        _unifiClientMock.Setup(c => c.ExecuteRequest(
            It.Is<Url>(u => u.ToString().Contains("networkconf")),
            It.IsAny<Func<IFlurlRequest, Task<UniResponse<List<UnifiNetwork>>>>>(),
            false))
            .ReturnsAsync(new ServiceResult<List<UnifiNetwork>> { Success = true, Data = networks });

        _unifiClientMock.Setup(c => c.ExecuteRequest(
            It.Is<Url>(u => u.ToString().Contains("user") && !u.QueryParams.Any()),
            It.IsAny<Func<IFlurlRequest, Task<UniClient>>>(),
            true))
            .ReturnsAsync(new ServiceResult<UniClient> { Success = true, Data = provisionedClient });

        _ipServiceMock.Setup(i => i.GetUnusedGroupIpAddress("TestGroup", It.IsAny<List<string>>()))
            .ReturnsAsync("192.168.1.100");

        // Act
        var result = await _service.ProvisionNewClient(provisionRequest);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.Name, Is.EqualTo("ProvisionedClient"));
    }

    [Test]
    public async Task DeleteClient_Success()
    {
        // Arrange
        var mac = "00:11:22:33:44:55";
        var existingClient = new UniClient
        {
            Id = "client1",
            Mac = mac,
            UseFixedIp = true,
            FixedIp = "192.168.1.100",
            Noted = true
        };

        _unifiClientMock.Setup(c => c.ExecuteRequest(
            It.Is<Url>(u => u.ToString().Contains("user") && u.QueryParams.Any(q => q.Name == "mac")),
            It.IsAny<Func<IFlurlRequest, Task<UniResponse<List<UniClient>>>>>(),
            false))
            .ReturnsAsync(new ServiceResult<List<UniClient>> { Success = true, Data = new List<UniClient> { existingClient } });

        _unifiClientMock.Setup(c => c.ExecuteRequest(
            It.Is<Url>(u => u.ToString().Contains("stamgr")),
            It.IsAny<Func<IFlurlRequest, Task<UniResponse<List<UniClient>>>>>(),
            true))
            .ReturnsAsync(new ServiceResult<List<UniClient>> { Success = true, Data = new List<UniClient>() });

        _ipServiceMock.Setup(i => i.ReturnIpAddress("192.168.1.100")).Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteClient(mac);

        // Assert
        Assert.That(result.Success, Is.True);
        _ipServiceMock.Verify(i => i.ReturnIpAddress("192.168.1.100"), Times.Once);
    }

    [Test]
    public async Task GetAllFixedClients_HandlesDeviceFailure()
    {
        // Arrange
        var fixedClients = new List<UniClient>
        {
            new() { Id = "1", Name = "Client1", Mac = "00:11:22:33:44:55", UseFixedIp = true }
        };

        _unifiClientMock.Setup(c => c.ExecuteRequest(
            It.Is<Url>(u => u.ToString().Contains("user")),
            It.IsAny<Func<IFlurlRequest, Task<UniResponse<List<UniClient>>>>>(),
            false))
            .ReturnsAsync(new ServiceResult<List<UniClient>> { Success = true, Data = fixedClients });

        _unifiClientMock.Setup(c => c.ExecuteRequest(
            It.Is<Url>(u => u.ToString().Contains("device")),
            It.IsAny<Func<IFlurlRequest, Task<UniResponse<List<UniDevice>>>>>(),
            false))
            .ReturnsAsync(new ServiceResult<List<UniDevice>> { Success = false, Errors = new List<string> { "Device fetch failed" } });

        // Act
        var result = await _service.GetAllFixedClients();

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Errors, Contains.Item("Device fetch failed"));
    }

    [Test]
    public async Task CreateClient_NetworkNotFound()
    {
        // Arrange
        var newClientRequest = new NewClientRequest
        {
            MacAddress = "00:11:22:33:44:55",
            Name = "TestClient",
            Hostname = "testclient",
            IpAddress = "192.168.1.100",
            SyncDns = true,
            StaticIp = true,
            Network = "NONEXISTENT"
        };

        // Mock the mac address check - Data = false indicates MAC does not exist.
        _unifiClientMock.Setup(c => c.ExecuteRequest(
            It.Is<Url>(u => u.ToString().Contains("user") && u.QueryParams.Any(q => q.Name == "mac")),
            It.IsAny<Func<IFlurlRequest, Task<bool>>>(),
            false))
            .ReturnsAsync(new ServiceResult<bool> { Success = true, Data = false });

        _unifiClientMock.Setup(c => c.ExecuteRequest(
            It.Is<Url>(u => u.ToString().Contains("networkconf")),
            It.IsAny<Func<IFlurlRequest, Task<UniResponse<List<UnifiNetwork>>>>>(),
            false))
            .ReturnsAsync(new ServiceResult<List<UnifiNetwork>> { Success = true, Data = new List<UnifiNetwork>() });
        // Act
        var result = await _service.CreateClient(newClientRequest);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Errors, Contains.Item("Network could not be found. NONEXISTENT"));
    }

    [Test]
    public async Task UpdateClient_ClientNotFound()
    {
        // Arrange
        var mac = "00:11:22:33:44:55";
        var editRequest = new EditClientRequest
        {
            Name = "UpdatedClient",
            Hostname = "updatedclient"
        };

        _unifiClientMock.Setup(c => c.ExecuteRequest(
            It.IsAny<Url>(),
            It.IsAny<Func<IFlurlRequest, Task<UniResponse<List<UniClient>>>>>(),
            false))
            .ReturnsAsync(new ServiceResult<List<UniClient>> { Success = true, Data = new List<UniClient>() });

        // Act
        var result = await _service.UpdateClient(mac, editRequest);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Errors, Contains.Item("Update Failed"));
    }
}
