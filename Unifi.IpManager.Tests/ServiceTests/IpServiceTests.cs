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
}
