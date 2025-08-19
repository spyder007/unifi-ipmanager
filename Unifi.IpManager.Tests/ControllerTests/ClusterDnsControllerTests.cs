using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using Unifi.IpManager.Controllers;
using Unifi.IpManager.Models.Dns;
using Unifi.IpManager.Models.DTO;
using Unifi.IpManager.Services;

namespace Unifi.IpManager.Tests.ControllerTests;

public class ClusterDnsControllerTests
{
    private Mock<IClusterDnsService> _clusterDnsServiceMock = null!;
    private ClusterDnsController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _clusterDnsServiceMock = new Mock<IClusterDnsService>();
        _controller = new ClusterDnsController(_clusterDnsServiceMock.Object);
    }

    [Test]
    public async Task Get_ReturnsClusterDns()
    {
        // Arrange
        var clusterName = "test-cluster";
        var zoneName = "example.com";
        var clusterDns = new ClusterDns
        {
            Name = clusterName,
            ZoneName = zoneName,
            ControlPlane = new List<HostDnsRecord>
            {
                new() { Id = "1", Hostname = "cp-test-cluster.example.com", IpAddress = "192.168.1.10", RecordType = "A" }
            },
            Traffic = new List<HostDnsRecord>
            {
                new() { Id = "2", Hostname = "tfx-test-cluster.example.com", IpAddress = "192.168.1.20", RecordType = "A" }
            }
        };

        _clusterDnsServiceMock.Setup(s => s.GetClusterDns(clusterName, zoneName))
            .ReturnsAsync(new ServiceResult<ClusterDns> { Success = true, Data = clusterDns });

        // Act
        var result = await _controller.Get(clusterName, zoneName);

        // Assert
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value?.Success, Is.True);
        Assert.That(result.Value?.Data?.Name, Is.EqualTo(clusterName));
        Assert.That(result.Value?.Data?.ZoneName, Is.EqualTo(zoneName));
        Assert.That(result.Value?.Data?.ControlPlane, Has.Count.EqualTo(1));
        Assert.That(result.Value?.Data?.Traffic, Has.Count.EqualTo(1));

        _clusterDnsServiceMock.Verify(s => s.GetClusterDns(clusterName, zoneName), Times.Once);
    }

    [Test]
    public async Task Get_ReturnsFailureResult()
    {
        // Arrange
        var clusterName = "nonexistent-cluster";
        var zoneName = "example.com";

        _clusterDnsServiceMock.Setup(s => s.GetClusterDns(clusterName, zoneName))
            .ReturnsAsync(new ServiceResult<ClusterDns> 
            { 
                Success = false, 
                Errors = new List<string> { "Cluster not found" } 
            });

        // Act
        var result = await _controller.Get(clusterName, zoneName);

        // Assert
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value?.Success, Is.False);
        Assert.That(result.Value?.Errors, Contains.Item("Cluster not found"));

        _clusterDnsServiceMock.Verify(s => s.GetClusterDns(clusterName, zoneName), Times.Once);
    }

    [Test]
    public async Task Get_HandlesNullZone()
    {
        // Arrange
        var clusterName = "test-cluster";
        string? zoneName = null;
        var clusterDns = new ClusterDns
        {
            Name = clusterName,
            ZoneName = "local",
            ControlPlane = new List<HostDnsRecord>(),
            Traffic = new List<HostDnsRecord>()
        };

        _clusterDnsServiceMock.Setup(s => s.GetClusterDns(clusterName, zoneName))
            .ReturnsAsync(new ServiceResult<ClusterDns> { Success = true, Data = clusterDns });

        // Act
        var result = await _controller.Get(clusterName, zoneName);

        // Assert
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value?.Success, Is.True);
        Assert.That(result.Value?.Data?.Name, Is.EqualTo(clusterName));

        _clusterDnsServiceMock.Verify(s => s.GetClusterDns(clusterName, zoneName), Times.Once);
    }

    [Test]
    public async Task Get_HandlesEmptyZone()
    {
        // Arrange
        var clusterName = "test-cluster";
        var zoneName = "";
        var clusterDns = new ClusterDns
        {
            Name = clusterName,
            ZoneName = "local",
            ControlPlane = new List<HostDnsRecord>(),
            Traffic = new List<HostDnsRecord>()
        };

        _clusterDnsServiceMock.Setup(s => s.GetClusterDns(clusterName, zoneName))
            .ReturnsAsync(new ServiceResult<ClusterDns> { Success = true, Data = clusterDns });

        // Act
        var result = await _controller.Get(clusterName, zoneName);

        // Assert
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value?.Success, Is.True);

        _clusterDnsServiceMock.Verify(s => s.GetClusterDns(clusterName, zoneName), Times.Once);
    }

    [Test]
    public async Task Put_UpdatesClusterDns()
    {
        // Arrange
        var clusterName = "update-cluster";
        var incomingCluster = new ClusterDns
        {
            Name = "original-name", // This should be overwritten by route parameter
            ZoneName = "example.com",
            ControlPlane = new List<HostDnsRecord>
            {
                new() { Id = "1", Hostname = "cp-update-cluster.example.com", IpAddress = "192.168.1.15", RecordType = "A" }
            },
            Traffic = new List<HostDnsRecord>
            {
                new() { Id = "2", Hostname = "tfx-update-cluster.example.com", IpAddress = "192.168.1.25", RecordType = "A" }
            }
        };

        var updatedCluster = new ClusterDns
        {
            Name = clusterName,
            ZoneName = "example.com",
            ControlPlane = incomingCluster.ControlPlane,
            Traffic = incomingCluster.Traffic
        };

        _clusterDnsServiceMock.Setup(s => s.UpdateClusterDns(It.Is<ClusterDns>(c => c.Name == clusterName)))
            .ReturnsAsync(new ServiceResult<ClusterDns> { Success = true, Data = updatedCluster });

        // Act
        var result = await _controller.Put(clusterName, incomingCluster);

        // Assert
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value?.Success, Is.True);
        Assert.That(result.Value?.Data?.Name, Is.EqualTo(clusterName));

        // Verify that the name was set from the route parameter
        Assert.That(incomingCluster.Name, Is.EqualTo(clusterName));

        _clusterDnsServiceMock.Verify(s => s.UpdateClusterDns(It.Is<ClusterDns>(c => 
            c.Name == clusterName && 
            c.ZoneName == "example.com")), Times.Once);
    }

    [Test]
    public async Task Put_ReturnsFailureWhenUpdateFails()
    {
        // Arrange
        var clusterName = "fail-cluster";
        var incomingCluster = new ClusterDns
        {
            Name = "original-name",
            ZoneName = "example.com",
            ControlPlane = new List<HostDnsRecord>(),
            Traffic = new List<HostDnsRecord>()
        };

        _clusterDnsServiceMock.Setup(s => s.UpdateClusterDns(It.IsAny<ClusterDns>()))
            .ReturnsAsync(new ServiceResult<ClusterDns> 
            { 
                Success = false, 
                Errors = new List<string> { "Update failed" } 
            });

        // Act
        var result = await _controller.Put(clusterName, incomingCluster);

        // Assert
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value?.Success, Is.False);
        Assert.That(result.Value?.Errors, Contains.Item("Update failed"));

        // Verify the name was still set from route parameter
        Assert.That(incomingCluster.Name, Is.EqualTo(clusterName));

        _clusterDnsServiceMock.Verify(s => s.UpdateClusterDns(It.Is<ClusterDns>(c => c.Name == clusterName)), Times.Once);
    }

    [Test]
    public void Put_HandlesNullIncomingCluster()
    {
        // Arrange
        var clusterName = "test-cluster";
        ClusterDns? incomingCluster = null;

        _clusterDnsServiceMock.Setup(s => s.UpdateClusterDns(It.IsAny<ClusterDns>()))
            .ReturnsAsync(new ServiceResult<ClusterDns> 
            { 
                Success = false, 
                Errors = new List<string> { "Invalid cluster data" } 
            });

        // Act & Assert
        Assert.ThrowsAsync<NullReferenceException>(async () => await _controller.Put(clusterName, incomingCluster));
    }

    [Test]
    public async Task Post_CreatesNewClusterDns()
    {
        // Arrange
        var newRequest = new NewClusterRequest
        {
            Name = "new-cluster",
            ZoneName = "example.com",
            ControlPlaneIps = new List<string> { "192.168.1.10", "192.168.1.11" },
            TrafficIps = new List<string> { "192.168.1.20", "192.168.1.21" }
        };

        var createdCluster = new ClusterDns
        {
            Name = "new-cluster",
            ZoneName = "example.com",
            ControlPlane = new List<HostDnsRecord>
            {
                new() { Id = "1", Hostname = "cp-new-cluster.example.com", IpAddress = "192.168.1.10", RecordType = "A" },
                new() { Id = "2", Hostname = "cp-new-cluster.example.com", IpAddress = "192.168.1.11", RecordType = "A" }
            },
            Traffic = new List<HostDnsRecord>
            {
                new() { Id = "3", Hostname = "tfx-new-cluster.example.com", IpAddress = "192.168.1.20", RecordType = "A" },
                new() { Id = "4", Hostname = "tfx-new-cluster.example.com", IpAddress = "192.168.1.21", RecordType = "A" }
            }
        };

        _clusterDnsServiceMock.Setup(s => s.CreateClusterDns(newRequest))
            .ReturnsAsync(new ServiceResult<ClusterDns> { Success = true, Data = createdCluster });

        // Act
        var result = await _controller.Post(newRequest);

        // Assert
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value?.Success, Is.True);
        Assert.That(result.Value?.Data?.Name, Is.EqualTo("new-cluster"));
        Assert.That(result.Value?.Data?.ControlPlane, Has.Count.EqualTo(2));
        Assert.That(result.Value?.Data?.Traffic, Has.Count.EqualTo(2));

        _clusterDnsServiceMock.Verify(s => s.CreateClusterDns(newRequest), Times.Once);
    }

    [Test]
    public async Task Post_ReturnsFailureWhenCreationFails()
    {
        // Arrange
        var newRequest = new NewClusterRequest
        {
            Name = "fail-cluster",
            ZoneName = "example.com",
            ControlPlaneIps = new List<string> { "192.168.1.10" },
            TrafficIps = new List<string> { "192.168.1.20" }
        };

        _clusterDnsServiceMock.Setup(s => s.CreateClusterDns(newRequest))
            .ReturnsAsync(new ServiceResult<ClusterDns> 
            { 
                Success = false, 
                Errors = new List<string> { "Creation failed" } 
            });

        // Act
        var result = await _controller.Post(newRequest);

        // Assert
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value?.Success, Is.False);
        Assert.That(result.Value?.Errors, Contains.Item("Creation failed"));

        _clusterDnsServiceMock.Verify(s => s.CreateClusterDns(newRequest), Times.Once);
    }

    [Test]
    public async Task Post_HandlesNullRequest()
    {
        // Arrange
        NewClusterRequest? newRequest = null;

        _clusterDnsServiceMock.Setup(s => s.CreateClusterDns(It.IsAny<NewClusterRequest>()))
            .ReturnsAsync(new ServiceResult<ClusterDns> 
            { 
                Success = false, 
                Errors = new List<string> { "Invalid request" } 
            });

        // Act
        var result = await _controller.Post(newRequest);

        // Assert
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value?.Success, Is.False);

        _clusterDnsServiceMock.Verify(s => s.CreateClusterDns(newRequest), Times.Once);
    }

    [Test]
    public async Task Post_HandlesEmptyIpLists()
    {
        // Arrange
        var newRequest = new NewClusterRequest
        {
            Name = "empty-cluster",
            ZoneName = "example.com",
            ControlPlaneIps = new List<string>(),
            TrafficIps = new List<string>()
        };

        var createdCluster = new ClusterDns
        {
            Name = "empty-cluster",
            ZoneName = "example.com",
            ControlPlane = new List<HostDnsRecord>(),
            Traffic = new List<HostDnsRecord>()
        };

        _clusterDnsServiceMock.Setup(s => s.CreateClusterDns(newRequest))
            .ReturnsAsync(new ServiceResult<ClusterDns> { Success = true, Data = createdCluster });

        // Act
        var result = await _controller.Post(newRequest);

        // Assert
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value?.Success, Is.True);
        Assert.That(result.Value?.Data?.ControlPlane, Is.Empty);
        Assert.That(result.Value?.Data?.Traffic, Is.Empty);

        _clusterDnsServiceMock.Verify(s => s.CreateClusterDns(newRequest), Times.Once);
    }

    [Test]
    public async Task Put_OverwritesNameRegardlessOfInput()
    {
        // Arrange
        var routeClusterName = "route-cluster";
        var incomingCluster = new ClusterDns
        {
            Name = "different-name",
            ZoneName = "example.com",
            ControlPlane = new List<HostDnsRecord>(),
            Traffic = new List<HostDnsRecord>()
        };

        _clusterDnsServiceMock.Setup(s => s.UpdateClusterDns(It.IsAny<ClusterDns>()))
            .ReturnsAsync(new ServiceResult<ClusterDns> { Success = true, Data = incomingCluster });

        // Act
        await _controller.Put(routeClusterName, incomingCluster);

        // Assert
        Assert.That(incomingCluster.Name, Is.EqualTo(routeClusterName));
        _clusterDnsServiceMock.Verify(s => s.UpdateClusterDns(It.Is<ClusterDns>(c => c.Name == routeClusterName)), Times.Once);
    }

    [Test]
    public async Task Get_HandlesSpecialCharactersInName()
    {
        // Arrange
        var clusterName = "test-cluster_123";
        var zoneName = "sub.example.com";

        _clusterDnsServiceMock.Setup(s => s.GetClusterDns(clusterName, zoneName))
            .ReturnsAsync(new ServiceResult<ClusterDns> 
            { 
                Success = true, 
                Data = new ClusterDns { Name = clusterName, ZoneName = zoneName } 
            });

        // Act
        var result = await _controller.Get(clusterName, zoneName);

        // Assert
        Assert.That(result.Value?.Success, Is.True);
        _clusterDnsServiceMock.Verify(s => s.GetClusterDns(clusterName, zoneName), Times.Once);
    }
}
