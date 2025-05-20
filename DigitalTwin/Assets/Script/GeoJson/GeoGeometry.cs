
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

//Interface for different Geometry
public interface IGeoGeometry
{
    string type { get; }
}

// Interface spécifique pour chaque type de géométrie
public interface IPointGeometry : IGeoGeometry
{
    List<double> coordinates { get; }
}

public interface ILineStringGeometry : IGeoGeometry
{
    List<double[]> coordinates { get; }
}

public interface IMultiLineStringGeometry : IGeoGeometry
{
    List<List<double[]>> coordinates { get; }
}

// Implémentations concrètes
[Serializable]
public class GeoGeometryPoint : IPointGeometry
{
    [JsonProperty("type")]
    public string type { get; set; } // "Point"

    [JsonProperty("coordinates")]
    public List<double> coordinates { get; set; }
}

[Serializable]
public class GeoGeometryLineString : ILineStringGeometry
{
    [JsonProperty("type")]
    public string type { get; set; } // "LineString"

    [JsonProperty("coordinates")]
    public List<double[]> coordinates { get; set; }
}

[Serializable]
public class GeoGeometryMultiLineString : IMultiLineStringGeometry
{
    [JsonProperty("type")]
    public string type { get; set; } // "MultiLineString"

    [JsonProperty("coordinates")]
    public List<List<double[]>> coordinates { get; set; }
} 


//Converter for polymorphic class
public class GeoGeometryConverter : JsonConverter
{
    //Can convert to a geojson class
    public override bool CanConvert(Type objectType)
    {
        return typeof(IGeoGeometry).IsAssignableFrom(objectType);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        JObject jObject = JObject.Load(reader);

        if (jObject["type"] == null)
            return null;

        string geometryType = jObject["type"].Value<string>();

        IGeoGeometry geometry;
        switch (geometryType)
        {
            case "Point":
                geometry = new GeoGeometryPoint();
                break;
            case "LineString":
                geometry = new GeoGeometryLineString();
                break;
            case "MultiLineString":
                geometry = new GeoGeometryMultiLineString();
                break;
            default:
                throw new JsonSerializationException($"Unsupported geometry type: {geometryType}");
        }

        serializer.Populate(jObject.CreateReader(), geometry);
        return geometry;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new NotImplementedException("Serialization is not implemented.");
    }

    public override bool CanWrite => false;
}