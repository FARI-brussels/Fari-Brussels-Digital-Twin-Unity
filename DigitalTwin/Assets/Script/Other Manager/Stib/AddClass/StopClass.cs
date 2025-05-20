using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

public enum StopType
{
    Metro,
    Bus,
    Tram,
}

public class StopClass
{
    public StopType Type { get; set; } 
    public string LineId { get; set; }
    public Color Color { get; set; }
    public string Name { get; set; }

    private static readonly Dictionary<StopType, Color> TypeColors = new()
    {
        { StopType.Metro, Color.green },
        { StopType.Bus, Color.blue },
        { StopType.Tram, Color.red },
    };

    public StopClass(string _line_id, string _stop_name, int _stop_id)
    {
        LineId = _line_id;
        Type = ClassifyVehicle(_line_id);
        Color = TypeColors[Type];
        Name = $"Stop " + _line_id + ": " + _stop_name + " " + _stop_id;
    }

    private StopType ClassifyVehicle(string line_id)
    {
        if (line_id == "1" || line_id == "2" || line_id == "5" || line_id == "6")
            return StopType.Metro;
        else if (line_id == "4" || line_id == "7" || line_id == "8" || line_id == "9" || line_id == "10" || line_id == "18" || line_id == "19" || line_id == "25" || line_id == "35" || line_id == "39" || line_id == "44" || line_id == "51" || line_id == "55" || line_id == "62" || line_id == "81" || line_id == "82" || line_id == "92" || line_id == "93" || line_id == "97")
            return StopType.Tram;
        else
            return StopType.Bus;
    }
}
