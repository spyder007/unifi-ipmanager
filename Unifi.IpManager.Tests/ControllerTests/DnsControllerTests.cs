using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Unifi.IpManager.Controllers;
using Unifi.IpManager.Models.DTO;
using Unifi.IpManager.Services;

namespace Unifi.IpManager.Tests.ControllerTests;

public class DnsControllerTests
{
    private Mock<IUnifiDnsService> _unifiDnsServiceMock = null!;
    private Mock<ILogger<DnsController>> _loggerMock = null!;
    private DnsController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _unifiDnsServiceMock = new Mock<IUnifiDnsService>();
        _loggerMock = new Mock<ILogger<DnsController>>();
        _controller = new DnsController(_unifiDnsServiceMock.Object, _loggerMock.Object);
    }

    [Test]
    public async Task Get_ReturnsAllDnsRecords()
    {
        // Arrange
        var dnsRecords = new List<HostDnsRecord>
        {
            new() { Id = "1", Hostname = "test1.example.com", IpAddress = "192.168.1.10", RecordType = "A" },
            new() { Id = "2", Hostname = "test2.example.com", IpAddress = "192.168.1.11", RecordType = "A" }
        };

        _unifiDnsServiceMock.Setup(s => s.GetHostDnsRecords())
            .ReturnsAsync(new ServiceResult<List<HostDnsRecord>> { Success = true, Data = dnsRecords });

        // Act
        var result = await _controller.Get();

        // Assert
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value?.Success, Is.True);
        Assert.That(result.Value?.Data, Has.Count.EqualTo(2));
        Assert.That(result.Value?.Data?[0].Hostname, Is.EqualTo("test1.example.com"));
        Assert.That(result.Value?.Data?[1].Hostname, Is.EqualTo("test2.example.com"));

        _unifiDnsServiceMock.Verify(s => s.GetHostDnsRecords(), Times.Once);
    }

    [Test]
    public async Task Get_ReturnsFailureResult()
    {
        // Arrange
        _unifiDnsServiceMock.Setup(s => s.GetHostDnsRecords())
            .ReturnsAsync(new ServiceResult<List<HostDnsRecord>> 
            { 
                Success = false, 
                Errors = new List<string> { "DNS service error" } 
            });

        // Act
        var result = await _controller.Get();

        // Assert
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value?.Success, Is.False);
        Assert.That(result.Value?.Errors, Contains.Item("DNS service error"));

        _unifiDnsServiceMock.Verify(s => s.GetHostDnsRecords(), Times.Once);
    }

    [Test]
    public async Task Post_CreatesNewDnsRecord()
    {
        // Arrange
        var hostRecord = new HostDnsRecord
        {
            Hostname = "newhost.example.com",
            IpAddress = "192.168.1.100",
            RecordType = "A"
        };

        var createdRecord = new HostDnsRecord
        {
            Id = "new123",
            Hostname = "newhost.example.com",
            IpAddress = "192.168.1.100",
            RecordType = "A"
        };

        _unifiDnsServiceMock.Setup(s => s.CreateHostDnsRecord(hostRecord))
            .ReturnsAsync(new ServiceResult<HostDnsRecord> { Success = true, Data = createdRecord });

        // Act
        var result = await _controller.Post(hostRecord);

        // Assert
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value?.Success, Is.True);
        Assert.That(result.Value?.Data?.Id, Is.EqualTo("new123"));
        Assert.That(result.Value?.Data?.Hostname, Is.EqualTo("newhost.example.com"));

        _unifiDnsServiceMock.Verify(s => s.CreateHostDnsRecord(hostRecord), Times.Once);
    }

    [Test]
    public async Task Post_ReturnsFailureWhenCreationFails()
    {
        // Arrange
        var hostRecord = new HostDnsRecord
        {
            Hostname = "failhost.example.com",
            IpAddress = "192.168.1.100",
            RecordType = "A"
        };

        _unifiDnsServiceMock.Setup(s => s.CreateHostDnsRecord(hostRecord))
            .ReturnsAsync(new ServiceResult<HostDnsRecord> 
            { 
                Success = false, 
                Errors = new List<string> { "Creation failed" } 
            });

        // Act
        var result = await _controller.Post(hostRecord);

        // Assert
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value?.Success, Is.False);
        Assert.That(result.Value?.Errors, Contains.Item("Creation failed"));

        _unifiDnsServiceMock.Verify(s => s.CreateHostDnsRecord(hostRecord), Times.Once);
    }

    [Test]
    public async Task Put_UpdatesExistingDnsRecord()
    {
        // Arrange
        var recordId = "existing123";
        var hostRecord = new HostDnsRecord
        {
            Hostname = "updatedhost.example.com",
            IpAddress = "192.168.1.101",
            RecordType = "A"
        };

        var updatedRecord = new HostDnsRecord
        {
            Id = recordId,
            Hostname = "updatedhost.example.com",
            IpAddress = "192.168.1.101",
            RecordType = "A"
        };

        _unifiDnsServiceMock.Setup(s => s.UpdateDnsHostRecord(It.Is<HostDnsRecord>(r => r.Id == recordId)))
            .ReturnsAsync(new ServiceResult<HostDnsRecord> { Success = true, Data = updatedRecord });

        // Act
        var result = await _controller.Put(recordId, hostRecord);

        // Assert
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value?.Success, Is.True);
        Assert.That(result.Value?.Data?.Id, Is.EqualTo(recordId));
        Assert.That(result.Value?.Data?.Hostname, Is.EqualTo("updatedhost.example.com"));

        // Verify that the ID was set on the host record before calling the service
        _unifiDnsServiceMock.Verify(s => s.UpdateDnsHostRecord(It.Is<HostDnsRecord>(r => 
            r.Id == recordId && 
            r.Hostname == "updatedhost.example.com" && 
            r.IpAddress == "192.168.1.101")), Times.Once);
    }

    [Test]
    public async Task Put_ReturnsFailureWhenUpdateFails()
    {
        // Arrange
        var recordId = "fail123";
        var hostRecord = new HostDnsRecord
        {
            Hostname = "failupdate.example.com",
            IpAddress = "192.168.1.101",
            RecordType = "A"
        };

        _unifiDnsServiceMock.Setup(s => s.UpdateDnsHostRecord(It.IsAny<HostDnsRecord>()))
            .ReturnsAsync(new ServiceResult<HostDnsRecord> 
            { 
                Success = false, 
                Errors = new List<string> { "Update failed" } 
            });

        // Act
        var result = await _controller.Put(recordId, hostRecord);

        // Assert
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value?.Success, Is.False);
        Assert.That(result.Value?.Errors, Contains.Item("Update failed"));

        _unifiDnsServiceMock.Verify(s => s.UpdateDnsHostRecord(It.Is<HostDnsRecord>(r => r.Id == recordId)), Times.Once);
    }

    [Test]
    public async Task DeleteClient_DeletesDnsRecord()
    {
        // Arrange
        var recordId = "delete123";

        _unifiDnsServiceMock.Setup(s => s.DeleteHostDnsRecord(recordId))
            .ReturnsAsync(new ServiceResult { Success = true });

        // Act
        var result = await _controller.DeleteClient(recordId);

        // Assert
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value?.Success, Is.True);

        _unifiDnsServiceMock.Verify(s => s.DeleteHostDnsRecord(recordId), Times.Once);
    }

    [Test]
    public async Task DeleteClient_ReturnsFailureWhenDeleteFails()
    {
        // Arrange
        var recordId = "faildelete123";

        _unifiDnsServiceMock.Setup(s => s.DeleteHostDnsRecord(recordId))
            .ReturnsAsync(new ServiceResult 
            { 
                Success = false, 
                Errors = new List<string> { "Delete failed" } 
            });

        // Act
        var result = await _controller.DeleteClient(recordId);

        // Assert
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value?.Success, Is.False);
        Assert.That(result.Value?.Errors, Contains.Item("Delete failed"));

        _unifiDnsServiceMock.Verify(s => s.DeleteHostDnsRecord(recordId), Times.Once);
    }

    [Test]
    public async Task Get_LogsTraceMessage()
    {
        // Arrange
        _unifiDnsServiceMock.Setup(s => s.GetHostDnsRecords())
            .ReturnsAsync(new ServiceResult<List<HostDnsRecord>> { Success = true, Data = new List<HostDnsRecord>() });

        // Act
        await _controller.Get();

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Processing request for all DNS records")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task Post_LogsTraceMessage()
    {
        // Arrange
        var hostRecord = new HostDnsRecord { Hostname = "test.com", IpAddress = "1.1.1.1", RecordType = "A" };
        _unifiDnsServiceMock.Setup(s => s.CreateHostDnsRecord(It.IsAny<HostDnsRecord>()))
            .ReturnsAsync(new ServiceResult<HostDnsRecord> { Success = true, Data = hostRecord });

        // Act
        await _controller.Post(hostRecord);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Processing request for new Dns Record")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task Put_LogsTraceMessage()
    {
        // Arrange
        var hostRecord = new HostDnsRecord { Hostname = "test.com", IpAddress = "1.1.1.1", RecordType = "A" };
        _unifiDnsServiceMock.Setup(s => s.UpdateDnsHostRecord(It.IsAny<HostDnsRecord>()))
            .ReturnsAsync(new ServiceResult<HostDnsRecord> { Success = true, Data = hostRecord });

        // Act
        await _controller.Put("test123", hostRecord);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Processing request for update dns record")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task DeleteClient_LogsTraceMessage()
    {
        // Arrange
        _unifiDnsServiceMock.Setup(s => s.DeleteHostDnsRecord(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResult { Success = true });

        // Act
        await _controller.DeleteClient("test123");

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Processing request for delete dns record")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task Put_SetsIdOnHostRecord()
    {
        // Arrange
        var recordId = "setid123";
        var hostRecord = new HostDnsRecord
        {
            Id = "originalId", // This should be overwritten
            Hostname = "test.example.com",
            IpAddress = "192.168.1.100",
            RecordType = "A"
        };

        _unifiDnsServiceMock.Setup(s => s.UpdateDnsHostRecord(It.IsAny<HostDnsRecord>()))
            .ReturnsAsync(new ServiceResult<HostDnsRecord> { Success = true, Data = hostRecord });

        // Act
        await _controller.Put(recordId, hostRecord);

        // Assert - Verify the ID was set to the route parameter value
        Assert.That(hostRecord.Id, Is.EqualTo(recordId));
        _unifiDnsServiceMock.Verify(s => s.UpdateDnsHostRecord(It.Is<HostDnsRecord>(r => r.Id == recordId)), Times.Once);
    }

    [Test]
    public async Task Get_HandlesEmptyResults()
    {
        // Arrange
        _unifiDnsServiceMock.Setup(s => s.GetHostDnsRecords())
            .ReturnsAsync(new ServiceResult<List<HostDnsRecord>> { Success = true, Data = new List<HostDnsRecord>() });

        // Act
        var result = await _controller.Get();

        // Assert
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value?.Success, Is.True);
        Assert.That(result.Value?.Data, Is.Not.Null);
        Assert.That(result.Value?.Data, Has.Count.EqualTo(0));
    }

    [Test]
    public async Task Post_HandlesNullInput()
    {
        // Arrange
        _unifiDnsServiceMock.Setup(s => s.CreateHostDnsRecord(null))
            .ReturnsAsync(new ServiceResult<HostDnsRecord> 
            { 
                Success = false, 
                Errors = new List<string> { "Invalid input" } 
            });

        // Act
        var result = await _controller.Post(null);

        // Assert
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value?.Success, Is.False);

        _unifiDnsServiceMock.Verify(s => s.CreateHostDnsRecord(null), Times.Once);
    }
}
