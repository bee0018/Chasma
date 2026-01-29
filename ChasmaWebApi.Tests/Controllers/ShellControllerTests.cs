using System.Collections.Concurrent;
using ChasmaWebApi.Controllers;
using ChasmaWebApi.Data.Interfaces;
using ChasmaWebApi.Data.Objects;
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

        List<ShellCommandResult> commandResults = [
            new ShellCommandResult
            {
                ExecutedCommand = "git status",
                IsSuccess = true,
                OutputMessage = "git status was successfully executed."
            },
            new ShellCommandResult
            {
                ExecutedCommand = "git push",
                IsSuccess = false,
                OutputMessage = "git push failed to execute."
            }
        ];
        shellManagerMock.Setup(i => i.ExecuteShellCommands(It.IsAny<string>(), commands)).Returns(commandResults);

        ActionResult<ExecuteShellCommandResponse> actionResult = Controller.ExecuteShellCommands(request);
        ExecuteShellCommandResponse response =  GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
        Assert.IsFalse(response.IsErrorResponse);

        Assert.AreEqual(response.Results.Count, 2);
        List<string> outputMessages = ["git status was successfully executed.", "git push failed to execute."];
        List<string> shellMessages = response.Results.Select(i => i.OutputMessage).ToList();
        CollectionAssert.Contains(shellMessages, outputMessages.First());
        CollectionAssert.Contains(shellMessages, outputMessages.Last());
    }

    /// <summary>
    /// Tests that the <see cref="ShellController.ExecuteBatchShellCommands(ExecuteBatchShellCommandsRequest)"/> sends an error
    /// response when the incoming request is null.
    /// </summary>
    [TestMethod]
    public void TestExecuteBatchShellCommandsWithNullRequest()
    {
        ActionResult<ExecuteBatchShellCommandsResponse>  actionResult = Controller.ExecuteBatchShellCommands(null);
        ExecuteBatchShellCommandsResponse response =  GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
        Assert.IsTrue(response.IsErrorResponse);
        Assert.AreEqual("Request is null. Cannot execute batch commands.", response.ErrorMessage);
    }

    /// <summary>
    /// Tests that the <see cref="ShellController.ExecuteBatchShellCommands(ExecuteBatchShellCommandsRequest)"/> sends an error
    /// response when the incoming request has no entries with commands to execute.
    /// </summary>
    [TestMethod]
    public void TestExecuteBatchShellCommandsWithNoCommandsToExecute()
    {
        ExecuteBatchShellCommandsRequest request = new();
        ActionResult<ExecuteBatchShellCommandsResponse> actionResult = Controller.ExecuteBatchShellCommands(request);
        ExecuteBatchShellCommandsResponse response =  GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
        Assert.IsTrue(response.IsErrorResponse);
        Assert.AreEqual("No batch shell commands were provided to execute.", response.ErrorMessage);
    }
    
    /// <summary>
    /// Tests that the <see cref="ShellController.ExecuteBatchShellCommands(ExecuteBatchShellCommandsRequest)"/> sends an error
    /// response when there is an exception executing shell commands.
    /// </summary>
    [TestMethod]
    public void TestExecuteBatchShellCommandsWithCommandsThrowingException()
    {
        List<BatchCommandEntry> entries = [new()];
        ExecuteBatchShellCommandsRequest request = new(){ BatchCommands = entries };
        shellManagerMock.Setup(i => i.ExecuteShellCommandsInBatch(It.IsAny<List<BatchCommandEntry>>()))
            .Throws(new Exception("error executing batch commands."));
        ActionResult<ExecuteBatchShellCommandsResponse> actionResult = Controller.ExecuteBatchShellCommands(request);
        ExecuteBatchShellCommandsResponse response =  GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
        Assert.IsTrue(response.IsErrorResponse);
        Assert.AreEqual("Error executing shell commands. Check internal server logs for more information.", response.ErrorMessage);
    }
    
    /// <summary>
    /// Tests that the <see cref="ShellController.ExecuteBatchShellCommands(ExecuteBatchShellCommandsRequest)"/> sends
    /// a successful response when all commands are executed successfully.
    /// </summary>
    [TestMethod]
    public void TestExecuteBatchShellCommandsNominalCase()
    {
        BatchCommandEntry batchCommand1 = new()
        {
            Commands = ["git status", "git push"],
            RepositoryId = Guid.NewGuid().ToString(),
        };
        BatchCommandEntry batchCommand2 = new()
        {
            Commands = ["git log --oneline", "git rev-parse HEAD"],
            RepositoryId = Guid.NewGuid().ToString(),
        };
        List<BatchCommandEntry> entries = [batchCommand1, batchCommand2];
        BatchCommandEntryResult result1 = new()
        {
            IsSuccess = true,
            Message = "commands successful",
            RepositoryName = TestRepositoryName
        };
        BatchCommandEntryResult result2 = new()
        {
            IsSuccess = true,
            Message = "commands successful",
            RepositoryName = "test-name"
        };
        List<BatchCommandEntryResult> results = [result1, result2];
        ExecuteBatchShellCommandsRequest request = new(){ BatchCommands = entries };
        shellManagerMock.Setup(i => i.ExecuteShellCommandsInBatch(It.IsAny<List<BatchCommandEntry>>()))
            .Returns(results);
        ActionResult<ExecuteBatchShellCommandsResponse> actionResult = Controller.ExecuteBatchShellCommands(request);
        ExecuteBatchShellCommandsResponse response =  GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));

        Assert.IsFalse(response.IsErrorResponse);
        Assert.AreEqual(results.Count, 2);

        List<string> repoNames = results.Select(i => i.RepositoryName).ToList();
        CollectionAssert.Contains(repoNames, TestRepositoryName);
        CollectionAssert.Contains(repoNames, result2.RepositoryName);
        
        List<string> outputMessages = results.Select(i => i.Message).ToList();
        CollectionAssert.Contains(outputMessages, result1.Message);
        CollectionAssert.Contains(outputMessages, result2.Message);
    }
}