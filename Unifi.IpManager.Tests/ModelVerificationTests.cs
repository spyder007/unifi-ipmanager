using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unifi.IpManager.Models.DTO;
using Unifi.IpManager.Models.Unifi;
using Unifi.IpManager.Models.Unifi.Requests;
using Unifi.IpManager.Options;

namespace Unifi.IpManager.Tests
{
    public class ModelVerificationTests
    {
        [SetUp]
        public void Setup()
        {
        }

        #region Property Tests
        [Test]
        public void EditClientRequestPropertyTest()
        {
            var editClientRequest = new EditClientRequest
            {
                Name = "test",
                Hostname = "test",
                Notes = new UniNote
                {
                    DnsHostname = "test",
                    SetOnDevice = true,
                    SyncDnsHostName = false
                }
            };

            Assert.Multiple(() =>
            {
                Assert.That(editClientRequest, Is.Not.Null);
                Assert.That(editClientRequest, Has.Property("Name").TypeOf<string>());
                Assert.That(editClientRequest, Has.Property("Hostname").TypeOf<string>());
                Assert.That(editClientRequest, Has.Property("Notes").TypeOf<UniNote>());
            });
        }

        [Test]
        public void NewClientRequestPropertyTest()
        {
            var newClientRequest = new NewClientRequest
            {
                MacAddress = "test",
                IpAddress = "test",
                SyncDns = true,
                StaticIp = true
            };

            Assert.Multiple(() =>
            {
                Assert.That(newClientRequest, Is.Not.Null);
                Assert.That(newClientRequest, Has.Property("MacAddress").TypeOf<string>());
                Assert.That(newClientRequest, Has.Property("IpAddress").TypeOf<string>());
                Assert.That(newClientRequest, Has.Property("SyncDns").TypeOf<bool>());
                Assert.That(newClientRequest, Has.Property("StaticIp").TypeOf<bool>());
            });
        }

        [Test]
        public void ProvisionRequestPropertyTest()
        {
            var provisionRequest = new ProvisionRequest
            {
                Group = "test",
                Name = "test",
                HostName = "test",
                SyncDns = true,
                StaticIp = true
            };

            Assert.Multiple(() =>
            {
                Assert.That(provisionRequest, Is.Not.Null);
                Assert.That(provisionRequest, Has.Property("Group").TypeOf<string>());
                Assert.That(provisionRequest, Has.Property("Name").TypeOf<string>());
                Assert.That(provisionRequest, Has.Property("HostName").TypeOf<string>());
                Assert.That(provisionRequest, Has.Property("SyncDns").TypeOf<bool>());
                Assert.That(provisionRequest, Has.Property("StaticIp").TypeOf<bool>());
            });
        }

        [Test]
        public void AddUniClientRequestPropertyTest()
        {
            var addUniClientRequest = new AddUniClientRequest
            {
                Mac = "test",
                Name = "test",
                HostName = "test",
                UseFixedIp = true,
                NetworkId = "test",
                FixedIp = "test",
                Note = "test"
            };

            Assert.Multiple(() =>
            {
                Assert.That(addUniClientRequest, Is.Not.Null);
                Assert.That(addUniClientRequest, Has.Property("Mac").TypeOf<string>());
                Assert.That(addUniClientRequest, Has.Property("Name").TypeOf<string>());
                Assert.That(addUniClientRequest, Has.Property("HostName").TypeOf<string>());
                Assert.That(addUniClientRequest, Has.Property("UseFixedIp").TypeOf<bool>());
                Assert.That(addUniClientRequest, Has.Property("NetworkId").TypeOf<string>());
                Assert.That(addUniClientRequest, Has.Property("FixedIp").TypeOf<string>());
                Assert.That(addUniClientRequest, Has.Property("Note").TypeOf<string>());
            });
        }

        [Test]
        public void EditUniClientRequestPropertyTest()
        {
            var editUniClientRequest = new EditUniClientRequest
            {
                Name = "test",
                HostName = "test",
                Note = "test",
                UserGroupId = "test"
            };

            Assert.Multiple(() =>
            {
                Assert.That(editUniClientRequest, Is.Not.Null);
                Assert.That(editUniClientRequest, Has.Property("UserGroupId").TypeOf<string>());
                Assert.That(editUniClientRequest, Has.Property("Name").TypeOf<string>());
                Assert.That(editUniClientRequest, Has.Property("HostName").TypeOf<string>());
                Assert.That(editUniClientRequest, Has.Property("Note").TypeOf<string>());
            });
        }

        [Test]
        public void IpBlockPropertyTest()
        {
            var ipBlock = new IpBlock
            {
                Min = 0,
                Max = 100
            };

            Assert.Multiple(() =>
            {
                Assert.That(ipBlock, Is.Not.Null);
                Assert.That(ipBlock, Has.Property("Min").TypeOf<int>());
                Assert.That(ipBlock, Has.Property("Max").TypeOf<int>());
            });
        }

        [Test]
        public void IpGroupPropertyTest()
        {
            var ipGroup = new IpGroup
            {
                Name = "test",
                Blocks = new List<IpBlock>
                {
                    new()
                    {
                        Min = 0,
                        Max = 100
                    }
                }

            };

            Assert.Multiple(() =>
            {
                Assert.That(ipGroup, Is.Not.Null);
                Assert.That(ipGroup, Has.Property("Name").TypeOf<string>());
                Assert.That(ipGroup, Has.Property("Blocks").TypeOf<List<IpBlock>>());
                Assert.That(ipGroup.Blocks, Has.Exactly(1).Items);
            });
        }

        #endregion
    }
}
