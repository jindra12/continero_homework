using System.Diagnostics.CodeAnalysis;
using Backend_Homework.Attributes;
using Backend_Homework.Converters;
using Backend_Homework.FileManagers;

namespace Backend_Homework.Processor
{
    public class CommandLineProcessor
    {
        private readonly Lazy<IDictionary<string, Type>> converters = new Lazy<IDictionary<string, Type>>(() => LoadTypesFromAssembly(typeof(IConverter)));
        private readonly Lazy<IDictionary<string, Type>> fileManagers = new Lazy<IDictionary<string, Type>>(() => LoadTypesFromAssembly(typeof(IFileManager)));

        private IConverter? inConverter;
        private IConverter? outConverter;
        private IFileManager? inFileManager;
        private IFileManager? outFileManager;
        private string? inFileManagerConfig;
        private string? outFileManagerConfig;

        private struct ArgumentTypes
        {
            public const string InFormat = "-in";
            public const string OutFormat = "-out";
            public const string InFile = "-from";
            public const string OutFile = "-to";
        }

        private enum ProcessorState
        {
            ExpectingConfiguration,
            ExpectingInFormat,
            ExpectingOutFormat,
            ExpectingInFileManager,
            ExpectingOutFileManager,
            ExpectingInFileManagerConfig,
            ExpectingOutFileManagerConfig,
        }

        private ProcessorState state = ProcessorState.ExpectingConfiguration;

        public CommandLineProcessor ProcessArgument(string argument)
        {
            if (TryParseCommandState(argument, out var nextState))
            {
                if (state != ProcessorState.ExpectingConfiguration)
                    throw new ArgumentException($"Missing configuration, current state: {state.ToString()}");
                state = nextState.Value;
            }
            else
            {
                switch (state)
                {
                    case ProcessorState.ExpectingConfiguration:
                        throw new ArgumentException($"Missing argument, try setting file format or type");
                    case ProcessorState.ExpectingInFileManager:
                        inFileManager = GetInstanceFromCommandLine<IFileManager>(fileManagers.Value, argument);
                        state = ProcessorState.ExpectingInFileManagerConfig;
                        break;
                    case ProcessorState.ExpectingInFileManagerConfig:
                        inFileManagerConfig = argument;
                        state = ProcessorState.ExpectingConfiguration;
                        break;
                    case ProcessorState.ExpectingInFormat:
                        inConverter = GetInstanceFromCommandLine<IConverter>(converters.Value, argument);
                        state = ProcessorState.ExpectingConfiguration;
                        break;
                    case ProcessorState.ExpectingOutFileManager:
                        outFileManager = GetInstanceFromCommandLine<IFileManager>(fileManagers.Value, argument);
                        state = ProcessorState.ExpectingOutFileManagerConfig;
                        break;
                    case ProcessorState.ExpectingOutFileManagerConfig:
                        outFileManagerConfig = argument;
                        state = ProcessorState.ExpectingConfiguration;
                        break;
                    case ProcessorState.ExpectingOutFormat:
                        outConverter = GetInstanceFromCommandLine<IConverter>(converters.Value, argument);
                        state = ProcessorState.ExpectingConfiguration;
                        break;
                }
            }
            return this;
        }

        public async Task Run()
        {
            if (inConverter is null || outConverter is null || outFileManager is null || inFileManager is null || inFileManagerConfig is null || outFileManager is null || outFileManagerConfig is null)
                throw new InvalidOperationException("Cannot process this command line output");
            
            using(var inFileStream = await inFileManager.Load(inFileManagerConfig))
            {
                var inContent = await inConverter.FromStream(inFileStream);
                using (var outStream = await outConverter.FromContent(inContent))
                {
                    await outFileManager.Save(outFileManagerConfig, outStream);
                }
            }
        }

        private bool TryParseCommandState(string argument, [NotNullWhen(true)] out ProcessorState? nextState)
        {
            switch (argument)
            {
                case ArgumentTypes.InFormat:
                    nextState = ProcessorState.ExpectingInFormat;
                    return true;
                case ArgumentTypes.OutFormat:
                    nextState = ProcessorState.ExpectingOutFormat;
                    return true;
                case ArgumentTypes.InFile:
                    nextState = ProcessorState.ExpectingInFileManager;
                    return true;
                case ArgumentTypes.OutFile:
                    nextState = ProcessorState.ExpectingOutFileManager;
                    return true;
                default:
                    nextState = null;
                    return false;
            }
        }

        private T GetInstanceFromCommandLine<T>(IDictionary<string, Type> types, string argument) where T : class
        {
            if (!types.ContainsKey(argument))
                throw new ArgumentException("Cannot find specified parser/loader by argument");
            return Activator.CreateInstance(types[argument]) as T ?? throw new InvalidOperationException($"Cannot create class of type {nameof(T)}");
        }

        private static IDictionary<string, Type> LoadTypesFromAssembly(Type baseType)
        {
            return AppDomain
                .CurrentDomain
                .GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => baseType.IsAssignableFrom(type) && type.IsClass).ToDictionary((type) => {
                    var classAttributes = type.GetCustomAttributes(typeof(CommandLineAttribute), true);
                    if (classAttributes.Count() != 1)
                        throw new TypeLoadException($"Cannot load {type.Name} implementation of {baseType.Name} that does not have precisely one CommandLineAttribute");
                    var commandLineAttribute = classAttributes.Cast<CommandLineAttribute>().Single();
                    return commandLineAttribute.Argument;
                });
        }
    }
}