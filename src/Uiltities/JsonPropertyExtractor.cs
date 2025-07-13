using System.Text.Json;

namespace NearbyCS_API.Utlls
{
    /// <summary>
    /// Provides resilient JSON property extraction methods with smart defaults.
    /// Designed for demo scenarios where graceful degradation is preferred over exceptions.
    /// </summary>
    public static class JsonPropertyExtractor
    {
        /// <summary>
        /// Extracts a string property from JsonElement with fallback to default value.
        /// Handles missing properties and type mismatches gracefully.
        /// </summary>
        /// <param name="element">The JsonElement to extract from</param>
        /// <param name="propertyName">The name of the property to extract</param>
        /// <param name="defaultValue">Default value if property is missing or invalid</param>
        /// <returns>The extracted string value or default value</returns>
        public static string ExtractStringProperty(JsonElement element, string propertyName, string defaultValue = "")
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                return prop.ValueKind == JsonValueKind.String ? prop.GetString() ?? defaultValue : 
                       prop.ValueKind == JsonValueKind.Number ? prop.ToString() :
                       prop.ValueKind == JsonValueKind.True ? "true" :
                       prop.ValueKind == JsonValueKind.False ? "false" :
                       defaultValue;
            }
            return defaultValue;
        }

        /// <summary>
        /// Extracts an integer property from JsonElement with fallback to default value.
        /// Handles string-to-int conversion and missing properties gracefully.
        /// </summary>
        /// <param name="element">The JsonElement to extract from</param>
        /// <param name="propertyName">The name of the property to extract</param>
        /// <param name="defaultValue">Default value if property is missing or invalid</param>
        /// <returns>The extracted integer value or default value</returns>
        public static int ExtractIntProperty(JsonElement element, string propertyName, int defaultValue = 0)
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                return prop.ValueKind == JsonValueKind.Number ? prop.GetInt32() : 
                       prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out var parsed) ? parsed : 
                       defaultValue;
            }
            return defaultValue;
        }

        /// <summary>
        /// Extracts a decimal property from JsonElement with fallback to default value.
        /// Handles string-to-decimal conversion and missing properties gracefully.
        /// </summary>
        /// <param name="element">The JsonElement to extract from</param>
        /// <param name="propertyName">The name of the property to extract</param>
        /// <param name="defaultValue">Default value if property is missing or invalid</param>
        /// <returns>The extracted decimal value or default value</returns>
        public static decimal ExtractDecimalProperty(JsonElement element, string propertyName, decimal defaultValue = 0m)
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                return prop.ValueKind == JsonValueKind.Number ? prop.GetDecimal() : 
                       prop.ValueKind == JsonValueKind.String && decimal.TryParse(prop.GetString(), out var parsed) ? parsed : 
                       defaultValue;
            }
            return defaultValue;
        }

        /// <summary>
        /// Extracts a boolean property from JsonElement with fallback to default value.
        /// Handles string-to-bool conversion ("true"/"false") and missing properties gracefully.
        /// </summary>
        /// <param name="element">The JsonElement to extract from</param>
        /// <param name="propertyName">The name of the property to extract</param>
        /// <param name="defaultValue">Default value if property is missing or invalid</param>
        /// <returns>The extracted boolean value or default value</returns>
        public static bool ExtractBoolProperty(JsonElement element, string propertyName, bool defaultValue = false)
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                return prop.ValueKind == JsonValueKind.True ? true :
                       prop.ValueKind == JsonValueKind.False ? false :
                       prop.ValueKind == JsonValueKind.String && bool.TryParse(prop.GetString(), out var parsed) ? parsed :
                       defaultValue;
            }
            return defaultValue;
        }

        /// <summary>
        /// Extracts a double property from JsonElement with fallback to default value.
        /// Handles string-to-double conversion and missing properties gracefully.
        /// </summary>
        /// <param name="element">The JsonElement to extract from</param>
        /// <param name="propertyName">The name of the property to extract</param>
        /// <param name="defaultValue">Default value if property is missing or invalid</param>
        /// <returns>The extracted double value or default value</returns>
        public static double ExtractDoubleProperty(JsonElement element, string propertyName, double defaultValue = 0.0)
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                return prop.ValueKind == JsonValueKind.Number ? prop.GetDouble() : 
                       prop.ValueKind == JsonValueKind.String && double.TryParse(prop.GetString(), out var parsed) ? parsed : 
                       defaultValue;
            }
            return defaultValue;
        }

        /// <summary>
        /// Safely parses a JSON string into JsonElement with error handling.
        /// Returns null if parsing fails.
        /// </summary>
        /// <param name="jsonString">The JSON string to parse</param>
        /// <returns>JsonElement if successful, null if parsing fails</returns>
        public static JsonElement? SafeParseJson(string jsonString)
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonString);
                return doc.RootElement.Clone();
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}