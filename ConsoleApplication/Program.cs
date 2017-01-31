using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Runtime.InteropServices.ComTypes;
using System.Speech.Recognition;
using System.Text;
using System.Threading;
using System.Windows.Markup;
using ConsoleApplication.Properties;
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

        public static void Main(string[] args)
        {
            // Settings
            var port = Environment.GetEnvironmentVariable("SONOGRAM_PORT");

            // Connect to the socket
            var client = new ClientWebSocket();
            Console.Write("Attempting to connect to socket...");
            client.ConnectAsync(new Uri($"ws://localhost:{port}"), CancellationToken.None).Wait();

            // Send the identification message
            var idMsg = new IdentificationMessage {Role = SonogramRole.Recognizer};
            client.SendAsync(Utf8Serialize(idMsg), WebSocketMessageType.Text, true, CancellationToken.None);
            Console.Write("Connected!");

            // Setup speech recognition
            var sr = new SpeechRecognitionEngine();
            sr.SetInputToDefaultAudioDevice();
            sr.EndSilenceTimeout = sr.InitialSilenceTimeout = new TimeSpan(0, 0, 0, 0, 100);
            sr.SpeechRecognized += (sender, eventArgs) =>
            {
                Console.WriteLine($"\"{eventArgs.Result.Text}\" recognized");
                var bytes = encoding.GetBytes(eventArgs.Result.Text);
                client.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true,
                    CancellationToken.None);
            };

            // Wait for the grammar to be received
            client.SendAsync(
                Utf8Serialize(new RequestGrammarMessage {Lang = "default"}),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
            var response = readSocket(client);
            var responseMessage = encoding.GetString(response);
            var grammarResponse = JsonConvert.DeserializeObject<GrammarMessage>(responseMessage);

            // Load the grammar
            sr.LoadGrammar(new Grammar(new MemoryStream(encoding.GetBytes(grammarResponse.Grammar))));

            // Start recognizing
            sr.RecognizeAsync(RecognizeMode.Multiple);

            Console.ReadLine();
        }
    }
}