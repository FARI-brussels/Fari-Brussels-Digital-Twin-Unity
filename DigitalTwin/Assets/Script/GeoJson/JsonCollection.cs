using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

[Serializable]
public class JsonCollection<TFeature>
{
    [JsonProperty("type")]
    public string type { get; set; } // "FeatureCollection"

    [JsonProperty("features")]
    public List<TFeature> features { get; set; }
}