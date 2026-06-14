namespace ChasmaWebApi.Data.Requests.Infrastructure
{
    /// <summary>
    /// Class representing the components of a request to retrieve a random security question for a user.
    /// </summary>
    public class GetRandomSecurityQuestionRequest
    {
        /// <summary>
        /// Gets or sets the username of the user for whom to retrieve a random security question.
        /// </summary>
        public string UserName { get; set; }
    }
}
