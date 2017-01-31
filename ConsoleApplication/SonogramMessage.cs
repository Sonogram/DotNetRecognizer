using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace ConsoleApplication
{
    [JsonObject(
//        ItemConverterType = typeof(StringEnumConverter),
//        ItemConverterParameters= new object[]{true},
        NamingStrategyType = typeof(CamelCaseNamingStrategy)
    )]
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
        public RecognitionMessage() : base("recognize") {}

        public string Message;
    }

    public class IdentificationMessage : SonogramMessage
    {
        public IdentificationMessage() : base("identify") {}

        public SonogramRole Role;
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