using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DanmakuWidgetServer.Converters
{
    public class RawJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return "";
            }

            try
            {
                JToken token = JToken.Load(reader);
                string jsonText = token.ToString();

                if (jsonText == "null" || jsonText == "{}")
                {
                    return "";
                }

                return jsonText;
            }
            catch
            {
                return "";
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null || string.IsNullOrEmpty(value.ToString()))
            {
                writer.WriteNull();
                return;
            }

            try
            {
                JToken token = JToken.Parse(value.ToString());
                token.WriteTo(writer);
            }
            catch
            {
                writer.WriteValue(value);
            }
        }
    }
}
