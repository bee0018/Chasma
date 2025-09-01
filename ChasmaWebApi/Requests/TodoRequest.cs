namespace ChasmaWebApi.Requests
{
    /// <summary>
    /// Class representing the basis for a todo request.
    /// </summary>
    public class TodoRequest
    {
        /// <summary>
        /// Gets or sets the identifier of the todo item.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the request is being screamed.
        /// </summary>
        public bool Scream { get; set; }
    }
}
