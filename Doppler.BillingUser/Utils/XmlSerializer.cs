using System.IO;
using System.Xml;

namespace Doppler.BillingUser.Utils
{
    public static class XmlSerializer
    {
        public static string ToXmlString<T>(T model, XmlWriterSettings settings)
        {
            var xmlSerializer = new System.Xml.Serialization.XmlSerializer(typeof(T));

            using (var stream = new StringWriter())
            using (var writer = XmlWriter.Create(stream, settings))
            {
                xmlSerializer.Serialize(writer, model);
                return stream.ToString();
            }
        }

        public static string ToXmlString<T>(T model)
        {
            return ToXmlString(model, new XmlWriterSettings
            {
                Indent = true,
                OmitXmlDeclaration = true
            });
        }

        public static T FromXmlString<T>(string xml)
        {
            var xmlSerializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
            using (var stringreader = new StringReader(xml))
            {
                return (T)xmlSerializer.Deserialize(stringreader);
            }
        }
    }
}
