using System;
using System.ComponentModel;
using System.IO;
using System.Net.WebSockets;
using System.Speech.Recognition;
using System.Linq;
using System.Text;
using System.Threading;
using CommandLine;
using Newtonsoft.Json;

namespace ConsoleApplication
{
    internal class Program
    {
        private static readonly Encoding encoding = Encoding.UTF8;

        private static ArraySegment<byte> Utf8Serialize(object input)
        {
            return new ArraySegment<byte>(encoding.GetBytes(JsonConvert.SerializeObject(input)));
        }

        private static byte[] readSocket(WebSocket socket)
        {
            var buffer = new byte[1000];
            var offset = 0;
            WebSocketReceiveResult result;
            do
            {
                // Update the section of the buffer we're using to recieve
                var segment = new ArraySegment<byte>(buffer, offset, buffer.Length - offset);

                // Receive the data
                result = socket.ReceiveAsync(segment, CancellationToken.None).Result;

                // Update where we're up to in the array
                offset += result.Count;

                // If it looks like we're about to run out of space, resize the array
                if (offset + result.Count > buffer.Length)
                    Array.Resize(ref buffer, buffer.Length * 2);
            } while (!result.EndOfMessage);

            return buffer;
        }

        private class Options
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

            // Connect to the socket
            var client = new ClientWebSocket();
            Console.Write("Attempting to connect to socket...");
            client.ConnectAsync(new Uri($"ws://localhost:{parsedArgs.Port}"), CancellationToken.None).Wait();
            Console.WriteLine("success!");

            // Setup speech recognition
            var sr = new SpeechRecognitionEngine();
            sr.SetInputToDefaultAudioDevice();
            sr.EndSilenceTimeout = sr.InitialSilenceTimeout = new TimeSpan(0, 0, 0, 0, 100);
            sr.SpeechRecognized += (sender, eventArgs) =>
            {
                Console.WriteLine($"\"{eventArgs.Result.Text}\" recognized");

                // Build the message object
                var msg = new RecognitionMessage {Semantics = eventArgs.Result.Semantics};

                // Convert it to JSON
                var msgJson = JsonConvert.SerializeObject(
                    msg,
                    parsedArgs.Debug ? Formatting.Indented : Formatting.None,
                    new SemanticConverter()
                );

                // Send it
                Console.WriteLine($"Sending:\n{msg}\nto the Sonogram server");
                client.SendAsync(new ArraySegment<byte>(encoding.GetBytes(msgJson)), WebSocketMessageType.Text, true,
                    CancellationToken.None);
            };

            // Wait for the grammar to be received
            Console.Write("Attempting to receive grammar...");
            client.SendAsync(
                Utf8Serialize(new RequestGrammarMessage {Lang = parsedArgs.Grammar}),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
            var response = readSocket(client);
            var responseMessage = encoding.GetString(response);
            var grammarResponse = JsonConvert.DeserializeObject<GrammarMessage>(responseMessage);

            // Load the grammar
            sr.LoadGrammar(new Grammar(new MemoryStream(encoding.GetBytes(grammarResponse.Grammar))));
            Console.WriteLine($"successfully loaded \"{parsedArgs.Grammar}\" grammar.");

            // Start recognizing
            if (parsedArgs.Read)
            {
                Console.WriteLine("Now reading from stdin.");
                while (true)
                {
                    var input = Console.ReadLine();
                    sr.EmulateRecognizeAsync(input);
                }
            }
            else
            {
                Console.WriteLine("Now listening.");
                sr.RecognizeAsync(RecognizeMode.Multiple);
                while (true)
                    Console.ReadLine();
            }
        }
    }
}