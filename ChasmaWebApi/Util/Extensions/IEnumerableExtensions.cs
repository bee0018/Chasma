namespace ChasmaWebApi.Util.Extensions
{
    /// <summary>
    /// Class containing extension methods and custom functionality to IEnumerable object types.
    /// </summary>
    public static class IEnumerableExtensions
    {
        /// <summary>
        /// Finds the index of the element based on the equality predicate.
        /// </summary>
        /// <typeparam name="T">The type of object to find.</typeparam>
        /// <param name="collection">The collection to search elements for.</param>
        /// <param name="predicate">The equality predicate to match the index with.</param>
        /// <returns>The index of the element meeting predicate conditions; -1 otherwise.</returns>
        public static int FindIndex<T>(this IEnumerable<T> collection, Func<T, bool> predicate)
        {
            int index = 0;
            foreach (T item in collection)
            {
                if (predicate(item)) return index;
                index++;
            }

            return -1;
        }
    }
}
