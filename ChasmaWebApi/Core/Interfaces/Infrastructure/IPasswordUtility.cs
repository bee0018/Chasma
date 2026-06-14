using ChasmaWebApi.Data.Models;

namespace ChasmaWebApi.Core.Interfaces.Infrastructure
{
    /// <summary>
    /// Defines the members of the password utility interface.
    /// </summary>
    public interface IPasswordUtility
    {
        /// <summary>
        /// Hash the given password using PBKDF2 with a random salt.
        /// </summary>
        /// <param name="password">The plain text password.</param>
        /// <returns>The hashed password with the salt.</returns>
        (string Hash, byte[] Salt) HashPassword(string password);

        /// <summary>
        /// Verifies if the stored hash matches the hash of the given password and salt.
        /// </summary>
        /// <param name="password">The login password.</param>
        /// <param name="salt">The user account salt.</param>
        /// <param name="storedHash">The database password.</param>
        /// <returns>True if the passwords match; false otherwise.</returns>
        bool VerifyPassword(string password, byte[] salt, string storedHash);

        /// <summary>
        /// Determines if the given password meets the defined strength requirements, such as minimum length, complexity, etc.
        /// Note: Is valid if there is at least one uppercase letter, one lowercase letter, one digit, one special character, and is at least 10 characters long.
        /// </summary>
        /// <param name="password">The plain text password to validate.</param>
        /// <returns>True if the password meets the strength requirements; false otherwise. </returns>
        bool IsPasswordValid(string password);

        /// <summary>
        /// Gets the security question for the user account that is randomly selected.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>The randomly selected security question.</returns>
        string GetRandomSecurityQuestion(UserAccountModel user);

        /// <summary>
        /// Gets the security answer for the user account that is randomly selected.
        /// </summary>
        /// <param name="user">The user to validate.</param>
        /// <param name="securityQuestion">The security question to validate.</param>
        /// <param name="plainTextSecurityAnswer">The security question's answer in plain text.</param>
        /// <returns>True if the security question is valid; false otherwise.</returns>
        bool VerifySecurityQuestionAnswer(UserAccountModel user, string securityQuestion, string plainTextSecurityAnswer);

        /// <summary>
        /// Gets the list of security questions for the user account, categorized by type (fact-based, personal favorite, family and relationship).
        /// </summary>
        /// <returns>The list of security questions.</returns>
        (List<string> FactBasedQuestions, List<string> PersonalFavoriteQuestions, List<string> FamilyAndRelationshipQuestions) GetSecurityQuestions();
    }
}
