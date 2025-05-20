using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

[Serializable]
public class StibGeoPropertiesStops
{
    public string route_short_name { get; set; }
    public string direction_id { get; set; }
    public int direction { get; set; }
    public int stop_id { get; set; }
    public string stop_name { get; set; }
    public int stop_sequence { get; set; }
    public double stop_lat { get; set; }
    public double stop_lon { get; set; }
}

[Serializable]
public class StibGeoPropertiesShapes
{
    public string line_id { get; set; }
    public int direction { get; set; }
    public int start { get; set; }
    public double distance { get; set; }
    public int end { get; set; }
    public string color { get; set; }
}

[Serializable]
public class StibGeoPropertiesVehiclePostion
{
    public int id { get; set; }
    public int pointId { get; set; }
    public string lineId { get; set; }
    public int direction { get; set; }
    public int distanceFromPoint { get; set; }
    public string color { get; set; }
    public double timestamp { get; set; }
    public string uuid { get; set; }
    public double distance { get; set; }
}