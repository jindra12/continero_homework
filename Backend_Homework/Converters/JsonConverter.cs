using Backend_Homework.Attributes;
using Backend_Homework.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Backend_Homework.Converters
{
    /// <summary>
    /// Converts JSON strings/streams to IContent and back
    /// </summary>
    [CommandLine("json")]
    public class JsonConverter : IConverter
    {
        /// <summary>
        /// Generates IContent from JSON stream
        /// </summary>
        /// <param name="file">Stream containing JSON string representation</param>
        /// <returns>IContent representation of JSON</returns>
        public async Task<IContent> FromStream(Stream file)
        {
            using (var streamReader = new StreamReader(file))
            using (var jsonTextReader = new JsonTextReader(streamReader))
            {
                var json = await JToken.LoadAsync(jsonTextReader);
                return SerializeIntoContent(json);
            }
        }

        /// <summary>
        /// Generates Stream containing JSON string representation
        /// </summary>
        /// <param name="content">model from which JSON is generated</param>
        /// <returns>Stream containing JSON string representation</returns>
        public Task<Stream> FromContent(IContent content)
        {
            var task = Task.Run<Stream>(() =>
            {
                var memoryStream = new MemoryStream();
                var streamWriter = new StreamWriter(memoryStream);
                SerializeIntoStream(content, streamWriter);
                streamWriter.Flush();
                return memoryStream;
            });
            task.ConfigureAwait(false);
            return task;
        }

        /// <summary>
        /// Internal JSON serializer
        /// </summary>
        /// <param name="json">json representation</param>
        /// <returns>content JSON representation</returns>
        private IContent SerializeIntoContent(JToken? json)
        {
            if (json is null || json.Type == JTokenType.None || json.Type == JTokenType.Null)
                return new PrimitiveContent();
            if (json.Type == JTokenType.Array)
            {
                var content = new ArrayContent();
                foreach (var property in json.Children())
                    content.Children.Add(SerializeIntoContent(property));
                return content;
            }
            if (json.Type == JTokenType.Object && json is JObject jObject)
            {
                var content = new ObjectContent();
                foreach (var property in jObject)
                    content.Children[property.Key] = SerializeIntoContent(property.Value);
                return content;
            }

            return new PrimitiveContent
            {
                Value = json.ToObject<object>()
            };
        }

        /// <summary>
        /// Serializes IContent into string JSON representation stream
        /// </summary>
        /// <param name="content">Content to be parsed</param>
        /// <param name="writer">stream into which this method writes</param>
        private void SerializeIntoStream(IContent content, StreamWriter writer)
        {
            if (content is ArrayContent arrayContent)
            {
                writer.Write("[");
                for (var i = 0; i < arrayContent.Children.Count; i++)
                {
                    var child = arrayContent.Children[i];
                    SerializeIntoStream(child, writer);
                    if (i != arrayContent.Children.Count - 1)
                        writer.Write(",");
                }

                writer.Write("]");
            } else if (content is ObjectContent objectContent)
            {
                writer.Write("{");
                var keyList = objectContent.Children.Keys.ToList();
                for (var i = 0; i < keyList.Count; i++)
                {
                    var child = objectContent.Children[keyList[i]];
                    writer.Write($"\"{keyList[i]}\":");
                    SerializeIntoStream(child, writer);
                    if (i != keyList.Count - 1)
                        writer.Write(",");
                }
                writer.Write("}");
            } else if (content is PrimitiveContent primitive) {
                if (primitive.Value is null)
                    writer.Write("null");
                else if (primitive.Value is not string stringValue)
                    writer.Write(primitive.Value.ToString());
                else
                    writer.Write($"\"{stringValue.Replace(Environment.NewLine, "\\n").Replace("\"", "\\\"")}\"");
            } else {
                throw new InvalidOperationException("Unknown type of IContent found");
            }
        }
    }
}