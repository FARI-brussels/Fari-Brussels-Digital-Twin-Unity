using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

public enum VehicleType
{
    Metro,
    Bus,
    Tram,
}

public class VehicleClass
{
    public VehicleType Type { get; set; }
    public string LineId { get; set; }
    public Color Color { get; set; }
    public string Name { get; set; }

    private static readonly Dictionary<VehicleType, Color> TypeColors = new()
    {
        { VehicleType.Metro, Color.yellow},
        { VehicleType.Bus, Color.cyan },
        { VehicleType.Tram, Color.magenta },
    };

    public VehicleClass(string _line_id, int direction)
    {
        LineId = _line_id;
        Type = ClassifyVehicle(_line_id);
        Color = TypeColors[Type];
        Name = $"Vehicle : " + _line_id + "; direction " + direction;
    }

    private VehicleType ClassifyVehicle(string line_id)
    {
        if (line_id == "1" || line_id == "2" || line_id == "5" || line_id == "6")
            return VehicleType.Metro;
        else if (line_id == "4" || line_id == "7" || line_id == "8" || line_id == "9" || line_id == "10" || line_id == "18" || line_id == "19" || line_id == "25" || line_id == "35" || line_id == "39" || line_id == "44" || line_id == "51" || line_id == "55" || line_id == "62" || line_id == "81" || line_id == "82" || line_id == "92" || line_id == "93" || line_id == "97")
            return VehicleType.Tram;
        else
            return VehicleType.Bus;
    }
}
