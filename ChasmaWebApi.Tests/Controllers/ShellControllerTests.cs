using System.Collections.Concurrent;
using ChasmaWebApi.Controllers;
using ChasmaWebApi.Data.Interfaces;
using ChasmaWebApi.Data.Requests.Shell;
using ChasmaWebApi.Data.Responses.Shell;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace ChasmaWebApi.Tests.Controllers;

/// <summary>
/// Class testing the functionality of the <see cref="ShellController"/>.
/// </summary>
[TestClass]
public class ShellControllerTests : ControllerTestBase<ShellController>
{
    /// <summary>
    /// The mocked implementation of the internal API logger.
    /// </summary>
    private readonly Mock<ILogger<ShellController>> loggerMock;
    
    /// <summary>
    /// The mocked implementation of the internal Shell Manager.
    /// </summary>
    private readonly Mock<IShellManager> shellManagerMock;
    
    /// <summary>
    /// The mocked implementation of the internal cache manager.
    /// </summary>
    private readonly Mock<ICacheManager> cacheManagerMock;

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ShellControllerTests"/> class.
    /// </summary>
    public ShellControllerTests()
    {
        loggerMock = new Mock<ILogger<ShellController>>();
        shellManagerMock = new Mock<IShellManager>();
        cacheManagerMock = new Mock<ICacheManager>();
    }

    #endregion
    
    /// <summary>
    /// Set up the tests before each case is run.
    /// </summary>
    [TestInitialize]
    public void Setup()
    {
        Controller = new ShellController(loggerMock.Object, shellManagerMock.Object, cacheManagerMock.Object);
    }
    
    /// <summary>
    /// Cleans up the resources after each test.
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        loggerMock.Reset();
        shellManagerMock.Reset();
        cacheManagerMock.Reset();
    }
    
    /// <summary>
    /// Tests that the <see cref="ShellController.ExecuteShellCommands(ExecuteShellCommandRequest)"/> sends an error
    /// response when the incoming request is null.
    /// </summary>
    [TestMethod]
    public void TestExecuteShellCommandsWithNullRequest()
    {
        ActionResult<ExecuteShellCommandResponse> actionResult = Controller.ExecuteShellCommands(null!);
        ExecuteShellCommandResponse response =  GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
        Assert.IsTrue(response.IsErrorResponse);
        Assert.AreEqual("Request is null. Cannot execute commands.", response.ErrorMessage);
    }
    
    /// <summary>
    /// Tests that the <see cref="ShellController.ExecuteShellCommands(ExecuteShellCommandRequest)"/> sends an error
    /// response when the incoming request has an empty repository identifier.
    /// </summary>
    [TestMethod]
    public void TestExecuteShellCommandsWithEmptyRepositoryId()
    {
        ExecuteShellCommandRequest request = new();
        ActionResult<ExecuteShellCommandResponse> actionResult = Controller.ExecuteShellCommands(request);
        ExecuteShellCommandResponse response =  GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
        Assert.IsTrue(response.IsErrorResponse);
        Assert.AreEqual("RepositoryId is required to execute shell commands.", response.ErrorMessage);
    }
    
    /// <summary>
    /// Tests that the <see cref="ShellController.ExecuteShellCommands(ExecuteShellCommandRequest)"/> sends an error
    /// response when the incoming request has a repository identifier that does not have a working directory.
    /// </summary>
    [TestMethod]
    public void TestExecuteShellCommandsWithWorkingDirectoryNotFound()
    {
        ConcurrentDictionary<string, string> workingDirectories = new();
        cacheManagerMock.SetupGet(i => i.WorkingDirectories).Returns(workingDirectories);
        ExecuteShellCommandRequest request = new() {RepositoryId = Guid.NewGuid().ToString()};
        ActionResult<ExecuteShellCommandResponse> actionResult = Controller.ExecuteShellCommands(request);
        ExecuteShellCommandResponse response =  GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
        Assert.IsTrue(response.IsErrorResponse);
        Assert.AreEqual("No working directory was found for the specified repository.", response.ErrorMessage);
    }
    
    /// <summary>
    /// Tests that the <see cref="ShellController.ExecuteShellCommands(ExecuteShellCommandRequest)"/> sends an error
    /// response when the incoming request has a repository identifier that does not have any commands to execute.
    /// </summary>
    [TestMethod]
    public void TestExecuteShellCommandsWithNoCommandsToExecute()
    {
        string repositoryId = Guid.NewGuid().ToString();
        ConcurrentDictionary<string, string> workingDirectories = new()
        {
            [repositoryId] = "working_directory"
        };
        cacheManagerMock.SetupGet(i => i.WorkingDirectories).Returns(workingDirectories);
        ExecuteShellCommandRequest request = new() {RepositoryId = repositoryId};
        ActionResult<ExecuteShellCommandResponse> actionResult = Controller.ExecuteShellCommands(request);
        ExecuteShellCommandResponse response =  GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
        Assert.IsTrue(response.IsErrorResponse);
        Assert.AreEqual("No shell commands were provided to execute.", response.ErrorMessage);
    }
    
    /// <summary>
    /// Tests that the <see cref="ShellController.ExecuteShellCommands(ExecuteShellCommandRequest)"/> sends an error
    /// response when executing the commands produces an exception.
    /// </summary>
    [TestMethod]
    public void TestExecuteShellCommandsWithUnexpectedException()
    {
        string repositoryId = Guid.NewGuid().ToString();
        ConcurrentDictionary<string, string> workingDirectories = new()
        {
            [repositoryId] = "working_directory"
        };
        cacheManagerMock.SetupGet(i => i.WorkingDirectories).Returns(workingDirectories);
        List<string> commands = ["git status", "git push"];
        ExecuteShellCommandRequest request = new()
        {
            RepositoryId = repositoryId,
            Commands = commands
        };
        shellManagerMock.Setup(i => i.ExecuteShellCommands(It.IsAny<string>(), commands)).Throws(new Exception());
        ActionResult<ExecuteShellCommandResponse> actionResult = Controller.ExecuteShellCommands(request);
        ExecuteShellCommandResponse response =  GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
        Assert.IsTrue(response.IsErrorResponse);
        Assert.AreEqual("Error executing shell commands. Check internal server logs for more information.", response.ErrorMessage);
    }
    
    /// <summary>
    /// Tests that the <see cref="ShellController.ExecuteShellCommands(ExecuteShellCommandRequest)"/> sends a successful
    /// response in the nominal case of executing shell commands.
    /// </summary>
    [TestMethod]
    public void TestExecuteShellCommandsNominalCase()
    {
        string repositoryId = Guid.NewGuid().ToString();
        ConcurrentDictionary<string, string> workingDirectories = new()
        {
            [repositoryId] = "working_directory"
        };
        cacheManagerMock.SetupGet(i => i.WorkingDirectories).Returns(workingDirectories);

        List<string> commands = ["git status", "git push"];
        ExecuteShellCommandRequest request = new()
        {
            RepositoryId = repositoryId,
            Commands = commands
        };

        List<string> outputMessages = ["git status was successfully executed.", "git push failed to execute."];
        shellManagerMock.Setup(i => i.ExecuteShellCommands(It.IsAny<string>(), commands)).Returns(outputMessages);

        ActionResult<ExecuteShellCommandResponse> actionResult = Controller.ExecuteShellCommands(request);
        ExecuteShellCommandResponse response =  GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
        Assert.IsFalse(response.IsErrorResponse);
        CollectionAssert.Contains(response.OutputMessages, outputMessages[0]);
        CollectionAssert.Contains(response.OutputMessages, outputMessages[1]);
    }
}