using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Unifi.IpManager.Extensions;
using Unifi.IpManager.Models.DTO;
using Unifi.IpManager.Models.Unifi;
using Unifi.IpManager.Services;

namespace Unifi.IpManager.Tests.ServiceTests;

public class UnifiDnsServiceTests
{
    private Mock<ILogger<UnifiDnsService>> _loggerMock = null!;
    private Mock<IUnifiClient> _unifiClientMock = null!;
    private UnifiDnsService _service = null!;
    private Url _baseApiUrlV2 = null!;
    private string _siteId = null!;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<UnifiDnsService>>();
        _unifiClientMock = new Mock<IUnifiClient>();
        _baseApiUrlV2 = "https://test.com/api/v2";
        _siteId = "default";

        _unifiClientMock.Setup(c => c.BaseApiUrlV2).Returns(_baseApiUrlV2);
        _unifiClientMock.Setup(c => c.SiteId).Returns(_siteId);

        _service = new UnifiDnsService(_loggerMock.Object, _unifiClientMock.Object);
    }

    [Test]
    public async Task GetHostDnsRecords_ReturnsAllRecords()
    {
        // Arrange
        var deviceRecords = new List<UniDeviceDnsRecord>
        {
            new() { Hostname = "device1", IpAddress = "192.168.1.100", MacAddress = "00:11:22:33:44:55" }
        };
        var staticRecords = new List<UniHostRecord>
        {
            new() { Id = "1", Key = "host1", Value = "192.168.1.101", RecordType = "A" }
        };

        _unifiClientMock.Setup(c => c.ExecuteRequest(
            It.IsAny<Url>(),
            It.IsAny<Func<IFlurlRequest, Task<List<UniDeviceDnsRecord>>>>(),
            false))
            .ReturnsAsync(new ServiceResult<List<UniDeviceDnsRecord>> { Success = true, Data = deviceRecords });

        _unifiClientMock.Setup(c => c.ExecuteRequest(
            It.IsAny<Url>(),
            It.IsAny<Func<IFlurlRequest, Task<List<UniHostRecord>>>>(),
            false))
            .ReturnsAsync(new ServiceResult<List<UniHostRecord>> { Success = true, Data = staticRecords });

        // Act
        var result = await _service.GetHostDnsRecords();

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Has.Count.EqualTo(2));
        Assert.That(result.Data.Any(r => r.Hostname == "device1" && r.DeviceLock), Is.True);
        Assert.That(result.Data.Any(r => r.Hostname == "host1" && !r.DeviceLock), Is.True);
    }

    [Test]
    public async Task GetHostDnsRecords_HandleDeviceCallFailed()
    {
        // Arrange
        var staticRecords = new List<UniHostRecord>
        {
            new() { Id = "1", Key = "host1", Value = "192.168.1.101", RecordType = "A" }
        };

        _unifiClientMock.Setup(c => c.ExecuteRequest(
            It.IsAny<Url>(),
            It.IsAny<Func<IFlurlRequest, Task<List<UniDeviceDnsRecord>>>>(),
            false))
            .ReturnsAsync(new ServiceResult<List<UniDeviceDnsRecord>> { Success = false, Errors = { "Flurl Error" } });

        _unifiClientMock.Setup(c => c.ExecuteRequest(
            It.IsAny<Url>(),
            It.IsAny<Func<IFlurlRequest, Task<List<UniHostRecord>>>>(),
            false))
            .ReturnsAsync(new ServiceResult<List<UniHostRecord>> { Success = true, Data = staticRecords });

        // Act
        var result = await _service.GetHostDnsRecords();

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Has.Count.EqualTo(1));
        Assert.That(result.Data.Any(r => r.Hostname == "host1" && !r.DeviceLock), Is.True);
    }

    [Test]
    public async Task GetHostDnsRecords_HandleDeviceCallException()
    {
        // Arrange
        var staticRecords = new List<UniHostRecord>
        {
            new() { Id = "1", Key = "host1", Value = "192.168.1.101", RecordType = "A" }
        };

        _unifiClientMock.Setup(c => c.ExecuteRequest(
            It.IsAny<Url>(),
            It.IsAny<Func<IFlurlRequest, Task<List<UniDeviceDnsRecord>>>>(),
            false))
            .ThrowsAsync(new Exception("Flurl Error"));

        _unifiClientMock.Setup(c => c.ExecuteRequest(
            It.IsAny<Url>(),
            It.IsAny<Func<IFlurlRequest, Task<List<UniHostRecord>>>>(),
            false))
            .ReturnsAsync(new ServiceResult<List<UniHostRecord>> { Success = true, Data = staticRecords });

        // Act
        var result = await _service.GetHostDnsRecords();

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Has.Count.EqualTo(1));
        Assert.That(result.Data.Any(r => r.Hostname == "host1" && !r.DeviceLock), Is.True);
    }

    [Test]
    public async Task CreateHostDnsRecord_ReturnsCreatedRecord()
    {
        // Arrange
        var hostRecord = new HostDnsRecord
        {
            Hostname = "newhost.local",
            IpAddress = "192.168.1.200",
            RecordType = "A"
        };

        var createdRecord = new HostDnsRecord
        {
            Id = "newid",
            Hostname = "newhost.local",
            DeviceLock = false,
            MacAddress = null,
            IpAddress = "192.168.1.200",
            RecordType = "A"
        };

        _unifiClientMock.Setup(c => c.ExecuteRequest(
            It.IsAny<Url>(),
            It.IsAny<Func<IFlurlRequest, Task<HostDnsRecord>>>(),
            true))
            .ReturnsAsync(new ServiceResult<HostDnsRecord> { Success = true, Data = createdRecord });

        // Act
        var result = await _service.CreateHostDnsRecord(hostRecord);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.Id, Is.EqualTo("newid"));
        Assert.That(result.Data.Hostname, Is.EqualTo("newhost.local"));
        Assert.That(result.Data.IpAddress, Is.EqualTo("192.168.1.200"));
    }

    [Test]
    public async Task UpdateDnsHostRecord_ReturnsUpdatedRecord()
    {
        // Arrange
        var hostRecord = new HostDnsRecord
        {
            Id = "existingid",
            Hostname = "updatedhost.local",
            IpAddress = "192.168.1.201",
            RecordType = "A"
        };

        var updatedRecord = new UniHostRecord
        {
            Id = "existingid",
            Key = "updatedhost",
            Value = "192.168.1.201",
            RecordType = "A"
        };

        _unifiClientMock.Setup(c => c.ExecuteRequest(
            It.IsAny<Url>(),
            It.IsAny<Func<IFlurlRequest, Task<HostDnsRecord>>>(),
            true))
            .ReturnsAsync(new ServiceResult<HostDnsRecord> { Success = true, Data = updatedRecord.ToHostDnsRecord() });

        // Act
        var result = await _service.UpdateDnsHostRecord(hostRecord);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.Id, Is.EqualTo("existingid"));
        Assert.That(result.Data.Hostname, Is.EqualTo("updatedhost"));
        Assert.That(result.Data.IpAddress, Is.EqualTo("192.168.1.201"));
    }

    [Test]
    public async Task DeleteHostDnsRecord_ReturnsSuccess()
    {
        // Arrange
        var recordId = "deleteid";

        _unifiClientMock.Setup(c => c.ExecuteRequest(
            It.IsAny<Url>(),
            It.IsAny<Func<IFlurlRequest, Task<string>>>(),
            true))
            .ReturnsAsync(new ServiceResult<string> { Success = true, Data = string.Empty });

        // Act
        var result = await _service.DeleteHostDnsRecord(recordId);

        // Assert
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task GetHostDnsRecords_HandlesMixedSuccessFailure()
    {
        // Arrange - devices fail, static dns succeeds
        var staticRecords = new List<UniHostRecord>
        {
            new() { Id = "1",  Key = "host1.local", Value = "192.168.1.101", RecordType = "A" }
        };

        _unifiClientMock.Setup(c => c.ExecuteRequest(
            It.IsAny<Url>(),
            It.IsAny<Func<IFlurlRequest, Task<List<UniDeviceDnsRecord>>>>(),
            false))
            .ReturnsAsync(new ServiceResult<List<UniDeviceDnsRecord>> { Success = false, Data = new List<UniDeviceDnsRecord>() });

        _unifiClientMock.Setup(c => c.ExecuteRequest(
            It.IsAny<Url>(),
            It.IsAny<Func<IFlurlRequest, Task<List<UniHostRecord>>>>(),
            false))
            .ReturnsAsync(new ServiceResult<List<UniHostRecord>> { Success = true, Data = staticRecords });

        // Act
        var result = await _service.GetHostDnsRecords();

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Has.Count.EqualTo(1));
        Assert.That(result.Data[0].Hostname, Is.EqualTo("host1.local"));
    }

    [Test]
    public async Task GetHostDnsRecords_HandlesMixedSuccessFailureException()
    {
        // Arrange - devices fail, static dns succeeds
        var staticRecords = new List<UniHostRecord>
        {
            new() { Id = "1",  Key = "host1.local", Value = "192.168.1.101", RecordType = "A" }
        };

        _unifiClientMock.Setup(c => c.ExecuteRequest(
            It.IsAny<Url>(),
            It.IsAny<Func<IFlurlRequest, Task<List<UniDeviceDnsRecord>>>>(),
            false))
            .ThrowsAsync(new Exception("Flurl Error"));

        _unifiClientMock.Setup(c => c.ExecuteRequest(
            It.IsAny<Url>(),
            It.IsAny<Func<IFlurlRequest, Task<List<UniHostRecord>>>>(),
            false))
            .ReturnsAsync(new ServiceResult<List<UniHostRecord>> { Success = true, Data = staticRecords });

        // Act
        var result = await _service.GetHostDnsRecords();

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Has.Count.EqualTo(1));
        Assert.That(result.Data[0].Hostname, Is.EqualTo("host1.local"));
    }

    [Test]
    public async Task CreateHostDnsRecord_HandlesFailure()
    {
        // Arrange
        var hostRecord = new HostDnsRecord
        {
            Hostname = "failhost",
            IpAddress = "192.168.1.200",
            RecordType = "A"
        };

        _unifiClientMock.Setup(c => c.ExecuteRequest(
            It.IsAny<Url>(),
            It.IsAny<Func<IFlurlRequest, Task<HostDnsRecord>>>(),
            true))
            .ReturnsAsync(new ServiceResult<HostDnsRecord> { Success = false, Errors = new List<string> { "Creation failed" } });

        // Act
        var result = await _service.CreateHostDnsRecord(hostRecord);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Errors, Contains.Item("Creation failed"));
    }

    [Test]
    public async Task UpdateDnsHostRecord_HandlesFailure()
    {
        // Arrange
        var hostRecord = new HostDnsRecord
        {
            Id = "failid",
            Hostname = "failhost",
            IpAddress = "192.168.1.200",
            RecordType = "A"
        };

        _unifiClientMock.Setup(c => c.ExecuteRequest(
            It.IsAny<Url>(),
            It.IsAny<Func<IFlurlRequest, Task<HostDnsRecord>>>(),
            true))
            .ReturnsAsync(new ServiceResult<HostDnsRecord> { Success = false, Errors = new List<string> { "Update failed" } });

        // Act
        var result = await _service.UpdateDnsHostRecord(hostRecord);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Errors, Contains.Item("Update failed"));
    }

    [Test]
    public async Task DeleteHostDnsRecord_HandlesFailure()
    {
        // Arrange
        var recordId = "failid";

        _unifiClientMock.Setup(c => c.ExecuteRequest(
            It.IsAny<Url>(),
            It.IsAny<Func<IFlurlRequest, Task<string>>>(),
            true))
            .ReturnsAsync(new ServiceResult<string> { Success = false, Errors = new List<string> { "Delete failed" } });

        // Act
        var result = await _service.DeleteHostDnsRecord(recordId);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Errors, Contains.Item("Delete failed"));
    }
}
