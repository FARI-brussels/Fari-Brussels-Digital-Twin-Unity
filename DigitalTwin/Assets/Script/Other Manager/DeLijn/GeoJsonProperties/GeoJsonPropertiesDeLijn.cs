using System;
using Newtonsoft.Json;

[Serializable]
public class DeLijnShapeProperties
{
    [JsonProperty("shape_id")]
    public string shape_id { get; set; }

    [JsonProperty("point_count")]
    public int point_count { get; set; }

    [JsonProperty("total_distance")]
    public double total_distance { get; set; }
}

[Serializable]
public class DeLijnStopProperties
{
    [JsonProperty("stop_id")]
    public string stop_id { get; set; }

    [JsonProperty("stop_code")]
    public string stop_code { get; set; }

    [JsonProperty("stop_name")]
    public string stop_name { get; set; }

    [JsonProperty("stop_url")]
    public string stop_url { get; set; }

    [JsonProperty("wheelchair_accessible")]
    public bool wheelchair_accessible { get; set; }
}

[Serializable]
public class VehicleProperties
{
    [JsonProperty("vehicle_id")]
    public string vehicle_id { get; set; }

    [JsonProperty("trip_id")]
    public string trip_id { get; set; }

    [JsonProperty("wheelchair_accessible")]
    public bool wheelchair_accessible { get; set; }
}