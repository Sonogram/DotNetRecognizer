using System;
using System.IO;
using System.IO.Pipes;
using System.Speech.Recognition;
using System.Text;
using ConsoleApplication.Properties;

namespace ConsoleApplication
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            // Setup speech recognition
            var sr = new SpeechRecognitionEngine();
            sr.SetInputToDefaultAudioDevice();
            sr.EndSilenceTimeout = sr.InitialSilenceTimeout = new TimeSpan(0, 0, 0, 0, 100);

            // Load the grammar
            var grammar = new MemoryStream(Encoding.UTF8.GetBytes(Resources.grammar));
            sr.LoadGrammar(new Grammar(grammar));

            // Connect to the pipe
            using (var client = new NamedPipeClientStream(".", "sonogram", PipeDirection.In,
                PipeOptions.Asynchronous))
            {
                Console.Write("Attempting to connect to pipe...");
                client.Connect();
                Console.WriteLine("Connected to pipe.");
                Console.WriteLine("There are currently {0} pipe server instances open.",
                    client.NumberOfServerInstances);

                while (client.CanWrite)
                {
                    var result = sr.Recognize();
                    var bytes = Encoding.UTF8.GetBytes(result.Text);
                    client.Write(bytes, 0, bytes.Length);
                }
            }
        }
    }
}