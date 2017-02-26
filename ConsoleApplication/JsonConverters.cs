using System;
using System.Speech;
using System.Speech.Recognition;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ConsoleApplication
{
    public class SemanticConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var val = (SemanticValue) value;

            if (val.Count == 0)
                writer.WriteValue(val.Value);
            else
            {
                writer.WriteStartObject();
                foreach (var kvp in val)
                {
                    writer.WritePropertyName(kvp.Key);
                    serializer.Serialize(writer, kvp.Value);
                }
                writer.WriteEndObject();
            }
//
//            var dict = new JObject();
//            foreach (var kvp in val)
//            {
//                dict.Add(kvp.Key, serializer.);
//            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead => false;

        public override bool CanConvert(Type objectType)
        {
            if (objectType == typeof(SemanticValue))
                return true;

            return false;
        }
    }
}