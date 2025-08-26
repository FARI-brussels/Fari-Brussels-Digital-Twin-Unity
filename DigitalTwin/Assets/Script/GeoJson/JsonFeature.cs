using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/*
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
}*/

[Serializable]
public class JsonFeature<TGeometry> where TGeometry : IGeoGeometry
{
    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public string id { get; set; } // "Feature"

    [JsonProperty("type")]
    public string type { get; set; } // "Feature"

    [JsonProperty("properties")]
    public JObject properties { get; set; }

    [JsonProperty("geometry")]
    [JsonConverter(typeof(GeoGeometryConverter))]
    public TGeometry geometry { get; set; }
}
