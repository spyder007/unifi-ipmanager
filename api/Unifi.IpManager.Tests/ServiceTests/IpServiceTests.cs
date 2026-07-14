using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Unifi.IpManager.Options;
using Microsoft.Extensions.Options;
using Unifi.IpManager.Services;
using Unifi.IpManager.Models.Unifi;

namespace Unifi.IpManager.Tests.ServiceTests;

public class IpServiceTests
{
    private IpOptions _ipOptions = null!;
    private Mock<IDistributedCache> _cacheMock = null!;
    private Mock<ILogger<IpService>> _loggerMock = null!;
    private IOptions<IpOptions> _options = null!;
    private IpService _service = null!;

    [SetUp]
    public void Setup()
    {
        _ipOptions = new IpOptions
        {
            IpCooldownMinutes = 5
        };
        _options = Microsoft.Extensions.Options.Options.Create(_ipOptions);
        _cacheMock = new Mock<IDistributedCache>();
        _loggerMock = new Mock<ILogger<IpService>>();
        _service = new IpService(_options, _loggerMock.Object, _cacheMock.Object);
    }

    [Test]
    public async Task GetUnusedNetworkIpAddress_ReturnsUnusedIp()
    {
        // Arrange
        var network = new UnifiNetwork
        {
            Name = "TestNetwork",
            IpSubnet = "192.168.1.0/24",
            DhcpStartAddress = "192.168.1.100"
        };
        var usedIps = new List<string> { "192.168.1.10" };
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync((byte[]?)null);

        // Act
        var result = await _service.GetUnusedNetworkIpAddress(network, usedIps);

        // Assert
        Assert.That(result, Is.EqualTo("192.168.1.11"));
    }

    [Test]
    public async Task GetUnusedNetworkIpAddress_StartsAtDot10()
    {
        // Arrange
        var network = new UnifiNetwork
        {
            Name = "TestNetwork",
            IpSubnet = "192.168.1.0/24",
            DhcpStartAddress = "192.168.1.100"
        };
        var usedIps = new List<string>();
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync((byte[]?)null);

        // Act
        var result = await _service.GetUnusedNetworkIpAddress(network, usedIps);

        // Assert
        Assert.That(result, Is.EqualTo("192.168.1.10"));
    }

    [Test]
    public async Task GetUnusedNetworkIpAddress_StopsBeforeDhcpStart()
    {
        // Arrange
        var network = new UnifiNetwork
        {
            Name = "TestNetwork",
            IpSubnet = "192.168.1.0/24",
            DhcpStartAddress = "192.168.1.15"
        };
        // Use all IPs from .10 to .14
        var usedIps = new List<string> { "192.168.1.10", "192.168.1.11", "192.168.1.12", "192.168.1.13", "192.168.1.14" };
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync((byte[]?)null);

        // Act
        var result = await _service.GetUnusedNetworkIpAddress(network, usedIps);

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void GetUnusedNetworkIpAddress_ReturnsEmptyIfNetworkNull()
    {
        var usedIps = new List<string>();

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _service.GetUnusedNetworkIpAddress(null, usedIps));
    }

    [Test]
    public async Task GetUnusedNetworkIpAddress_SkipsIpInCooldown()
    {
        // Arrange
        var network = new UnifiNetwork
        {
            Name = "TestNetwork",
            IpSubnet = "192.168.1.0/24",
            DhcpStartAddress = "192.168.1.100"
        };
        var usedIps = new List<string>();
        // Simulate 192.168.1.10 is in cooldown
        _cacheMock.Setup(c => c.GetAsync("Unifi.IpManager.IpCooldown.192.168.1.10", default)).ReturnsAsync(Encoding.UTF8.GetBytes("192.168.1.10"));
        _cacheMock.Setup(c => c.GetAsync("Unifi.IpManager.IpCooldown.192.168.1.11", default)).ReturnsAsync((byte[]?)null);

        // Act
        var result = await _service.GetUnusedNetworkIpAddress(network, usedIps);

        // Assert
        Assert.That(result, Is.EqualTo("192.168.1.11"));
    }

    [Test]
    public async Task GetUnusedNetworkIpAddress_ReturnsEmptyWhenAllIpsExhausted()
    {
        // Arrange - all IPs in range are used
        var network = new UnifiNetwork
        {
            Name = "TestNetwork",
            IpSubnet = "192.168.1.0/24",
            DhcpStartAddress = "192.168.1.13"
        };
        var usedIps = new List<string> { "192.168.1.10", "192.168.1.11", "192.168.1.12" };
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync((byte[]?)null);

        // Act
        var result = await _service.GetUnusedNetworkIpAddress(network, usedIps);

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public async Task GetUnusedNetworkIpAddress_ReturnsEmptyWhenAllIpsInCooldown()
    {
        // Arrange - all IPs are in cooldown
        var network = new UnifiNetwork
        {
            Name = "TestNetwork",
            IpSubnet = "192.168.1.0/24",
            DhcpStartAddress = "192.168.1.13"
        };
        var usedIps = new List<string>();
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync(Encoding.UTF8.GetBytes("test"));

        // Act
        var result = await _service.GetUnusedNetworkIpAddress(network, usedIps);

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void GetUnusedNetworkIpAddress_HandlesNullUsedIpsList()
    {
        // Arrange
        var network = new UnifiNetwork
        {
            Name = "TestNetwork",
            IpSubnet = "192.168.1.0/24",
            DhcpStartAddress = "192.168.1.100"
        };
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync((byte[]?)null);

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _service.GetUnusedNetworkIpAddress(network, null));
    }

    [Test]
    public async Task GetUnusedNetworkIpAddress_HandlesInvalidSubnet()
    {
        // Arrange
        var network = new UnifiNetwork
        {
            Name = "TestNetwork",
            IpSubnet = "invalid",
            DhcpStartAddress = "192.168.1.100"
        };
        var usedIps = new List<string>();

        // Act
        var result = await _service.GetUnusedNetworkIpAddress(network, usedIps);

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public async Task GetUnusedNetworkIpAddress_HandlesMissingSubnet()
    {
        // Arrange
        var network = new UnifiNetwork
        {
            Name = "TestNetwork",
            IpSubnet = null,
            DhcpStartAddress = "192.168.1.100"
        };
        var usedIps = new List<string>();

        // Act
        var result = await _service.GetUnusedNetworkIpAddress(network, usedIps);

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public async Task GetUnusedNetworkIpAddress_HandlesMissingDhcpStart()
    {
        // Arrange
        var network = new UnifiNetwork
        {
            Name = "TestNetwork",
            IpSubnet = "192.168.1.0/24",
            DhcpStartAddress = null
        };
        var usedIps = new List<string>();

        // Act
        var result = await _service.GetUnusedNetworkIpAddress(network, usedIps);

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public async Task GetUnusedNetworkIpAddress_WorksWithDifferentSubnets()
    {
        // Arrange
        var network = new UnifiNetwork
        {
            Name = "TestNetwork",
            IpSubnet = "10.0.0.0/24",
            DhcpStartAddress = "10.0.0.100"
        };
        var usedIps = new List<string>();
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync((byte[]?)null);

        // Act
        var result = await _service.GetUnusedNetworkIpAddress(network, usedIps);

        // Assert
        Assert.That(result, Is.EqualTo("10.0.0.10"));
    }

    [Test]
    public async Task GetUnusedNetworkIpAddress_WorksWith23Subnet()
    {
        // Arrange - /23 subnet covers 192.168.10.0 - 192.168.11.255
        var network = new UnifiNetwork
        {
            Name = "TestNetwork",
            IpSubnet = "192.168.10.0/23",
            DhcpStartAddress = "192.168.11.200"
        };
        var usedIps = new List<string> { "192.168.10.10", "192.168.10.11" };
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync((byte[]?)null);

        // Act
        var result = await _service.GetUnusedNetworkIpAddress(network, usedIps);

        // Assert - Should find the next available IP
        Assert.That(result, Is.EqualTo("192.168.10.12"));
    }

    [Test]
    public async Task GetUnusedNetworkIpAddress_23SubnetSpansMultipleThirdOctets()
    {
        // Arrange - /23 subnet, use all IPs in 192.168.10.x range
        var network = new UnifiNetwork
        {
            Name = "TestNetwork",
            IpSubnet = "192.168.10.0/23",
            DhcpStartAddress = "192.168.11.200"
        };

        // Mark all IPs from 192.168.10.10 to 192.168.10.255 as used
        var usedIps = new List<string>();
        for (int i = 10; i <= 255; i++)
        {
            usedIps.Add($"192.168.10.{i}");
        }

        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync((byte[]?)null);

        // Act
        var result = await _service.GetUnusedNetworkIpAddress(network, usedIps);

        // Assert - Should roll over to 192.168.11.0
        Assert.That(result, Is.EqualTo("192.168.11.0"));
    }

    [Test]
    public async Task GetUnusedNetworkIpAddress_WorksWith22Subnet()
    {
        // Arrange - /22 subnet covers 10.0.0.0 - 10.0.3.255
        var network = new UnifiNetwork
        {
            Name = "TestNetwork",
            IpSubnet = "10.0.0.0/22",
            DhcpStartAddress = "10.0.2.100"
        };
        var usedIps = new List<string>();
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync((byte[]?)null);

        // Act
        var result = await _service.GetUnusedNetworkIpAddress(network, usedIps);

        // Assert - Should start at network base + 10
        Assert.That(result, Is.EqualTo("10.0.0.10"));
    }

    [Test]
    public async Task GetUnusedNetworkIpAddress_LogsWarningWhenNoIpsFound()
    {
        // Arrange - all IPs exhausted
        var network = new UnifiNetwork
        {
            Name = "TestNetwork",
            IpSubnet = "192.168.1.0/24",
            DhcpStartAddress = "192.168.1.13"
        };
        var usedIps = new List<string> { "192.168.1.10", "192.168.1.11", "192.168.1.12" };
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync((byte[]?)null);

        // Act
        var result = await _service.GetUnusedNetworkIpAddress(network, usedIps);

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));

        // Verify warning was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No open IPs found for network TestNetwork")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task ReturnIpAddress_AddsIpToCooldown()
    {
        // Arrange
        var ipAddress = "192.168.1.100";
        _cacheMock.Setup(c => c.GetAsync($"Unifi.IpManager.IpCooldown.{ipAddress}", default))
            .ReturnsAsync((byte[]?)null);

        // Act
        await _service.ReturnIpAddress(ipAddress);

        // Assert
        _cacheMock.Verify(c => c.SetAsync(
            $"Unifi.IpManager.IpCooldown.{ipAddress}",
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            default), Times.Once);
    }

    [Test]
    public async Task ReturnIpAddress_HandlesNullIpAddress()
    {
        // Act
        await _service.ReturnIpAddress(null);

        // Assert - should not throw, should not cache
        _cacheMock.Verify(c => c.SetAsync(
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            default), Times.Never);
    }

    [Test]
    public async Task ReturnIpAddress_HandlesEmptyIpAddress()
    {
        // Act
        await _service.ReturnIpAddress("");

        // Assert - should not throw, should not cache
        _cacheMock.Verify(c => c.SetAsync(
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            default), Times.Never);
    }

    [Test]
    public async Task ReturnIpAddress_DoesNotCacheIfAlreadyInCooldown()
    {
        // Arrange
        var ipAddress = "192.168.1.100";
        _cacheMock.Setup(c => c.GetAsync($"Unifi.IpManager.IpCooldown.{ipAddress}", default))
            .ReturnsAsync(Encoding.UTF8.GetBytes(ipAddress));

        // Act
        await _service.ReturnIpAddress(ipAddress);

        // Assert - should not add again if already in cache
        _cacheMock.Verify(c => c.SetAsync(
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            default), Times.Never);
    }

    [Test]
    public async Task IntegrationTest_IpLifecycle()
    {
        // This test simulates a complete IP lifecycle: get unused -> return -> cooldown -> available again
        var network = new UnifiNetwork
        {
            Name = "TestNetwork",
            IpSubnet = "192.168.1.0/24",
            DhcpStartAddress = "192.168.1.100"
        };
        var usedIps = new List<string>();
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync((byte[]?)null);

        // Get an unused IP
        var unusedIp = await _service.GetUnusedNetworkIpAddress(network, usedIps);
        Assert.That(unusedIp, Is.EqualTo("192.168.1.10"));

        // Return the IP (should go into cooldown)
        await _service.ReturnIpAddress(unusedIp);

        // Verify SetAsync was called to put IP in cooldown
        _cacheMock.Verify(c => c.SetAsync(
            $"Unifi.IpManager.IpCooldown.{unusedIp}",
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            default), Times.Once);

        // Now simulate IP is in cooldown - should skip it
        _cacheMock.Setup(c => c.GetAsync($"Unifi.IpManager.IpCooldown.{unusedIp}", default))
            .ReturnsAsync(Encoding.UTF8.GetBytes(unusedIp));
        _cacheMock.Setup(c => c.GetAsync("Unifi.IpManager.IpCooldown.192.168.1.11", default))
            .ReturnsAsync((byte[]?)null);

        var nextIp = await _service.GetUnusedNetworkIpAddress(network, usedIps);
        Assert.That(nextIp, Is.EqualTo("192.168.1.11")); // Should get next available IP
    }
}
