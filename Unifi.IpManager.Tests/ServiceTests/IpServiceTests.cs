using System;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
// using Microsoft.Extensions.Options; // removed duplicate
using Moq;
using NUnit.Framework;
using Unifi.IpManager.Options;
using Microsoft.Extensions.Options;
using Unifi.IpManager.Services;

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
			IpCooldownMinutes = 5,
			IpGroups = new List<IpGroup>
			{
				new IpGroup
				{
					Name = "TestGroup",
					Blocks = new List<IpBlock>
					{
						new IpBlock { Min = 100, Max = 102 }
					}
				}
			}
		};
	_options = Microsoft.Extensions.Options.Options.Create(_ipOptions);
		_cacheMock = new Mock<IDistributedCache>();
		_loggerMock = new Mock<ILogger<IpService>>();
		_service = new IpService(_options, _loggerMock.Object, _cacheMock.Object);
	}

	[Test]
	public async Task GetUnusedGroupIpAddress_ReturnsUnusedIp()
	{
		// Arrange
		var usedIps = new List<string> { "192.168.1.100" };
		_cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync((byte[]?)null);

		// Act
		var result = await _service.GetUnusedGroupIpAddress("TestGroup", usedIps);

		// Assert
		Assert.That(result, Is.EqualTo("192.168.1.101"));
	}

	[Test]
	public async Task GetUnusedGroupIpAddress_ReturnsEmptyIfNoGroup()
	{
		var usedIps = new List<string>();
		var result = await _service.GetUnusedGroupIpAddress("Nonexistent", usedIps);
		Assert.That(result, Is.EqualTo(string.Empty));
	}

	[Test]
	public async Task GetUnusedGroupIpAddress_SkipsIpInCooldown()
	{
		var usedIps = new List<string>();
		// Simulate 192.168.1.100 is in cooldown
		_cacheMock.Setup(c => c.GetAsync("Unifi.IpManager.IpCooldown.192.168.1.100", default)).ReturnsAsync(Encoding.UTF8.GetBytes("192.168.1.100"));
		_cacheMock.Setup(c => c.GetAsync("Unifi.IpManager.IpCooldown.192.168.1.101", default)).ReturnsAsync((byte[]?)null);
		var result = await _service.GetUnusedGroupIpAddress("TestGroup", usedIps);
		Assert.That(result, Is.EqualTo("192.168.1.101"));
	}

	[Test]
	public async Task GetUnusedGroupIpAddress_ReturnsEmptyWhenAllIpsExhausted()
	{
		// Arrange - all IPs in range are used
		var usedIps = new List<string> { "192.168.1.100", "192.168.1.101" };
		_cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync((byte[]?)null);

		// Act
		var result = await _service.GetUnusedGroupIpAddress("TestGroup", usedIps);

		// Assert
		Assert.That(result, Is.EqualTo(string.Empty));
	}

	[Test]
	public async Task GetUnusedGroupIpAddress_ReturnsEmptyWhenAllIpsInCooldown()
	{
		// Arrange - all IPs are in cooldown
		var usedIps = new List<string>();
		_cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync(Encoding.UTF8.GetBytes("test"));

		// Act
		var result = await _service.GetUnusedGroupIpAddress("TestGroup", usedIps);

		// Assert
		Assert.That(result, Is.EqualTo(string.Empty));
	}

	[Test]
	public async Task GetUnusedGroupIpAddress_HandlesMultipleBlocks()
	{
		// Arrange - setup service with multiple IP blocks
		_ipOptions.IpGroups = new List<IpGroup>
		{
			new IpGroup
			{
				Name = "MultiBlock",
				Blocks = new List<IpBlock>
				{
					new IpBlock { Min = 10, Max = 12 },
					new IpBlock { Min = 20, Max = 22 }
				}
			}
		};
		_options = Microsoft.Extensions.Options.Options.Create(_ipOptions);
		_service = new IpService(_options, _loggerMock.Object, _cacheMock.Object);

		var usedIps = new List<string> { "192.168.1.10", "192.168.1.11" }; // Block first block
		_cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync((byte[]?)null);

		// Act
		var result = await _service.GetUnusedGroupIpAddress("MultiBlock", usedIps);

		// Assert
		Assert.That(result, Is.EqualTo("192.168.1.12"));
	}

	[Test]
	public async Task GetUnusedGroupIpAddress_ReturnsFirstAvailableFromSecondBlock()
	{
		// Arrange - setup service with multiple IP blocks
		_ipOptions.IpGroups = new List<IpGroup>
		{
			new IpGroup
			{
				Name = "MultiBlock",
				Blocks = new List<IpBlock>
				{
					new IpBlock { Min = 10, Max = 11 }, // Only 2 IPs
					new IpBlock { Min = 20, Max = 22 }
				}
			}
		};
		_options = Microsoft.Extensions.Options.Options.Create(_ipOptions);
		_service = new IpService(_options, _loggerMock.Object, _cacheMock.Object);

		var usedIps = new List<string> { "192.168.1.10", "192.168.1.11" }; // Block first block completely
		_cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync((byte[]?)null);

		// Act
		var result = await _service.GetUnusedGroupIpAddress("MultiBlock", usedIps);

		// Assert
		Assert.That(result, Is.EqualTo("192.168.1.20"));
	}

	[Test]
	public void GetUnusedGroupIpAddress_HandlesNullUsedIpsList()
	{
		// Arrange
		_cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync((byte[]?)null);

		// Act & Assert
		Assert.ThrowsAsync<ArgumentNullException>(async () => 
			await _service.GetUnusedGroupIpAddress("TestGroup", null));
	}

	[Test]
	public async Task GetUnusedGroupIpAddress_HandlesEmptyGroupName()
	{
		// Arrange
		var usedIps = new List<string>();

		// Act
		var result = await _service.GetUnusedGroupIpAddress("", usedIps);

		// Assert
		Assert.That(result, Is.EqualTo(string.Empty));
	}

	[Test]
	public async Task GetUnusedGroupIpAddress_HandlesNullGroupName()
	{
		// Arrange
		var usedIps = new List<string>();

		// Act
		var result = await _service.GetUnusedGroupIpAddress(null, usedIps);

		// Assert
		Assert.That(result, Is.EqualTo(string.Empty));
	}

	[Test]
	public void GetIpGroupForAddress_ReturnsGroupName()
	{
		var result = _service.GetIpGroupForAddress("192.168.1.100");
		Assert.That(result, Is.EqualTo("TestGroup"));
	}

	[Test]
	public void GetIpGroupForAddress_ReturnsEmptyForInvalidIp()
	{
		var result = _service.GetIpGroupForAddress("notanip");
		Assert.That(result, Is.EqualTo(string.Empty));
	}

	[Test]
	public void GetIpGroupForAddress_ReturnsEmptyForNullIp()
	{
		var result = _service.GetIpGroupForAddress(null);
		Assert.That(result, Is.EqualTo(string.Empty));
	}

	[Test]
	public void GetIpGroupForAddress_ReturnsEmptyForEmptyIp()
	{
		var result = _service.GetIpGroupForAddress("");
		Assert.That(result, Is.EqualTo(string.Empty));
	}

	[Test]
	public void GetIpGroupForAddress_ReturnsEmptyForWhitespaceIp()
	{
		var result = _service.GetIpGroupForAddress("   ");
		Assert.That(result, Is.EqualTo(string.Empty));
	}

	[Test]
	public void GetIpGroupForAddress_HandlesMinBoundary()
	{
		var result = _service.GetIpGroupForAddress("192.168.1.100"); // Min boundary
		Assert.That(result, Is.EqualTo("TestGroup"));
	}

	[Test]
	public void GetIpGroupForAddress_HandlesMaxBoundary()
	{
		var result = _service.GetIpGroupForAddress("192.168.1.102"); // Max boundary
		Assert.That(result, Is.EqualTo("TestGroup"));
	}

	[Test]
	public void GetIpGroupForAddress_ReturnsEmptyForIpOutsideRange()
	{
		var result = _service.GetIpGroupForAddress("192.168.1.99"); // Below min
		Assert.That(result, Is.EqualTo(string.Empty));
		
		result = _service.GetIpGroupForAddress("192.168.1.103"); // Above max
		Assert.That(result, Is.EqualTo(string.Empty));
	}

	[Test]
	public void GetIpGroupForAddress_HandlesMultipleGroupsReturnsFirst()
	{
		// Arrange - setup multiple groups with overlapping ranges
		_ipOptions.IpGroups = new List<IpGroup>
		{
			new IpGroup
			{
				Name = "Group1",
				Blocks = new List<IpBlock> { new IpBlock { Min = 100, Max = 150 } }
			},
			new IpGroup
			{
				Name = "Group2",
				Blocks = new List<IpBlock> { new IpBlock { Min = 120, Max = 180 } }
			}
		};
		_options = Microsoft.Extensions.Options.Options.Create(_ipOptions);
		_service = new IpService(_options, _loggerMock.Object, _cacheMock.Object);

		// Act
		var result = _service.GetIpGroupForAddress("192.168.1.130"); // In both ranges

		// Assert
		Assert.That(result, Is.EqualTo("Group1")); // Should return first match
	}

	[TestCase("192.168.1", ExpectedResult = "")]
	[TestCase("192.168.1.256", ExpectedResult = "")]
	[TestCase("192.168.1.100.1", ExpectedResult = "")]
	[TestCase("192.168.a.100", ExpectedResult = "")]
	[TestCase("192..1.100", ExpectedResult = "")]
	[TestCase("999.999.999.999", ExpectedResult = "")]
	public string GetIpGroupForAddress_HandlesInvalidIpFormats(string ipAddress)
	{
		return _service.GetIpGroupForAddress(ipAddress);
	}

	[Test]
	public async Task ReturnIpAddress_SetsCacheIfNotExists()
	{
		var ip = "192.168.1.100";
		_cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync((byte[]?)null);
		_cacheMock.Setup(c => c.SetAsync(
			It.IsAny<string>(),
			It.IsAny<byte[]>(),
			It.IsAny<DistributedCacheEntryOptions>(),
			default)).Returns(Task.CompletedTask).Verifiable();

		await _service.ReturnIpAddress(ip);
		_cacheMock.Verify(c => c.SetAsync(
			It.Is<string>(k => k.Contains(ip)),
			It.Is<byte[]>(b => Encoding.UTF8.GetString(b) == ip),
			It.IsAny<DistributedCacheEntryOptions>(),
			default), Times.Once);
	}

	[Test]
	public async Task ReturnIpAddress_DoesNotSetCacheIfExists()
	{
		var ip = "192.168.1.100";
		_cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync(Encoding.UTF8.GetBytes(ip));
		await _service.ReturnIpAddress(ip);
		_cacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), default), Times.Never);
	}

	[Test]
	public async Task ReturnIpAddress_SetsCacheWithCorrectExpiration()
	{
		var ip = "192.168.1.100";
		_cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync((byte[]?)null);

		DistributedCacheEntryOptions? capturedOptions = null;
		_cacheMock.Setup(c => c.SetAsync(
			It.IsAny<string>(),
			It.IsAny<byte[]>(),
			It.IsAny<DistributedCacheEntryOptions>(),
			default))
			.Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>((key, value, options, token) =>
			{
				capturedOptions = options;
			})
			.Returns(Task.CompletedTask);

		await _service.ReturnIpAddress(ip);

		Assert.That(capturedOptions, Is.Not.Null);
		Assert.That(capturedOptions!.AbsoluteExpiration, Is.Not.Null);
		// Verify expiration is approximately correct (within 1 minute tolerance)
		var expectedExpiration = DateTime.Now.AddMinutes(_ipOptions.IpCooldownMinutes);
		var actualExpiration = capturedOptions.AbsoluteExpiration!.Value.DateTime;
		Assert.That(Math.Abs((expectedExpiration - actualExpiration).TotalMinutes), Is.LessThan(1));
	}

	[Test]
	public async Task ReturnIpAddress_UsesCacheKeyTemplate()
	{
		var ip = "192.168.1.100";
		var expectedKey = $"Unifi.IpManager.IpCooldown.{ip}";
		_cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync((byte[]?)null);

		await _service.ReturnIpAddress(ip);

		_cacheMock.Verify(c => c.GetAsync(expectedKey, default), Times.Once);
	}

	[Test]
	public async Task ReturnIpAddress_HandlesNullIpAddress()
	{
		await _service.ReturnIpAddress(null);
		// Should not throw exception, verify no cache operations
		_cacheMock.Verify(c => c.GetAsync(It.IsAny<string>(), default), Times.Never);
		_cacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), default), Times.Never);
	}

	[Test]
	public async Task ReturnIpAddress_HandlesEmptyIpAddress()
	{
		await _service.ReturnIpAddress("");
		// Should not throw exception, verify cache operations still happen with empty string
		var expectedKey = "Unifi.IpManager.IpCooldown.";
		_cacheMock.Verify(c => c.GetAsync(expectedKey, default), Times.Once);
	}

	[Test]
	public void ReturnIpAddress_HandlesCacheException()
	{
		var ip = "192.168.1.100";
		_cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default))
			.ThrowsAsync(new Exception("Cache error"));

		// Should not throw exception
		Assert.DoesNotThrowAsync(async () => await _service.ReturnIpAddress(ip));
	}

	[Test]
	public async Task GetUnusedGroupIpAddress_WithEmptyBlocksList()
	{
		// Arrange - group with no blocks
		_ipOptions.IpGroups = new List<IpGroup>
		{
			new IpGroup
			{
				Name = "EmptyGroup",
				Blocks = new List<IpBlock>()
			}
		};
		_options = Microsoft.Extensions.Options.Options.Create(_ipOptions);
		_service = new IpService(_options, _loggerMock.Object, _cacheMock.Object);

		var usedIps = new List<string>();

		// Act
		var result = await _service.GetUnusedGroupIpAddress("EmptyGroup", usedIps);

		// Assert
		Assert.That(result, Is.EqualTo(string.Empty));
	}

	[Test]
	public void GetUnusedGroupIpAddress_WithNullBlocksList()
	{
		// Arrange - group with null blocks
		_ipOptions.IpGroups = new List<IpGroup>
		{
			new IpGroup
			{
				Name = "NullBlocksGroup",
				Blocks = null
			}
		};
		_options = Microsoft.Extensions.Options.Options.Create(_ipOptions);
		_service = new IpService(_options, _loggerMock.Object, _cacheMock.Object);

		var usedIps = new List<string>();

		// Act & Assert
		Assert.ThrowsAsync<NullReferenceException>(async () =>
			await _service.GetUnusedGroupIpAddress("NullBlocksGroup", usedIps));
	}

	[Test]
	public void GetIpGroupForAddress_WithEmptyGroupsList()
	{
		// Arrange - no groups configured
		_ipOptions.IpGroups = new List<IpGroup>();
		_options = Microsoft.Extensions.Options.Options.Create(_ipOptions);
		_service = new IpService(_options, _loggerMock.Object, _cacheMock.Object);

		// Act
		var result = _service.GetIpGroupForAddress("192.168.1.100");

		// Assert
		Assert.That(result, Is.EqualTo(string.Empty));
	}

	[Test]
	public void GetIpGroupForAddress_WithNullGroupsList()
	{
		// Arrange - null groups list
		_ipOptions.IpGroups = null;
		_options = Microsoft.Extensions.Options.Options.Create(_ipOptions);
		_service = new IpService(_options, _loggerMock.Object, _cacheMock.Object);

		// Act & Assert
		Assert.Throws<NullReferenceException>(() =>
			_service.GetIpGroupForAddress("192.168.1.100"));
	}

	[Test]
	public async Task GetUnusedGroupIpAddress_LogsWarningWhenNoIpsFound()
	{
		// Arrange - all IPs exhausted
		var usedIps = new List<string> { "192.168.1.100", "192.168.1.101" };
		_cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync((byte[]?)null);

		// Act
		var result = await _service.GetUnusedGroupIpAddress("TestGroup", usedIps);

		// Assert
		Assert.That(result, Is.EqualTo(string.Empty));
		
		// Verify warning was logged
		_loggerMock.Verify(
			x => x.Log(
				LogLevel.Warning,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No open IPs found for TestGroup")),
				It.IsAny<Exception?>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
	}

	[Test]
	public async Task IntegrationTest_IpLifecycle()
	{
		// This test simulates a complete IP lifecycle: get unused -> return -> cooldown -> available again
		var usedIps = new List<string>();
		_cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync((byte[]?)null);

		// Get an unused IP
		var unusedIp = await _service.GetUnusedGroupIpAddress("TestGroup", usedIps);
		Assert.That(unusedIp, Is.EqualTo("192.168.1.100"));

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
		_cacheMock.Setup(c => c.GetAsync("Unifi.IpManager.IpCooldown.192.168.1.101", default))
			.ReturnsAsync((byte[]?)null);

		var nextIp = await _service.GetUnusedGroupIpAddress("TestGroup", usedIps);
		Assert.That(nextIp, Is.EqualTo("192.168.1.101")); // Should get next available IP
	}
}
