using System;
using CommandLine;

namespace ConsoleApplication
{
    public class Options
    {
        [Option(HelpText = "Enable debug mode - more detailed logging")]
        public bool Debug { get; set; } = false;

        [Option(HelpText = "Read text input from stdin instead of listening to audio. Useful for debugging.")]
        public bool Read { get; set; } = false;

        [Option(HelpText = "The port on which to attempt to connect to the Sonogram server")]
        public string Port { get; set; } = Environment.GetEnvironmentVariable("SONOGRAM_PORT");

        [Option(HelpText = "The grammar to recognize (e.g. the programming language)")]
        public string Grammar { get; set; } = "default";
    }

    internal class Program
    {
        public static void Main(string[] args)
        {
            // Parse settings
            Options parsedArgs = null;
            Parser.Default.ParseArguments<Options>(args)
                .WithNotParsed(errors =>
                {
                    foreach (var error in errors)
                        Console.Error.WriteLine(error.ToString());
                    Environment.Exit(1);
                })
                .WithParsed(parsed => { parsedArgs = parsed; });
            
            // Start the engine
            var engine = new SonogramEngine(parsedArgs);
            engine.Init();
        }
    }
}