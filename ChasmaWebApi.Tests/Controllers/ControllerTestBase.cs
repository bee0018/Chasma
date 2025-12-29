using Microsoft.AspNetCore.Mvc;

namespace ChasmaWebApi.Tests.Controllers
{
    /// <summary>
    /// Class representing the base class for controller tests.
    /// </summary>
    /// <typeparam name="T">The type of API controller base that is being tested.</typeparam>
    public class ControllerTestBase<T>
        where T : ControllerBase
    {
        /// <summary>
        /// The full name of the test user.
        /// </summary>
        protected const string TestUserFullName = "Test User";

        /// <summary>
        /// The username of the test user.
        /// </summary>
        protected const string TestUserName = "testUser";

        /// <summary>
        /// The password of the test user.
        /// </summary>
        protected const string TestUserPassword = "password";

        /// <summary>
        /// The email of the test user.
        /// </summary>
        protected const string TestUserEmail = "test.user@email.com";

        /// <summary>
        /// The name of the test repository.
        /// </summary>
        protected const string TestRepositoryName = "TestRepository";

        /// <summary>
        /// The test instance of the <see cref="ControllerBase"/>.
        /// </summary>
        protected T Controller;

        /// <summary>
        /// Extracts the inner response from the action result task.
        /// </summary>
        /// <typeparam name="T">The type of response to retrieve.</typeparam>
        /// <param name="task">The task containing the response.</param>
        /// <param name="objectType">The type of action of result.</param>
        /// <returns>The inner response.</returns>
        protected static T ExtractActionResultInnerResponseFromTask<T>(Task<ActionResult<T>> task, Type objectType)
        {
            ActionResult<T> actionResult = task.Result;
            if (actionResult?.Result == null)
            {
                throw new NullReferenceException("Cannot extract inner result because the action result is null.");
            }

            return ExtractActionResultInnerResponseFromActionResult(actionResult, objectType);
        }

        /// <summary>
        /// Extracts the inner response from the action result.
        /// </summary>
        /// <typeparam name="T">The type of response to retrieve.</typeparam>
        /// <param name="task">The task containing the response.</param>
        /// <param name="objectType">The type of action of result.</param>
        /// <returns>The inner response.</returns>
        protected static T ExtractActionResultInnerResponseFromActionResult<T>(ActionResult<T> actionResult, Type objectType)
        {
            if (objectType == null)
            {
                throw new NullReferenceException("Cannot extract inner result because object result type is null.");
            }

            if (actionResult.Result == null)
            {
                throw new NullReferenceException("Cannot extract inner result because object result null.");
            }

            ObjectResult innerObjectResult;
            if (objectType == typeof(OkObjectResult))
            {
                innerObjectResult = actionResult.Result as OkObjectResult;
            }
            else if (objectType == typeof(BadRequestObjectResult))
            {
                innerObjectResult = actionResult.Result as BadRequestObjectResult;
            }
            else
            {
                throw new ArgumentException("The object type specified is not implemented.");
            }

            return (T)innerObjectResult.Value;
        }
    }
}
