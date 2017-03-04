using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace ConsoleApplication
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class SonogramMessage
    {
        public SonogramMessage(string action)
        {
            Action = action;
        }

        public string Action;
    }

    public class RecognitionMessage : SonogramMessage
    {
        public RecognitionMessage() : base("recognition") {}

        public object Semantics;
    }

    public class GrammarMessage : SonogramMessage
    {
        public GrammarMessage() : base("grammar") {}

        public string Grammar;
    }

    public class RequestGrammarMessage : SonogramMessage
    {
        public RequestGrammarMessage() : base("requestGrammar") {}

        public string Lang;
    }

    [JsonConverter(typeof(StringEnumConverter), true)]
    public enum SonogramRole
    {
        Recognizer,
        Editor
    }
}