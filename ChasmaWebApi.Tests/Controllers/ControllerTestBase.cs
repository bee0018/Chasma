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
        /// Extracts the response from the HTTP action request.
        /// </summary>
        /// <typeparam name="TResponseType">The type of response to retrieve.</typeparam>
        /// <param name="task">The task containing the response.</param>
        /// <param name="actionResultType">The type of action of result.</param>
        /// <returns>The inner response.</returns>
        protected static TResponseType GetResponseFromHttpAction<TResponseType>(Task<ActionResult<TResponseType>> task, Type actionResultType)
        {
            ActionResult<TResponseType> actionResult = task.Result;
            if (actionResult?.Result == null)
            {
                throw new NullReferenceException("Cannot extract inner result because the action result is null.");
            }

            return GetResponseFromHttpAction(actionResult, actionResultType);
        }
        
        /// <summary>
        /// Extracts the response from the HTTP action result.
        /// </summary>
        /// <param name="actionResult">The action result containing the response.</param>
        /// <param name="actionResultType">The type of action result.</param>
        /// <typeparam name="TResponseType">The type of response to be extracted</typeparam>
        /// <returns>The inner response.</returns>
        protected static TResponseType GetResponseFromHttpAction<TResponseType>(ActionResult<TResponseType> actionResult, Type actionResultType)
        {
            if (actionResultType == null)
            {
                throw new NullReferenceException("Cannot extract inner result because object result type is null.");
            }

            if (actionResult.Result == null)
            {
                throw new NullReferenceException("Cannot extract inner result because object result null.");
            }

            ObjectResult innerObjectResult;
            if (actionResultType == typeof(OkObjectResult))
            {
                innerObjectResult = actionResult.Result as OkObjectResult;
            }
            else if (actionResultType == typeof(BadRequestObjectResult))
            {
                innerObjectResult = actionResult.Result as BadRequestObjectResult;
            }
            else
            {
                throw new ArgumentException("The object type specified is not implemented.");
            }

            return (TResponseType)innerObjectResult.Value;
        }
    }
}
