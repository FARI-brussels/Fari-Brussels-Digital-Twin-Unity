using System;


[Serializable]
public class EdgesGeoJsonProperties
{
    public string index { get; set; }
    public string element { get; set; }
    public string id { get; set; }
    public string name { get; set; }
    public double sampledSeconds { get; set; }
    public double departed { get; set; }
    public double arrived { get; set; }
    public double entered { get; set; }
    public double left { get; set; }
    public double laneChangedFrom { get; set; }
    public double laneChangedTo { get; set; }
    public double traveltime { get; set; }
    public double overlapTraveltime { get; set; }
    public float density { get; set; }
    public double laneDensity { get; set; }
    public double occupancy { get; set; }
    public double waitingTime { get; set; }
    public double timeLoss { get; set; }
    public float speed { get; set; }
    public double speedRelative { get; set; }
    public double teleported { get; set; }
}

