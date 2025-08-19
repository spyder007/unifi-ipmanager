using System;
using NUnit.Framework;
using Unifi.IpManager.Extensions;
using Unifi.IpManager.Models.DTO;
using Unifi.IpManager.Models.Unifi;

namespace Unifi.IpManager.Tests.ExtensionTests;

public class HostRecordExtensionsTests
{
    [Test]
    public void ToUniHostRecord_ConvertsHostDnsRecordCorrectly()
    {
        // Arrange
        var hostDnsRecord = new HostDnsRecord
        {
            Id = "test123",
            Hostname = "test.example.com",
            IpAddress = "192.168.1.100",
            RecordType = "A",
            MacAddress = "00:11:22:33:44:55",
            DeviceLock = true
        };

        // Act
        var result = hostDnsRecord.ToUniHostRecord();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo("test123"));
        Assert.That(result.Key, Is.EqualTo("test.example.com"));
        Assert.That(result.Value, Is.EqualTo("192.168.1.100"));
        Assert.That(result.RecordType, Is.EqualTo("A"));
        Assert.That(result.Enabled, Is.True); // Default value
        Assert.That(result.Port, Is.EqualTo(0)); // Default value
        Assert.That(result.Priority, Is.EqualTo(0)); // Default value
        Assert.That(result.Ttl, Is.EqualTo(0)); // Default value
        Assert.That(result.Weight, Is.EqualTo(0)); // Default value
    }

    [Test]
    public void ToUniHostRecord_HandlesNullValues()
    {
        // Arrange
        var hostDnsRecord = new HostDnsRecord
        {
            Id = null,
            Hostname = null,
            IpAddress = null,
            RecordType = null
        };

        // Act
        var result = hostDnsRecord.ToUniHostRecord();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.Null);
        Assert.That(result.Key, Is.Null);
        Assert.That(result.Value, Is.Null);
        Assert.That(result.RecordType, Is.Null);
        Assert.That(result.Enabled, Is.True); // Still defaults to true
        Assert.That(result.Port, Is.EqualTo(0));
        Assert.That(result.Priority, Is.EqualTo(0));
        Assert.That(result.Ttl, Is.EqualTo(0));
        Assert.That(result.Weight, Is.EqualTo(0));
    }

    [Test]
    public void ToUniHostRecord_HandlesEmptyValues()
    {
        // Arrange
        var hostDnsRecord = new HostDnsRecord
        {
            Id = "",
            Hostname = "",
            IpAddress = "",
            RecordType = ""
        };

        // Act
        var result = hostDnsRecord.ToUniHostRecord();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(""));
        Assert.That(result.Key, Is.EqualTo(""));
        Assert.That(result.Value, Is.EqualTo(""));
        Assert.That(result.RecordType, Is.EqualTo(""));
        Assert.That(result.Enabled, Is.True);
    }

    [Test]
    public void ToUniHostRecord_HandlesDifferentRecordTypes()
    {
        // Arrange
        var testCases = new[]
        {
            "A",
            "AAAA",
            "CNAME",
            "MX",
            "TXT",
            "SRV"
        };

        foreach (var recordType in testCases)
        {
            var hostDnsRecord = new HostDnsRecord
            {
                Id = "test",
                Hostname = "test.com",
                IpAddress = "1.1.1.1",
                RecordType = recordType
            };

            // Act
            var result = hostDnsRecord.ToUniHostRecord();

            // Assert
            Assert.That(result.RecordType, Is.EqualTo(recordType), $"Failed for record type: {recordType}");
        }
    }

    [Test]
    public void ToHostDnsRecord_FromUniHostRecord_ConvertsCorrectly()
    {
        // Arrange
        var uniHostRecord = new UniHostRecord
        {
            Id = "uni123",
            Key = "host.example.com",
            Value = "10.0.0.1",
            RecordType = "AAAA",
            Enabled = false,
            Port = 80,
            Priority = 10,
            Ttl = 300,
            Weight = 5
        };

        // Act
        var result = uniHostRecord.ToHostDnsRecord();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo("uni123"));
        Assert.That(result.Hostname, Is.EqualTo("host.example.com"));
        Assert.That(result.IpAddress, Is.EqualTo("10.0.0.1"));
        Assert.That(result.RecordType, Is.EqualTo("AAAA"));
        Assert.That(result.DeviceLock, Is.False); // Always false for host records
        Assert.That(result.MacAddress, Is.Null); // Not set from UniHostRecord
    }

    [Test]
    public void ToHostDnsRecord_FromUniHostRecord_HandlesNullValues()
    {
        // Arrange
        var uniHostRecord = new UniHostRecord
        {
            Id = null,
            Key = null,
            Value = null,
            RecordType = null
        };

        // Act
        var result = uniHostRecord.ToHostDnsRecord();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.Null);
        Assert.That(result.Hostname, Is.Null);
        Assert.That(result.IpAddress, Is.Null);
        Assert.That(result.RecordType, Is.Null);
        Assert.That(result.DeviceLock, Is.False);
    }

    [Test]
    public void ToHostDnsRecord_FromUniDeviceDnsRecord_ConvertsCorrectly()
    {
        // Arrange
        var uniDeviceRecord = new UniDeviceDnsRecord
        {
            Hostname = "device.local",
            IpAddress = "192.168.1.200",
            MacAddress = "AA:BB:CC:DD:EE:FF"
        };

        // Act
        var result = uniDeviceRecord.ToHostDnsRecord();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.Null); // Not set from device record
        Assert.That(result.Hostname, Is.EqualTo("device.local"));
        Assert.That(result.IpAddress, Is.EqualTo("192.168.1.200"));
        Assert.That(result.MacAddress, Is.EqualTo("AA:BB:CC:DD:EE:FF"));
        Assert.That(result.RecordType, Is.EqualTo("A")); // Default for device records
        Assert.That(result.DeviceLock, Is.True); // Always true for device records
    }

    [Test]
    public void ToHostDnsRecord_FromUniDeviceDnsRecord_HandlesNullValues()
    {
        // Arrange
        var uniDeviceRecord = new UniDeviceDnsRecord
        {
            Hostname = null,
            IpAddress = null,
            MacAddress = null
        };

        // Act
        var result = uniDeviceRecord.ToHostDnsRecord();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Hostname, Is.Null);
        Assert.That(result.IpAddress, Is.Null);
        Assert.That(result.MacAddress, Is.Null);
        Assert.That(result.RecordType, Is.EqualTo("A")); // Still defaults to A
        Assert.That(result.DeviceLock, Is.True); // Still true for device records
    }

    [Test]
    public void ToHostDnsRecord_FromUniDeviceDnsRecord_HandlesEmptyValues()
    {
        // Arrange
        var uniDeviceRecord = new UniDeviceDnsRecord
        {
            Hostname = "",
            IpAddress = "",
            MacAddress = ""
        };

        // Act
        var result = uniDeviceRecord.ToHostDnsRecord();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Hostname, Is.EqualTo(""));
        Assert.That(result.IpAddress, Is.EqualTo(""));
        Assert.That(result.MacAddress, Is.EqualTo(""));
        Assert.That(result.RecordType, Is.EqualTo("A"));
        Assert.That(result.DeviceLock, Is.True);
    }

    [Test]
    public void RoundTrip_HostDnsRecord_To_UniHostRecord_And_Back()
    {
        // Arrange
        var originalRecord = new HostDnsRecord
        {
            Id = "roundtrip123",
            Hostname = "roundtrip.test.com",
            IpAddress = "172.16.0.1",
            RecordType = "CNAME",
            MacAddress = "11:22:33:44:55:66", // This will be lost in round trip
            DeviceLock = true // This will become false after round trip
        };

        // Act
        var uniRecord = originalRecord.ToUniHostRecord();
        var backToHostRecord = uniRecord.ToHostDnsRecord();

        // Assert
        Assert.That(backToHostRecord.Id, Is.EqualTo(originalRecord.Id));
        Assert.That(backToHostRecord.Hostname, Is.EqualTo(originalRecord.Hostname));
        Assert.That(backToHostRecord.IpAddress, Is.EqualTo(originalRecord.IpAddress));
        Assert.That(backToHostRecord.RecordType, Is.EqualTo(originalRecord.RecordType));
        
        // These values change during conversion
        Assert.That(backToHostRecord.MacAddress, Is.Null); // Lost in conversion
        Assert.That(backToHostRecord.DeviceLock, Is.False); // Always false for UniHostRecord conversion
    }

    [Test]
    public void DeviceLock_Behavior_DiffersBetweenConversions()
    {
        // Arrange
        var hostRecord = new HostDnsRecord { DeviceLock = true };
        var deviceRecord = new UniDeviceDnsRecord();

        // Act
        var fromHostRecord = hostRecord.ToUniHostRecord().ToHostDnsRecord();
        var fromDeviceRecord = deviceRecord.ToHostDnsRecord();

        // Assert
        Assert.That(fromHostRecord.DeviceLock, Is.False, "Host records should never be device locked after conversion");
        Assert.That(fromDeviceRecord.DeviceLock, Is.True, "Device records should always be device locked");
    }

    [Test]
    public void ToUniHostRecord_SetsCorrectDefaults()
    {
        // Arrange
        var hostRecord = new HostDnsRecord();

        // Act
        var result = hostRecord.ToUniHostRecord();

        // Assert - Verify all default values are set correctly
        Assert.That(result.Enabled, Is.True, "Enabled should default to true");
        Assert.That(result.Port, Is.EqualTo(0), "Port should default to 0");
        Assert.That(result.Priority, Is.EqualTo(0), "Priority should default to 0");
        Assert.That(result.Ttl, Is.EqualTo(0), "TTL should default to 0");
        Assert.That(result.Weight, Is.EqualTo(0), "Weight should default to 0");
    }

    [Test]
    public void ToHostDnsRecord_FromUniDeviceDnsRecord_AlwaysSetsRecordTypeToA()
    {
        // Arrange - Even though UniDeviceDnsRecord doesn't have RecordType, the conversion should always set it to "A"
        var deviceRecord = new UniDeviceDnsRecord
        {
            Hostname = "device.test",
            IpAddress = "1.2.3.4",
            MacAddress = "00:00:00:00:00:00"
        };

        // Act
        var result = deviceRecord.ToHostDnsRecord();

        // Assert
        Assert.That(result.RecordType, Is.EqualTo("A"), "Device DNS records should always convert to A records");
    }

    [Test]
    public void ExtensionMethods_HandleExtremeLengthStrings()
    {
        // Arrange
        var longString = new string('x', 1000);
        var hostRecord = new HostDnsRecord
        {
            Id = longString,
            Hostname = longString,
            IpAddress = longString,
            RecordType = longString
        };

        // Act
        var uniRecord = hostRecord.ToUniHostRecord();
        var backToHost = uniRecord.ToHostDnsRecord();

        // Assert - Should handle long strings without truncation
        Assert.That(uniRecord.Id, Is.EqualTo(longString));
        Assert.That(uniRecord.Key, Is.EqualTo(longString));
        Assert.That(uniRecord.Value, Is.EqualTo(longString));
        Assert.That(uniRecord.RecordType, Is.EqualTo(longString));
        
        Assert.That(backToHost.Id, Is.EqualTo(longString));
        Assert.That(backToHost.Hostname, Is.EqualTo(longString));
        Assert.That(backToHost.IpAddress, Is.EqualTo(longString));
        Assert.That(backToHost.RecordType, Is.EqualTo(longString));
    }
}
