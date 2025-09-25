using System.Text;
using System.Xml.Serialization;

namespace ChasmaWebApi.Util
{
    /// <summary>
    /// Class representing the base functionality of the XML serialization/deserialzation of custom Chasma objects.
    /// </summary>
    public class ChasmaXmlBase : ICloneable
    {
        /// <summary>
        /// Deserializes XML content to a tactical object.
        /// </summary>
        /// <typeparam name="T">The type of class to be deserialized.</typeparam>
        /// <param name="xmlContent">The XML content.</param>
        /// <returns>The Chasma concrete object formed from XML.</returns>
        public static T? DeserializeToObject<T>(string xmlContent)
            where T : class
        {
            XmlSerializer serializer = new(typeof(T));
            byte[] xmlConvertedByteArray = Encoding.UTF8.GetBytes(xmlContent);
            string decodedXml = Encoding.UTF8.GetString(xmlConvertedByteArray);
            using StringReader reader = new(decodedXml);
            return serializer.Deserialize(reader) as T;
        }

        /// <summary>
        /// Deserializes XML from a file to a concrete object.
        /// </summary>
        /// <typeparam name="T">The type of class to be deserialized to.</typeparam>
        /// <param name="filePath">The file path to the XML content.</param>
        /// <returns>The Chasma concrete object formed from the XML.</returns>
        public static T? DeserializeFromFile<T>(string filePath)
            where T : class
        {
            XmlSerializer serializer = new(typeof(T));
            using StreamReader reader = new(filePath);
            return serializer.Deserialize(reader) as T;
        }

        /// <summary>
        /// Generates XML from objects that inherit from <see cref="ChasmaXmlBase"/>.
        /// </summary>
        /// <typeparam name="T">The type of object to generate XML for.</typeparam>
        /// <param name="xmlObject">The concrete object to generate XML for.</param>
        /// <returns>The XML string representation of the object.</returns>
        public static string GenerateXml<T>(T xmlObject)
            where T : ChasmaXmlBase
        {
            if (xmlObject == null)
            {
                throw new Exception("Cannot generate XML from null object.");
            }

            if (xmlObject is not ChasmaXmlBase)
            {
                throw new InvalidOperationException($"The object does not inherit from {nameof(ChasmaXmlBase)}");
            }

            XmlSerializer serializer = new(typeof(T));
            using StringWriter writer = new();
            serializer.Serialize(writer, xmlObject);
            return writer.ToString();
        }

        /// <inheritdoc/>
        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
