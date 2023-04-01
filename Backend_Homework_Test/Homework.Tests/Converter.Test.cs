using System.Text;
using Backend_Homework.Converters;

namespace Homework.Tests;

public class ConverterTests
{
    /// <summary>
    /// Instance of JSON converter, converts JSON to IContent and back
    /// </summary>
    private JsonConverter jsonConverter = new JsonConverter();

    /// <summary>
    /// Instance of XML converter, converts XML to IContent and back
    /// </summary>
    private XmlConverter xmlConverter = new XmlConverter();

    /// <summary>
    /// Tests JSON serialization to IContent and back
    /// </summary>
    /// <param name="expected">JSON input</param>
    [Theory]
    [MemberData(nameof(TestJsonGenerator))]
    public async Task JsonSerializationTests(string expected) =>
        await BasicSerializationTest(jsonConverter, expected);

    /// <summary>
    /// Tests XML serialization to IContent and back
    /// </summary>
    /// <param name="expected">XML input</param>
    [Theory]
    [MemberData(nameof(TestXmlGenerator))]
    public async Task XmlSerializationTests(string expected) =>
        await BasicSerializationTest(xmlConverter, expected);

    /// <summary>
    /// Run serialization test
    /// </summary>
    /// <param name="converter">Serializer instance</param>
    /// <param name="expected">input to convert to IContent</param>
    private async Task BasicSerializationTest(IConverter converter, string expected)
    {
        var content = await converter.FromStream(GetStreamFromString(expected));
        var actual = GetStringFromStream(await converter.FromContent(content));
        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// Converts stream to string
    /// </summary>
    /// <param name="text">string to be converted to stream</param>
    private Stream GetStreamFromString(string text)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(text)); 
    }

    /// <summary>
    /// Converts stream to string
    /// </summary>
    /// <param name="stream">stream to be converted into string</param>
    private string GetStringFromStream(Stream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);
        using (var reader = new StreamReader(stream))
        {
            return reader.ReadToEnd();
        }
    }

    /// <summary>
    /// Returns test JSONs
    /// </summary>
    public static IEnumerable<object[]> TestJsonGenerator =>
        new List<object[]>
        {
            new object[] { "4" },
            new object[] { "{}" },
            new object[] { "[]" },
            new object[] { "{\"test\":1}" },
            new object[] { "{\"test\":[1]}" },
            new object[] { "{\"test\":[1,2,3]}" },
            new object[] { "{\"test\":[1,{\"a\":[{\"b\":2}]},3]}" },
            new object[] { "[{\"test\":1}]" },
        };

    /// <summary>
    /// Returns test XMLs
    /// </summary>
    public static IEnumerable<object[]> TestXmlGenerator =>
        new List<object[]>
            {
                new object[] { "<?xml version=\"1.0\"?><note><to>Tove</to><from>Jani</from><heading>Reminder</heading><body>Don't forget me this weekend!</body></note>" },
                new object[] { "<?xml version=\"1.0\"?><note to=\"1\" from=\"2\">One, two<white></white></note>" },
            };
}