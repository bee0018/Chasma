using ChasmaWebApi.Core.Interfaces.Infrastructure;
using ChasmaWebApi.Data.Models;
using System.Security.Cryptography;

namespace ChasmaWebApi.Util
{
    /// <summary>
    /// Utility class for password hashing and verification.
    /// </summary>
    public class PasswordUtility : IPasswordUtility
    {
        // <inheritdoc/>
        public (string Hash, byte[] Salt) HashPassword(string password)
        {
            byte[] salt = new byte[16];
            using RandomNumberGenerator rng = RandomNumberGenerator.Create();
            rng.GetBytes(salt);
            byte[] derivedKey = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100000, HashAlgorithmName.SHA256, 32);
            string hash = Convert.ToBase64String(derivedKey);
            return (hash, salt);
        }

        // <inheritdoc/>
        public bool VerifyPassword(string password, byte[] salt, string storedHash)
        {
            byte[] derivedKey = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100000, HashAlgorithmName.SHA256, 32);
            string hash = Convert.ToBase64String(derivedKey);
            return hash == storedHash;
        }

        // <inheritdoc/>
        public bool IsPasswordValid(string password)
        {
            bool hasLower = false;
            bool hasUpper = false;
            bool hasSymbol = false;
            bool hasDigit = false;
            foreach (char character in password)
            {
                if (char.IsLower(character))
                {
                    hasLower = true;
                }
                else if (char.IsUpper(character))
                {
                    hasUpper = true;
                }
                else if (!char.IsLetterOrDigit(character))
                {
                    hasSymbol = true;
                }
                else if (char.IsDigit(character))
                {
                    hasDigit = true;
                }

                if (hasLower && hasUpper && hasSymbol && hasDigit && password.Length >= 10)
                {
                    return true;
                }
            }

            return false;
        }

        // <inheritdoc/>
        public string GetRandomSecurityQuestion(UserAccountModel user)
        {
            Random random = new();
            int randomNumber = random.Next(1, 4);
            return randomNumber switch
            {
                1 => user.FirstSecurityQuestion,
                2 => user.SecondSecurityQuestion,
                3 => user.ThirdSecurityQuestion,
                _ => throw new InvalidOperationException("Random number out of range."),
            };
        }

        // <inheritdoc/>
        public bool VerifySecurityQuestionAnswer(UserAccountModel user, string securityQuestion, string plainTextSecurityAnswer)
        {
            if (securityQuestion == user.FirstSecurityQuestion)
            {
                return VerifyPassword(plainTextSecurityAnswer, user.FirstSecurityAnswerSalt, user.FirstSecurityAnswer);
            }
            else if (securityQuestion == user.SecondSecurityQuestion)
            {
                return VerifyPassword(plainTextSecurityAnswer, user.SecondSecurityAnswerSalt, user.SecondSecurityAnswer);
            }
            else if (securityQuestion == user.ThirdSecurityQuestion)
            {
                return VerifyPassword(plainTextSecurityAnswer, user.ThirdSecurityAnswerSalt, user.ThirdSecurityAnswer);
            }
            else
            {
                throw new InvalidOperationException("Invalid security question.");
            }
        }

        // <inheritdoc/>
        public (List<string> FactBasedQuestions, List<string> PersonalFavoriteQuestions, List<string> FamilyAndRelationshipQuestions) GetSecurityQuestions()
        {
            List<string> factBasedQuestions =
                [
                "What was the name of your first elementary school?",
                "In what city or town did your parents meet?",
                "What was the name of your first pet?",
                "What was your childhood nickname?",
                "What was the model of your first car?",
                "On what street did you grow up?",
                "What was your favorite childhood cartoon?",
                "What was the name of your first boss?",
                "In what city was your first job?",
                "What was the first concert you attended?"
                ];

            List<string> personalFavoriteQuestions =
                [
                "What is your favorite movie of all time?",
                "What is the name of your favorite book?",
                "What is your favorite food or dish?",
                "What is your favorite sport or sports team?",
                "What was your favorite subject in high school?",
                "What is your favorite singer or musical band?",
                "What is your dream vacation destination?",
                "What is your favorite color?",
                "What is the name of your favorite video game?",
                "What is your favorite holiday?"
                ];

            List<string> familyAndRelationshipQuestions =
                [
                    "What is your maternal grandmother's maiden name?",
                    "What is the middle name of your youngest sibling?",
                    "In what city was your mother born?",
                    "In what city was your father born?",
                    "What is the first name of your oldest cousin?",
                    "What is the name of the hospital where you were born?",
                    "What is your spouse's/partner's mother's first name?",
                    "What is the middle name of your oldest child?",
                    "What year did your parents get married?",
                    "What is the first name of your childhood best friend?"
                ];

            return (factBasedQuestions, personalFavoriteQuestions, familyAndRelationshipQuestions);
        }
    }
}