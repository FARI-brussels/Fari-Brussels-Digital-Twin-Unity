using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;


[Serializable]
public class JsonFeature<TProperties, TGeometry> where TGeometry : IGeoGeometry
{
    [JsonProperty("id")]
    public string id { get; set; } // "Feature"

    [JsonProperty("type")]
    public string type { get; set; } // "Feature"

    [JsonProperty("properties")]
    public TProperties properties { get; set; }

    [JsonProperty("geometry")]
    [JsonConverter(typeof(GeoGeometryConverter))]
    public TGeometry geometry { get; set; }
}
