using ChasmaWebApi.Data.Objects.DryRun;

namespace ChasmaWebApi.Util
{
    /// <summary>
    /// Utility class for handling common operations related to dry run simulations, such as setting error messages and updating simulation results.
    /// </summary>
    public static class DryRunHelper
    {
        /// <summary>
        /// Fails the simulation result and sets the error message to be returned to the client based on the provided error message.
        /// </summary>
        /// <param name="result">The simulation result.</param>
        /// <param name="errorMessage">The simulation error message.</param>
        public static void FailSimulationResult(SimulatedResultBase result, string errorMessage)
        {
            result.IsSuccessful = false;
            result.ErrorMessage = errorMessage;
        }
    }
}
