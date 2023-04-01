using Backend_Homework.Processor;
using Backend_Homework.Converters;
using Backend_Homework.Models;
using Backend_Homework.FileManagers;
using Backend_Homework.Attributes;

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

    public static IEnumerable<object[]> TestCommandProcessorGenerator =>
        new List<object[]>
        {
            new object[] { new string[] { "-in", "format", "-out", "format", "-from", "testfile", "config", "-to", "testfile", "config" } },
            new object[] { new string[] { "-out", "format", "-in", "format", "-to", "testfile", "config", "-from", "testfile", "config" } },
        };
}