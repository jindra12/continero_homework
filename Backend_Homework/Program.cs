using Backend_Homework.Processor;
using Backend_Homework.UI;

namespace Continero.Homework
{
    /*public class Document // Class in the main space
    {
        public string Title { get; set; }
        public string Text { get; set; }
    }
    class Program
    {
        /// <summary>
        /// This way of programming is not extensible, you cannot easily add formats to process
        /// </summary>
        static void Main(string[] args)
        {
            // Does not accept parameters from args
            var sourceFileName = Path.Combine(Environment.CurrentDirectory, "..\\..\\..\\Source Files\\Document1.xml");


            var targetFileName = Path.Combine(Environment.CurrentDirectory, "..\\..\\..\\Target Files\\Document1.json");
        try // bad formatting
            {
                // Does not check if file is missing
                FileStream sourceStream = File.Open(sourceFileName, FileMode.Open);
                var reader = new StreamReader(sourceStream); // no using() syntax, might leave the file open
                string input = reader.ReadToEnd(); // Won't even compile, string is declared in the wrong scope
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message); // Does not make sense to consume an exception, do nothing and then re-throw a message
            }
            var xdoc = XDocument.Parse(input);
            var doc = new Document
            {
                Title = xdoc.Root.Element("title").Value, // Does not check if the elements are present
                Text = xdoc.Root.Element("text").Value
            };
            var serializedDoc = JsonConvert.SerializeObject(doc);
            var targetStream = File.Open(targetFileName, FileMode.Create, FileAccess.Write);
            var sw = new StreamWriter(targetStream);
            sw.Write(serializedDoc);
        }
    }*/

    class Program
    {
        static void Main(string[] args)
        {
            var processor = new CommandLineProcessor();
            foreach (var arg in args)
                processor.ProcessArgument(arg);
            var task = processor.Run();
            task.ConfigureAwait(false);
            new Loading(task);
        }
    }
}