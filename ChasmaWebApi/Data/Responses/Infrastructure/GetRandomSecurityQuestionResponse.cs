namespace ChasmaWebApi.Data.Responses.Infrastructure
{
    /// <summary>
    /// Class representing a response to a request to retrieve a random security question for a user in the application.
    /// </summary>
    public class GetRandomSecurityQuestionResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets the random security question retrieved for the user.
        /// </summary>
        public string SecurityQuestion { get; set; }
    }
}
