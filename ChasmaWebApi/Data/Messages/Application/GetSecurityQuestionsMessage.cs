namespace ChasmaWebApi.Data.Messages.Application
{
    /// <summary>
    /// Class representing the components of a message to get the security questions for a user account for password reset purposes.
    /// </summary>
    public class GetSecurityQuestionsMessage
    {
        /// <summary>
        /// Gets or sets the first set of security questions.
        /// </summary>
        public List<string> SecurityQuestionsFirstSet { get; set; } = [];

        /// <summary>
        /// Gets or sets the second set of security questions.
        /// </summary>
        public List<string> SecurityQuestionsSecondSet { get; set; } = [];

        /// <summary>
        /// Gets or sets the third set of security questions.
        /// </summary>
        public List<string> SecurityQuestionsThirdSet { get; set; } = [];
    }
}
