using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Unifi.IpManager.Controllers;
using Unifi.IpManager.Options;

namespace Unifi.IpManager.Tests
{
    public class InfoControllerTests
    {
        private UnifiControllerOptions? _testOptions = new();

        [SetUp]
        public void Setup()
        {
            _testOptions = new UnifiControllerOptions
            {
                Url = "https://localhost/",
                Username = "test",
                Password = "1234"
            };
        }

        [Test]
        public void InfoGetTest()
        {
            var optionsMock = new Mock<IOptions<UnifiControllerOptions>>();
            optionsMock.Setup(options => options.Value).Returns(_testOptions ?? new UnifiControllerOptions());

            var logMock = new Mock<ILogger<InfoController>>();

            var controller = new InfoController(logMock.Object, optionsMock.Object);

            var result = controller.Get();

            Assert.Multiple(() =>
            {
                Assert.That(result.Value, Is.Not.Null);
                Assert.That(result.Value?.UnifiControllerOptions.Url, Is.EqualTo(_testOptions?.Url));
                Assert.That(result.Value?.UnifiControllerOptions.Username, Is.EqualTo(_testOptions?.Username));
                Assert.That(result.Value?.UnifiControllerOptions.Password, Is.EqualTo(_testOptions?.Password));
            });
        }
    }
}
