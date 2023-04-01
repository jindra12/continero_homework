using System.Text;
using Backend_Homework.Converters;

namespace Homework.Tests;

public class ConverterTests
{
    private JsonConverter jsonConverter = new JsonConverter();
    private XmlConverter xmlConverter = new XmlConverter();

    [Theory]
    [MemberData(nameof(TestJsonGenerator))]
    public async Task JsonSerializationTests(string expected) =>
        await BasicSerializationTest(jsonConverter, expected);

    [Theory]
    [MemberData(nameof(TestXmlGenerator))]
    public async Task XmlSerializationTests(string expected) =>
        await BasicSerializationTest(xmlConverter, expected);

    private async Task BasicSerializationTest(IConverter converter, string expected)
    {
        var content = await converter.FromStream(GetStreamFromString(expected));
        var actual = GetStringFromStream(await converter.FromContent(content));
        Assert.Equal(expected, actual);
    }

    private Stream GetStreamFromString(string text)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(text)); 
    }

    private string GetStringFromStream(Stream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);
        using (var reader = new StreamReader(stream))
        {
            return reader.ReadToEnd();
        }
    }

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

    public static IEnumerable<object[]> TestXmlGenerator =>
        new List<object[]>
            {
                new object[] { "<?xml version=\"1.0\"?><note><to>Tove</to><from>Jani</from><heading>Reminder</heading><body>Don't forget me this weekend!</body></note>" },
                new object[] { "<?xml version=\"1.0\"?><note to=\"1\" from=\"2\">One, two<white></white></note>" },
            };
}