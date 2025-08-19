using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Unifi.IpManager.Models.Dns;
using Unifi.IpManager.Models.DTO;
using Unifi.IpManager.Services;

namespace Unifi.IpManager.Tests.ServiceTests;

public class ClusterDnsServiceTests
{
    private Mock<IUnifiDnsService> _unifiDnsServiceMock = null!;
    private Mock<ILogger<ClusterDnsService>> _loggerMock = null!;
    private ClusterDnsService _service = null!;

    [SetUp]
    public void Setup()
    {
        _unifiDnsServiceMock = new Mock<IUnifiDnsService>();
        _loggerMock = new Mock<ILogger<ClusterDnsService>>();
        _service = new ClusterDnsService(_unifiDnsServiceMock.Object, _loggerMock.Object);
    }

    [Test]
    public async Task CreateClusterDns_Success()
    {
        // Arrange
        var newClusterRequest = new NewClusterRequest
        {
            Name = "test-cluster",
            ZoneName = "example.com",
            ControlPlaneIps = new List<string> { "192.168.1.10", "192.168.1.11" },
            TrafficIps = new List<string> { "192.168.1.20", "192.168.1.21" }
        };

        var createdRecords = new List<HostDnsRecord>
        {
            new() { Id = "1", Hostname = "cp-test-cluster.example.com", IpAddress = "192.168.1.10", RecordType = "A" },
            new() { Id = "2", Hostname = "cp-test-cluster.example.com", IpAddress = "192.168.1.11", RecordType = "A" },
            new() { Id = "3", Hostname = "tfx-test-cluster.example.com", IpAddress = "192.168.1.20", RecordType = "A" },
            new() { Id = "4", Hostname = "tfx-test-cluster.example.com", IpAddress = "192.168.1.21", RecordType = "A" }
        };

        _unifiDnsServiceMock.Setup(s => s.CreateHostDnsRecord(It.IsAny<HostDnsRecord>()))
            .ReturnsAsync(new ServiceResult<HostDnsRecord> { Success = true, Data = new HostDnsRecord() });

        _unifiDnsServiceMock.Setup(s => s.GetHostDnsRecords())
            .ReturnsAsync(new ServiceResult<List<HostDnsRecord>> { Success = true, Data = createdRecords });

        // Act
        var result = await _service.CreateClusterDns(newClusterRequest);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.Name, Is.EqualTo("test-cluster"));
        Assert.That(result.Data.ZoneName, Is.EqualTo("example.com"));
        Assert.That(result.Data.ControlPlane, Has.Count.EqualTo(2));
        Assert.That(result.Data.Traffic, Has.Count.EqualTo(2));

        // Verify that CreateHostDnsRecord was called 4 times (2 control plane + 2 traffic)
        _unifiDnsServiceMock.Verify(s => s.CreateHostDnsRecord(It.IsAny<HostDnsRecord>()), Times.Exactly(4));
    }

    [Test]
    public async Task CreateClusterDns_PartialFailure()
    {
        // Arrange
        var newClusterRequest = new NewClusterRequest
        {
            Name = "test-cluster",
            ZoneName = "example.com",
            ControlPlaneIps = new List<string> { "192.168.1.10" },
            TrafficIps = new List<string> { "192.168.1.20" }
        };

        _unifiDnsServiceMock.SetupSequence(s => s.CreateHostDnsRecord(It.IsAny<HostDnsRecord>()))
            .ReturnsAsync(new ServiceResult<HostDnsRecord> { Success = true, Data = new HostDnsRecord() })
            .ReturnsAsync(new ServiceResult<HostDnsRecord> { Success = false, Errors = new List<string> { "Creation failed" } });

        // Act
        var result = await _service.CreateClusterDns(newClusterRequest);

        // Assert
        Assert.That(result.Success, Is.False);
        _unifiDnsServiceMock.Verify(s => s.CreateHostDnsRecord(It.IsAny<HostDnsRecord>()), Times.Exactly(2));
    }

    [Test]
    public async Task CreateClusterDns_ExceptionHandling()
    {
        // Arrange
        var newClusterRequest = new NewClusterRequest
        {
            Name = "test-cluster",
            ZoneName = "example.com",
            ControlPlaneIps = new List<string> { "192.168.1.10" },
            TrafficIps = new List<string> { "192.168.1.20" }
        };

        _unifiDnsServiceMock.Setup(s => s.CreateHostDnsRecord(It.IsAny<HostDnsRecord>()))
            .ThrowsAsync(new Exception("DNS service error"));

        // Act
        var result = await _service.CreateClusterDns(newClusterRequest);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Errors, Contains.Item("DNS service error"));
    }

    [Test]
    public async Task GetClusterDns_Success()
    {
        // Arrange
        var name = "test-cluster";
        var zone = "example.com";
        var hostRecords = new List<HostDnsRecord>
        {
            new() { Id = "1", Hostname = "cp-test-cluster.example.com", IpAddress = "192.168.1.10", RecordType = "A" },
            new() { Id = "2", Hostname = "tfx-test-cluster.example.com", IpAddress = "192.168.1.20", RecordType = "A" },
            new() { Id = "3", Hostname = "other-service.example.com", IpAddress = "192.168.1.30", RecordType = "A" }
        };

        _unifiDnsServiceMock.Setup(s => s.GetHostDnsRecords())
            .ReturnsAsync(new ServiceResult<List<HostDnsRecord>> { Success = true, Data = hostRecords });

        // Act
        var result = await _service.GetClusterDns(name, zone);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.Name, Is.EqualTo(name));
        Assert.That(result.Data.ZoneName, Is.EqualTo(zone));
        Assert.That(result.Data.ControlPlane, Has.Count.EqualTo(1));
        Assert.That(result.Data.Traffic, Has.Count.EqualTo(1));
        Assert.That(result.Data.ControlPlane[0].Hostname, Is.EqualTo("cp-test-cluster.example.com"));
        Assert.That(result.Data.Traffic[0].Hostname, Is.EqualTo("tfx-test-cluster.example.com"));
    }

    [Test]
    public async Task GetClusterDns_NoZoneProvided_ExtractsFromHostname()
    {
        // Arrange
        var name = "test-cluster";
        var zone = "";
        var hostRecords = new List<HostDnsRecord>
        {
            new() { Id = "1", Hostname = "cp-test-cluster.example.com", IpAddress = "192.168.1.10", RecordType = "A" }
        };

        _unifiDnsServiceMock.Setup(s => s.GetHostDnsRecords())
            .ReturnsAsync(new ServiceResult<List<HostDnsRecord>> { Success = true, Data = hostRecords });

        // Act
        var result = await _service.GetClusterDns(name, zone);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.ZoneName, Is.EqualTo("example.com"));
    }

    [Test]
    public async Task GetClusterDns_ExceptionHandling()
    {
        // Arrange
        _unifiDnsServiceMock.Setup(s => s.GetHostDnsRecords())
            .ReturnsAsync(new ServiceResult<List<HostDnsRecord>> { Success = false, Errors = new List<string> { "DNS fetch error" } });

        // Act
        var result = await _service.GetClusterDns("test-cluster", "example.com");

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Errors, Contains.Item("DNS fetch error"));
    }

        [Test]
    public async Task GetClusterDns_FetchErrorsHandling()
    {
        // Arrange
        _unifiDnsServiceMock.Setup(s => s.GetHostDnsRecords())
            .ThrowsAsync(new Exception("DNS fetch error"));

        // Act
        var result = await _service.GetClusterDns("test-cluster", "example.com");

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Errors, Contains.Item("DNS fetch error"));
    }

    [Test]
    public async Task UpdateClusterDns_Success()
    {
        // Arrange
        var clusterDns = new ClusterDns
        {
            Name = "test-cluster",
            ZoneName = "example.com",
            ControlPlane = new List<HostDnsRecord>
            {
                new() { Id = "1", Hostname = "cp-test-cluster.example.com", IpAddress = "192.168.1.10", RecordType = "A" },
                new() { Hostname = "cp-test-cluster.example.com", IpAddress = "192.168.1.12", RecordType = "A" } // New record without ID
            },
            Traffic = new List<HostDnsRecord>
            {
                new() { Id = "2", Hostname = "tfx-test-cluster.example.com", IpAddress = "192.168.1.21", RecordType = "A" } // Updated IP
            }
        };

        var existingRecords = new List<HostDnsRecord>
        {
            new() { Id = "1", Hostname = "cp-test-cluster.example.com", IpAddress = "192.168.1.10", RecordType = "A" },
            new() { Id = "2", Hostname = "tfx-test-cluster.example.com", IpAddress = "192.168.1.20", RecordType = "A" },
            new() { Id = "3", Hostname = "tfx-test-cluster.example.com", IpAddress = "192.168.1.22", RecordType = "A" } // To be deleted
        };

        _unifiDnsServiceMock.Setup(s => s.GetHostDnsRecords())
            .ReturnsAsync(new ServiceResult<List<HostDnsRecord>> { Success = true, Data = existingRecords });

        _unifiDnsServiceMock.Setup(s => s.CreateHostDnsRecord(It.IsAny<HostDnsRecord>()))
            .ReturnsAsync(new ServiceResult<HostDnsRecord> { Success = true, Data = new HostDnsRecord() });

        _unifiDnsServiceMock.Setup(s => s.UpdateDnsHostRecord(It.IsAny<HostDnsRecord>()))
            .ReturnsAsync(new ServiceResult<HostDnsRecord> { Success = true, Data = new HostDnsRecord() });

        _unifiDnsServiceMock.Setup(s => s.DeleteHostDnsRecord(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResult { Success = true });

        // Act
        var result = await _service.UpdateClusterDns(clusterDns);

        // Assert
        Assert.That(result.Success, Is.True);

        // Verify operations were called correctly
        _unifiDnsServiceMock.Verify(s => s.CreateHostDnsRecord(It.IsAny<HostDnsRecord>()), Times.Once); // New record
        _unifiDnsServiceMock.Verify(s => s.UpdateDnsHostRecord(It.IsAny<HostDnsRecord>()), Times.Exactly(2)); // Updated records
        _unifiDnsServiceMock.Verify(s => s.DeleteHostDnsRecord("3"), Times.Once); // Deleted record
    }

    [Test]
    public async Task UpdateClusterDns_ClusterNotExists()
    {
        // Arrange
        var clusterDns = new ClusterDns
        {
            Name = "nonexistent-cluster",
            ZoneName = "example.com",
            ControlPlane = new List<HostDnsRecord>(),
            Traffic = new List<HostDnsRecord>()
        };

        _unifiDnsServiceMock.Setup(s => s.GetHostDnsRecords())
            .ReturnsAsync(new ServiceResult<List<HostDnsRecord>> { Success = true, Data = new List<HostDnsRecord>() });

        // Act
        var result = await _service.UpdateClusterDns(clusterDns);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Errors, Contains.Item("Cluster nonexistent-cluster does not exist."));
    }

    [Test]
    public async Task UpdateClusterDns_PartialFailure()
    {
        var expectedError = "Update failed";
        // Arrange
        var clusterDns = new ClusterDns
        {
            Name = "test-cluster",
            ZoneName = "example.com",
            ControlPlane = new List<HostDnsRecord>
            {
                new() { Id = "1", Hostname = "cp-test-cluster.example.com", IpAddress = "192.168.1.10", RecordType = "A" }
            },
            Traffic = new List<HostDnsRecord>()
        };

        var existingRecords = new List<HostDnsRecord>
        {
            new() { Id = "1", Hostname = "cp-test-cluster.example.com", IpAddress = "192.168.1.10", RecordType = "A" }
        };

        _unifiDnsServiceMock.Setup(s => s.GetHostDnsRecords())
            .ReturnsAsync(new ServiceResult<List<HostDnsRecord>> { Success = true, Data = existingRecords });

        _unifiDnsServiceMock.Setup(s => s.UpdateDnsHostRecord(It.IsAny<HostDnsRecord>()))
            .ReturnsAsync(new ServiceResult<HostDnsRecord> { Success = false, Errors = new List<string> { expectedError } });

        // Act
        var result = await _service.UpdateClusterDns(clusterDns);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Errors, Contains.Item(expectedError));
    }

    [Test]
    public async Task UpdateClusterDns_ExceptionHandling()
    {
        // Arrange
        var clusterDns = new ClusterDns
        {
            Name = "test-cluster",
            ZoneName = "example.com",
            ControlPlane = new List<HostDnsRecord>(),
            Traffic = new List<HostDnsRecord>()
        };

        _unifiDnsServiceMock.Setup(s => s.GetHostDnsRecords())
            .ThrowsAsync(new Exception("Update error"));

        // Act
        var result = await _service.UpdateClusterDns(clusterDns);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Errors, Contains.Item($"Cluster {clusterDns.Name} does not exist."));
    }
}
