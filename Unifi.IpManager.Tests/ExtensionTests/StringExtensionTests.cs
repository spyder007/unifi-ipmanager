using System;
using NUnit.Framework;
using Unifi.IpManager.Extensions;

namespace Unifi.IpManager.Tests.ExtensionTests;

public class StringExtensionTests
{
    [Test]
    public void GetDomainFromHostname_WithFullyQualifiedDomainName_ReturnsDomain()
    {
        // Arrange
        var hostname = "server.example.com";

        // Act
        var result = hostname.GetDomainFromHostname();

        // Assert
        Assert.That(result, Is.EqualTo("example.com"));
    }

    [Test]
    public void GetDomainFromHostname_WithSubdomain_ReturnsDomainWithSubdomain()
    {
        // Arrange
        var hostname = "api.service.example.com";

        // Act
        var result = hostname.GetDomainFromHostname();

        // Assert
        Assert.That(result, Is.EqualTo("service.example.com"));
    }

    [Test]
    public void GetDomainFromHostname_WithMultipleSubdomains_ReturnsAllButFirst()
    {
        // Arrange
        var hostname = "www.api.service.example.com";

        // Act
        var result = hostname.GetDomainFromHostname();

        // Assert
        Assert.That(result, Is.EqualTo("api.service.example.com"));
    }

    [Test]
    public void GetDomainFromHostname_WithTwoPartHostname_ReturnsSameHostname()
    {
        // Arrange - Only two parts, so return the whole hostname
        var hostname = "example.com";

        // Act
        var result = hostname.GetDomainFromHostname();

        // Assert
        Assert.That(result, Is.EqualTo("example.com"));
    }

    [Test]
    public void GetDomainFromHostname_WithSinglePartHostname_ReturnsSameHostname()
    {
        // Arrange - Single part hostname (no dots)
        var hostname = "localhost";

        // Act
        var result = hostname.GetDomainFromHostname();

        // Assert
        Assert.That(result, Is.EqualTo("localhost"));
    }

    [Test]
    public void GetDomainFromHostname_WithEmptyString_ThrowsArgumentException()
    {
        // Arrange
        var hostname = "";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => hostname.GetDomainFromHostname());
        Assert.That(exception!.Message, Does.Contain("Invalid hostname"));
        Assert.That(exception.ParamName, Is.EqualTo("hostname"));
    }

    [Test]
    public void GetDomainFromHostname_WithNullString_ThrowsArgumentException()
    {
        // Arrange
        string? hostname = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => hostname!.GetDomainFromHostname());
        Assert.That(exception!.Message, Does.Contain("Invalid hostname"));
        Assert.That(exception.ParamName, Is.EqualTo("hostname"));
    }

    [Test]
    public void GetDomainFromHostname_WithWhitespaceString_ThrowsArgumentException()
    {
        // Arrange
        var hostname = "   ";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => hostname.GetDomainFromHostname());
        Assert.That(exception!.Message, Does.Contain("Invalid hostname"));
        Assert.That(exception.ParamName, Is.EqualTo("hostname"));
    }

    [Test]
    public void GetDomainFromHostname_WithHostnameStartingWithDot_ReturnsExpectedResult()
    {
        // Arrange
        var hostname = ".example.com";

        // Act
        var result = hostname.GetDomainFromHostname();

        // Assert
        Assert.That(result, Is.EqualTo("example.com"));
    }

    [Test]
    public void GetDomainFromHostname_WithHostnameEndingWithDot_ReturnsExpectedResult()
    {
        // Arrange
        var hostname = "server.example.com.";

        // Act
        var result = hostname.GetDomainFromHostname();

        // Assert
        Assert.That(result, Is.EqualTo("example.com."));
    }

    [Test]
    public void GetDomainFromHostname_WithConsecutiveDots_ReturnsExpectedResult()
    {
        // Arrange
        var hostname = "server..example.com";

        // Act
        var result = hostname.GetDomainFromHostname();

        // Assert
        Assert.That(result, Is.EqualTo(".example.com"));
    }

    [Test]
    public void GetDomainFromHostname_WithIPAddress_ReturnsSameValue()
    {
        // Arrange - IP addresses don't have traditional domain structure
        var hostname = "192.168.1.100";

        // Act
        var result = hostname.GetDomainFromHostname();

        // Assert
        Assert.That(result, Is.EqualTo("168.1.100"));
    }

    [Test]
    public void GetDomainFromHostname_WithLongSubdomainChain_ReturnsAllButFirst()
    {
        // Arrange
        var hostname = "a.b.c.d.e.f.example.com";

        // Act
        var result = hostname.GetDomainFromHostname();

        // Assert
        Assert.That(result, Is.EqualTo("b.c.d.e.f.example.com"));
    }

    [Test]
    public void GetDomainFromHostname_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var hostname = "test-server.sub-domain.example-site.com";

        // Act
        var result = hostname.GetDomainFromHostname();

        // Assert
        Assert.That(result, Is.EqualTo("sub-domain.example-site.com"));
    }

    [Test]
    public void GetDomainFromHostname_WithNumbers_HandlesCorrectly()
    {
        // Arrange
        var hostname = "server1.zone2.example3.com";

        // Act
        var result = hostname.GetDomainFromHostname();

        // Assert
        Assert.That(result, Is.EqualTo("zone2.example3.com"));
    }

    [Test]
    public void GetDomainFromHostname_WithInternationalDomain_HandlesCorrectly()
    {
        // Arrange
        var hostname = "server.测试.example.com";

        // Act
        var result = hostname.GetDomainFromHostname();

        // Assert
        Assert.That(result, Is.EqualTo("测试.example.com"));
    }

    [Test]
    public void GetDomainFromHostname_WithUppercaseHostname_PreservesCase()
    {
        // Arrange
        var hostname = "SERVER.EXAMPLE.COM";

        // Act
        var result = hostname.GetDomainFromHostname();

        // Assert
        Assert.That(result, Is.EqualTo("EXAMPLE.COM"));
    }

    [Test]
    public void GetDomainFromHostname_WithMixedCaseHostname_PreservesCase()
    {
        // Arrange
        var hostname = "SeRvEr.ExAmPlE.CoM";

        // Act
        var result = hostname.GetDomainFromHostname();

        // Assert
        Assert.That(result, Is.EqualTo("ExAmPlE.CoM"));
    }

    [Test]
    public void GetDomainFromHostname_WithOnlyDots_ReturnsExpectedResult()
    {
        // Arrange
        var hostname = "...";

        // Act
        var result = hostname.GetDomainFromHostname();

        // Assert
        Assert.That(result, Is.EqualTo(".."));
    }

    [Test]
    public void GetDomainFromHostname_WithSingleDot_ReturnsSameValue()
    {
        // Arrange
        var hostname = ".";

        // Act
        var result = hostname.GetDomainFromHostname();

        // Assert
        Assert.That(result, Is.EqualTo("."));
    }
}
