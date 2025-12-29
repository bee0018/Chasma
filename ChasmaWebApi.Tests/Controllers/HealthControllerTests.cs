using ChasmaWebApi.Controllers;
using ChasmaWebApi.Data.Messages;
using ChasmaWebApi.Data.Objects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace ChasmaWebApi.Tests.Controllers
{
    /// <summary>
    /// Class testing the functionality of the <see cref="HealthController"/> class.
    /// </summary>
    [TestClass]
    public class HealthControllerTests : ControllerTestBase<HealthController>
    {

        /// <summary>
        /// The mocked internal logger for API testing.
        /// </summary>
        private readonly Mock<ILogger<HealthController>> loggerMock;

        #region Constructor

        /// <summary>
        /// Instantiates a new <see cref="HealthControllerTests"/> class.
        /// </summary>
        public HealthControllerTests()
        {
            loggerMock = new Mock<ILogger<HealthController>>();
        }

        #endregion

        /// <summary>
        /// Sets up resources before each test.
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            Controller = new HealthController(loggerMock.Object);
        }

        /// <summary>
        /// Disposes of test resources after each test.
        /// </summary>
        [TestCleanup]
        public void CleanupTests()
        {
            loggerMock.Reset();
        }

        /// <summary>
        /// Tests that that <see cref="HealthController.GetHeartbeat"/> sends heartbeat messages.
        /// </summary>
        [TestMethod]
        public void TestGetHearbeat()
        {
            ActionResult<HeartbeatMessage> heartbeatMessageActionResult = Controller.GetHeartbeat();
            HeartbeatMessage heartbeatMessage = ExtractActionResultInnerResponseFromActionResult(heartbeatMessageActionResult, typeof(OkObjectResult));
            Assert.IsNotNull(heartbeatMessage);
            Assert.IsNotNull(heartbeatMessage.Message);
            Assert.AreEqual(HeartbeatStatus.Ok, heartbeatMessage.Status);
        }
    }
}
