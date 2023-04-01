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
    /// <summary>
    /// Test converter, does nothing
    /// </summary>
    [CommandLine("format")]
    public class EmptyConverter : IConverter
    {
        /// <summary>
        /// Empty test method
        /// </summary>
        /// <returns>Empty instance of Task with memory stream</returns>
        public Task<Stream> FromContent(IContent content)
        {
            return Task.Run<Stream>(() => new MemoryStream());
        }

        /// <summary>
        /// Empty test method
        /// </summary>
        /// <param name="file">Can be empty, only here for interface</param>
        /// <returns>Task of empty primitive content</returns>
        public Task<IContent> FromStream(Stream file)
        {
            return Task.Run<IContent>(() => new PrimitiveContent());
        }
    }

    /// <summary>
    /// Empty file manager
    /// </summary>
    [CommandLine("testfile")]
    public class EmptyFileManager : IFileManager
    {
        /// <summary>
        /// Empty test method
        /// </summary>
        /// <param name="config">Can be empty string, does nothing with it</param>
        /// <returns>Empty memory stream</returns>
        public Task<Stream> Load(string config)
        {
            return Task.Run<Stream>(() => new MemoryStream());
        }

        /// <summary>
        /// Empty test method
        /// </summary>
        /// <param name="config">Does nothing with this parameter</param>
        /// <param name="file">Does nothing with this parameter</param>
        /// <returns>Completed task</returns>
        public Task Save(string config, Stream file)
        {
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Test file manager, returns inline console parameter
    /// </summary>
    [CommandLine("inline")]
    public class InlineFileManager : IFileManager
    {
        /// <summary>
        /// Loads memory stream from console input
        /// </summary>
        /// <param name="config">Console param input</param>
        /// <returns>Memory stream from input</returns>
        public Task<Stream> Load(string config)
        {
            return Task.Run<Stream>(() => new MemoryStream(Encoding.UTF8.GetBytes(config)));
        }

        /// <summary>
        /// Snapshot test method
        /// </summary>
        /// <param name="config">Name of the snapshot</param>
        /// <param name="file">Contents to be snapshotted as string</param>
        /// <returns>Completed task</returns>
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

    /// <summary>
    /// Tests whether or not command line processor can load correct classes based on args
    /// </summary>
    /// <param name="args">Console line arguments</param>
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

    /// <summary>
    /// Snapshots of inline loaded data
    /// </summary>
    /// <param name="args">Console line arguments</param>
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

    /// <summary>
    /// Returns console arguments for converting inline data
    /// </summary>
    public static IEnumerable<object[]> TestSnapshotCommandProcessorGenerator =>
        new List<object[]>
        {
            new object[] { new string[] { "-in", "xml", "-out", "json", "-from", "inline", "<?xml version=\"1.0\"?><note><to>Tove</to><from>Jani</from><heading>Reminder</heading><body>Don't forget me this weekend!</body></note>", "-to", "inline", "JsonSnapshot" } },
            new object[] { new string[] { "-in", "json", "-out", "xml", "-from", "inline", "{\"#document\":{\"xml\":{\"#attributes\":{\"version\":\"1.0\"}},\"note\":{\"to\":{\"#text\":\"Tove\"},\"from\":{\"#text\":\"Jani\"},\"heading\":{\"#text\":\"Reminder\"},\"body\":{\"#text\":\"Don't forget me this weekend!\"}}}}", "-to", "inline", "XmlSnapshot" } },
        };

    /// <summary>
    /// Returns console arguments for testing loaded classes based on args
    /// </summary>
    public static IEnumerable<object[]> TestCommandProcessorGenerator =>
        new List<object[]>
        {
            new object[] { new string[] { "-in", "format", "-out", "format", "-from", "testfile", "config", "-to", "testfile", "config" } },
            new object[] { new string[] { "-out", "format", "-in", "format", "-to", "testfile", "config", "-from", "testfile", "config" } },
        };
}