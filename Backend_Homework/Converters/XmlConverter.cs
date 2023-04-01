using System.Xml;
using Backend_Homework.Attributes;
using Backend_Homework.Models;

namespace Backend_Homework.Converters
{
    [CommandLine("xml")]
    public class XmlConverter : IConverter
    {
        /// <summary>
        /// Taken from: https://learn.microsoft.com/en-us/dotnet/api/system.xml.xmlnode.name?view=net-8.0,
        /// added Attributes for consistent parsing
        /// </summary>
        private struct ReservedNames
        {
            public const string CDATA = "#cdata-section";
            public const string Comment = "#comment";
            public const string Document = "#document";
            public const string DocumentFragment = "#document-fragment";
            public const string Text = "#text";
            public const string Whitespace = "#whitespace";
            public const string SignificantWhitespace = "#significant-whitespace";
            public const string XmlDeclaration = "xml";
            public const string Attributes = "#attributes";
        }

        private static readonly HashSet<string> skippableNames = new HashSet<string>
        {
            ReservedNames.CDATA,
            ReservedNames.Comment,
            ReservedNames.Document,
            ReservedNames.DocumentFragment,
            ReservedNames.Attributes,
        };

        public Task<IContent> FromStream(Stream file)
        {
            return Task.Run<IContent>(() =>
            {
                var xml = new XmlDocument();
                xml.Load(file);
                return SerializeIntoContent(xml, true);
            });
        }

        public Task<Stream> FromContent(IContent content)
        {
            var task = Task.Run<Stream>(() =>
            {
                var memoryStream = new MemoryStream();
                var streamWriter = new StreamWriter(memoryStream);
                SerializeIntoStream(content, streamWriter, true);
                streamWriter.Flush();
                return memoryStream;
            });
            task.ConfigureAwait(false);
            return task;
        }

        private void SerializeIntoStream(IContent content, StreamWriter streamWriter, bool isRoot = false)
        {
            if (isRoot)
            {
                if (content is not ObjectContent rootContent)
                    throw new InvalidOperationException("Cannot serialize non-object content into xml");
                var parentKey = rootContent.Children.Single().Key;
                if (parentKey != ReservedNames.Document)
                    throw new InvalidOperationException($"tag {parentKey} cannot be root in xml, #document is missing");
                else
                {
                    SerializeIntoStream(rootContent.Children.First().Value, streamWriter);
                    return;
                }
            }
            if (content is PrimitiveContent primitive)
                streamWriter.Write(primitive.Value?.ToString() ?? "");
            else if (content is ObjectContent objectContent)
            {
                foreach (var pair in GetNonReservedChildren(objectContent))
                {
                    var value = pair.Value;
                    if (value is ObjectContent objectValue)
                    {
                        if (pair.Key == ReservedNames.XmlDeclaration)
                        {
                            streamWriter.Write("<?xml");
                            SerializeAttributeIntoStream(objectValue, streamWriter);
                            streamWriter.Write("?>");
                        }
                        else
                        {
                            streamWriter.Write($"<{pair.Key}");
                            SerializeAttributeIntoStream(objectValue, streamWriter);
                            streamWriter.Write(">");
                            SerializeIntoStream(value, streamWriter);
                            streamWriter.Write($"</{pair.Key}>");
                        }
                    }
                    else if (value is ArrayContent arrayValue)
                    {
                        foreach (var element in arrayValue.Children)
                        {
                            if (element is not ObjectContent objectElement)
                                throw new InvalidOperationException("Invalid content structure, this should not happen with valid xml");
                            else
                            {
                                streamWriter.Write($"<{pair.Key}");
                                SerializeAttributeIntoStream(objectElement, streamWriter);
                                streamWriter.Write(">");
                                SerializeIntoStream(value, streamWriter);
                                streamWriter.Write($"</{pair.Key}>");
                            }
                        }
                    }
                    else if (value is PrimitiveContent primitiveValue)
                        SerializeIntoStream(primitiveValue, streamWriter);
                    else
                        throw new InvalidOperationException("Unknown type of content detected");
                }
            }
            else
                throw new InvalidOperationException("Invalid content structure, this should not happen with valid xml");
        }

        private IList<KeyValuePair<string, IContent>> GetNonReservedChildren(ObjectContent content)
        {
            var accumulator = new List<KeyValuePair<string, IContent>>();
            foreach (var pair in content.Children)
                if (!skippableNames.Contains(pair.Key))
                    accumulator.Add(pair);
            return accumulator;
        }

        private void SerializeAttributeIntoStream(ObjectContent content, StreamWriter streamWriter)
        {
            if (content.Children.ContainsKey(ReservedNames.Attributes))
            {
                var attributes = content.Children[ReservedNames.Attributes];
                if (attributes is not ObjectContent objectAttributes)
                    throw new InvalidOperationException($"{content.Children[ReservedNames.Attributes]} child must be an object");
                foreach (var attribute in objectAttributes.Children)
                {
                    var value = attribute.Value;
                    if (value is not PrimitiveContent primitive)
                        throw new InvalidOperationException("Cannot serialize non-primitive xml attribute");
                    if (primitive.Value is not null)
                        streamWriter.Write($" {attribute.Key}=\"{primitive.Value.ToString()}\"");
                }
            }
        }

        private IContent SerializeIntoContent(XmlNode xml, bool rootElement = false)
        {
            if (xml.NodeType == XmlNodeType.Text || xml.NodeType == XmlNodeType.Whitespace || xml.NodeType == XmlNodeType.SignificantWhitespace)
            {
                return new PrimitiveContent
                {
                    Value = xml.InnerText
                };
            }

            var (trueParent, toWriteIn) = BuildObjectContent(xml, rootElement);

            var xmlList = xml.ChildNodes.Cast<XmlNode>().GroupBy((node) => node.Name);
            foreach (var group in xmlList)
            {
                if (group.Count() > 1)
                {
                    toWriteIn.Children[group.First().Name] = new ArrayContent
                    {
                        Children = group.Select((element) => SerializeIntoContent(element)).ToList()
                    };
                }
                else
                {
                    var child = group.First();
                    toWriteIn.Children[child.Name] = SerializeIntoContent(child);
                }
            }
            var xmlAttributes = GetAttributes(xml);

            if (xmlAttributes.Count > 0)
            {
                var attributes = new ObjectContent();
                foreach (var (Name, Value) in xmlAttributes)
                {
                    attributes.Children[Name] = new PrimitiveContent
                    {
                        Value = Value
                    };
                }
                toWriteIn.Children[ReservedNames.Attributes] = attributes;
            }

            return trueParent;
        }

        private IList<(string Name, string Value)> GetAttributes(XmlNode xml)
        {
            var accumulator = new List<(string Name, string Value)>();
            if (xml.Name == ReservedNames.XmlDeclaration)
            {
                var attributes = (xml.Value ?? "").Split(" ");
                foreach (var attribute in attributes)
                {
                    var attributeSplit = attribute.Split("=");
                    var attributeName = attributeSplit[0];
                    var cleanAttributeValue = attributeSplit[1].Replace("\"", "");
                    accumulator.Add((attributeName, cleanAttributeValue));
                }

            }
            else if (xml.Attributes?.Count > 0)
            {
                foreach (XmlAttribute attribute in xml.Attributes)
                {
                    accumulator.Add((attribute.Name, attribute.Value));
                }
            }
            return accumulator;
        }

        private (ObjectContent toReturn, ObjectContent toWriteIn) BuildObjectContent(XmlNode xml, bool rootElement)
        {
            if (rootElement)
            {
                var trueParent = new ObjectContent();
                var toWriteIn = new ObjectContent();
                trueParent.Children[xml.Name] = toWriteIn;
                return (trueParent, toWriteIn);
            }
            var parent = new ObjectContent();
            return (parent, parent);
        }
    }
}