using System.Text;
using Backend_Homework.Processor;
using Backend_Homework.Converters;
using Backend_Homework.Models;
using Backend_Homework.FileManagers;
using Backend_Homework.Attributes;
using Snapshooter.Xunit;

namespace Homework.Tests;

public class CommandLineProcessorTest
{
    [CommandLine("format")]
    public class EmptyConverter : IConverter
    {
        public Task<Stream> FromContent(IContent content)
        {
            return Task.Run<Stream>(() => new MemoryStream());
        }

        public Task<IContent> FromStream(Stream file)
        {
            return Task.Run<IContent>(() => new PrimitiveContent());
        }
    }

    [CommandLine("testfile")]
    public class EmptyFileManager : IFileManager
    {
        public Task<Stream> Load(string config)
        {
            return Task.Run<Stream>(() => new MemoryStream());
        }

        public Task Save(string config, Stream file)
        {
            return Task.CompletedTask;
        }
    }

    [CommandLine("inline")]
    public class InlineFileManager : IFileManager
    {
        public Task<Stream> Load(string config)
        {
            return Task.Run<Stream>(() => new MemoryStream(Encoding.UTF8.GetBytes(config)));
        }

        public Task Save(string config, Stream file)
        {
            file.Seek(0, SeekOrigin.Begin);
            using (var reader = new StreamReader(file))
            {
                Snapshot.Match(reader.ReadToEnd(), config);
            }
            return Task.CompletedTask;
        }
    }

    [Theory]
    [MemberData(nameof(TestCommandProcessorGenerator))]
    public async Task CanRunProcessorTestFromCommandLineInput(string[] args)
    {
        var commandLineProcessor = new CommandLineProcessor();
        foreach (var arg in args)
            commandLineProcessor.ProcessArgument(arg);

        var exception = await Record.ExceptionAsync(() => commandLineProcessor.Run());
        Assert.Null(exception);
    }

    [Theory]
    [MemberData(nameof(TestSnapshotCommandProcessorGenerator))]
    public async Task ProcessorTestSnapshots(string[] args)
    {
        Snapshot.FullName(args[9]);
        var commandLineProcessor = new CommandLineProcessor();
        foreach (var arg in args)
            commandLineProcessor.ProcessArgument(arg);
        await commandLineProcessor.Run();
    }

    public static IEnumerable<object[]> TestSnapshotCommandProcessorGenerator =>
        new List<object[]>
        {
            new object[] { new string[] { "-in", "xml", "-out", "json", "-from", "inline", "<?xml version=\"1.0\"?><note><to>Tove</to><from>Jani</from><heading>Reminder</heading><body>Don't forget me this weekend!</body></note>", "-to", "inline", "JsonSnapshot" } },
            new object[] { new string[] { "-in", "json", "-out", "xml", "-from", "inline", "{\"#document\":{\"xml\":{\"#attributes\":{\"version\":\"1.0\"}},\"note\":{\"to\":{\"#text\":\"Tove\"},\"from\":{\"#text\":\"Jani\"},\"heading\":{\"#text\":\"Reminder\"},\"body\":{\"#text\":\"Don't forget me this weekend!\"}}}}", "-to", "inline", "XmlSnapshot" } },
        };

    public static IEnumerable<object[]> TestCommandProcessorGenerator =>
        new List<object[]>
        {
            new object[] { new string[] { "-in", "format", "-out", "format", "-from", "testfile", "config", "-to", "testfile", "config" } },
            new object[] { new string[] { "-out", "format", "-in", "format", "-to", "testfile", "config", "-from", "testfile", "config" } },
        };
}