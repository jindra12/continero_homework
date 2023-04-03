using System.Diagnostics.CodeAnalysis;
using Backend_Homework.Attributes;
using Backend_Homework.Converters;
using Backend_Homework.FileManagers;

namespace Backend_Homework.Processor
{
    /// <summary>
    /// Builds a conversion with saving and loading through console commands
    /// </summary>
    public class CommandLineProcessor
    {
        /// <summary>
        /// Map of possible types of converters, using CommandLineAttribute value as index
        /// </summary>
        private readonly Lazy<IDictionary<string, Type>> converters = new Lazy<IDictionary<string, Type>>(() => LoadTypesFromAssembly(typeof(IConverter)));
        /// <summary>
        /// Map of possible types of file loaders, using CommandLineAttribute value as index
        /// </summary>
        private readonly Lazy<IDictionary<string, Type>> fileManagers = new Lazy<IDictionary<string, Type>>(() => LoadTypesFromAssembly(typeof(IFileManager)));

        /// <summary>
        /// Converter to be applied to input
        /// </summary>
        private IConverter? inConverter;
        
        /// <summary>
        /// Converter to be applied to output
        /// </summary>
        private IConverter? outConverter;
        
        /// <summary>
        /// Input file manager
        /// </summary>
        private IFileManager? inFileManager;
        
        /// <summary>
        /// Output file manager
        /// </summary>
        private IFileManager? outFileManager;
        
        /// <summary>
        /// Configuration for input file manager
        /// </summary>
        private string? inFileManagerConfig;

        /// <summary>
        /// Configuration for output file manager
        /// </summary>
        private string? outFileManagerConfig;

        /// <summary>
        /// Describes names of console arguments
        /// </summary>
        private struct ArgumentTypes
        {
            /// <summary>
            /// In format argument name
            /// </summary>
            public const string InFormat = "-in";
            /// <summary>
            /// Out format argument name
            /// </summary>
            public const string OutFormat = "-out";
            /// <summary>
            /// Input file configuration
            /// </summary>
            public const string InFile = "-from";
            /// <summary>
            /// Output file configuration
            /// </summary>
            public const string OutFile = "-to";
        }

        /// <summary>
        /// States of builder from console args
        /// </summary>
        private enum ProcessorState
        {
            /// <summary>
            /// Is expecting one of argument types
            /// </summary>
            ExpectingConfiguration,
    
            /// <summary>
            /// Expects the next argument to be input format for conversion
            /// </summary>
            ExpectingInFormat,
    
            /// <summary>
            /// Expects the next argument to be output format for conversion
            /// </summary>
            ExpectingOutFormat,

            /// <summary>
            /// Expect the next argument to be type of file manager
            /// </summary>
            ExpectingInFileManager,

            /// <summary>
            /// Expect the next argument to be type of file manager
            /// </summary>
            ExpectingOutFileManager,

            /// <summary>
            /// Expects the next argument to be file manager input configuration
            /// </summary>
            ExpectingInFileManagerConfig,

            /// <summary>
            /// Expects the next argument to be file manager output configuration
            /// </summary>
            ExpectingOutFileManagerConfig,
        }

        /// <summary>
        /// Current state of processor
        /// </summary>
        private ProcessorState state = ProcessorState.ExpectingConfiguration;

        /// <summary>
        /// Processes a single argument from console
        /// </summary>
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

        /// <summary>
        /// After processing arguments, run this method to convert files
        /// </summary>
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

        /// <summary>
        /// Tries to parse next state from argument types
        /// </summary>
        /// <param name="argument">argument from console line</param>
        /// <param name="nextState">Will be null if argument does not match one of ArgumentTypes struct const values</param>
        /// <returns>True, if argument matches one of ArgumentTypes</returns>
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

        /// <summary>
        /// Matches a type with console line argument
        /// </summary>
        /// <typeparam name="T">An instance of interface T matching the string argument through an attribute</typeparam>
        private T GetInstanceFromCommandLine<T>(IDictionary<string, Type> types, string argument) where T : class
        {
            if (!types.ContainsKey(argument))
                throw new ArgumentException("Cannot find specified parser/loader by argument");
            return Activator.CreateInstance(types[argument]) as T ?? throw new InvalidOperationException($"Cannot create class of type {nameof(T)}");
        }

        /// <summary>
        /// Loads types from current assembly that match specific Type and have the CommandLine Attribute
        /// </summary>
        /// <param name="baseType">Interface type to match from current assembly</param>
        /// <returns>A map of types matching the Type from parameter</returns>
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