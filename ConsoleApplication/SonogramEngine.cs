using System;
using System.IO;
using System.Net.WebSockets;
using System.Speech.Recognition;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace ConsoleApplication
{
    public class SonogramEngine
    {
        private Options options;
        private bool dictate = false;
        private ClientWebSocket client = new ClientWebSocket();
        private SpeechRecognitionEngine sr = new SpeechRecognitionEngine();
        private Grammar grammar;

        /// <summary>
        /// True if the user is dictating free text to directly write out. False if they are using a language grammar.
        /// </summary>
        private bool Dictate
        {
            get { return dictate; }
            set
            {
                dictate = value;

                // Change the grammar according to the value of dictate
                sr.UnloadAllGrammars();
                sr.LoadGrammar(value ? new DictationGrammar() : grammar);
            }
        }

        public SonogramEngine(Options opts)
        {
            options = opts;

            // Setup speech recognition
            sr.SetInputToDefaultAudioDevice();
            sr.EndSilenceTimeout = sr.InitialSilenceTimeout = new TimeSpan(0, 0, 0, 0, 100);
            sr.SpeechRecognized += (sender, eventArgs) => ProcessSpeech(eventArgs.Result);
        }

        /// <summary>
        /// Callback function, called whenever text is recognized.
        /// </summary>
        /// <param name="result"></param>
        private void ProcessSpeech(RecognitionResult result)
        {
            Console.WriteLine($"\"{result.Text}\" recognized");

            // Work out if we should toggle dictate mode
            if (Dictate && result.Text.Contains("q k"))
                Dictate = false;
            else if (!Dictate && (string) result.Semantics["action"].Value == "insert")
                Dictate = true;

            // Build the message object
            var msg = new RecognitionMessage {Semantics = result.Semantics};

            // Convert it to JSON
            var msgJson = JsonConvert.SerializeObject(
                msg,
                options.Debug ? Formatting.Indented : Formatting.None,
                new SemanticConverter()
            );

            // Send it
            Console.WriteLine($"Sending:\n{msgJson}\nto the Sonogram server");
            client.SendAsync(new ArraySegment<byte>(Encoding.GetBytes(msgJson)), WebSocketMessageType.Text, true,
                CancellationToken.None);
        }

        /// <summary>
        /// Start the recognition loop
        /// </summary>
        private void Recognize()
        {
            if (options.Read)
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

        /// <summary>
        /// Connect to the Sonogram websocket server
        /// </summary>
        public void ConnectToServer()
        {
            Console.Write("Attempting to connect to socket...");
            client.ConnectAsync(new Uri($"ws://localhost:{options.Port}"), CancellationToken.None).Wait();
            Console.WriteLine("success!");
        }

        /// <summary>
        /// Ask the server for the grammar, and then parse the grammar
        /// </summary>
        private void ObtainGrammar()
        {
            // Wait for the grammar to be received
            Console.Write("Attempting to receive grammar...");
            client.SendAsync(
                Utf8Serialize(new RequestGrammarMessage {Lang = options.Grammar}),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
            var grammarResponse = JsonConvert.DeserializeObject<GrammarMessage>(Encoding.GetString(ReadSocket(client)));

            // Load the grammar and store it in the class
            grammar = new Grammar(new MemoryStream(Encoding.GetBytes(grammarResponse.Grammar)));
            sr.LoadGrammar(grammar);
            Console.WriteLine($"successfully loaded \"{options.Grammar}\" grammar.");
        }

        /// <summary>
        /// Main entry point for the engine. Connects to the server then begins recognition.
        /// </summary>
        public void Init()
        {
            ConnectToServer();
            ObtainGrammar();
            Recognize();
        }

        private static readonly Encoding Encoding = Encoding.UTF8;

        private static ArraySegment<byte> Utf8Serialize(object input)
        {
            return new ArraySegment<byte>(Encoding.GetBytes(JsonConvert.SerializeObject(input)));
        }

        private static byte[] ReadSocket(WebSocket socket)
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
    }
}