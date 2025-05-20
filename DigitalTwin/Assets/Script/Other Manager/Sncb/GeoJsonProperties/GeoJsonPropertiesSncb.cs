using UnityEngine;
using System;
using System.Collections.Generic;


[Serializable]
public class GeoPoint2D
{
    public double lon { get; set; }
    public double lat { get; set; }
}

public class SncbGeoPropertiesShapes
{
    public GeoPoint2D geo_point_2d { get; set; } // Pour "geo_point_2d": { "lon": ..., "lat": ... }
    public string stationfrom_id { get; set; } // Pour "stationfrom_id": "841"
    public string stationfrom_name { get; set; } // Pour "stationfrom_name": "MOLLEM"
    public string stationto_id { get; set; } // Pour "stationto_id": "821"
    public string stationto_name { get; set; } // Pour "stationto_name": "MERCHTEM"
    public double? length { get; set; } // Pour "length": 2.45542483750497
}


[Serializable]
public class SncbGeoPropertiesStops
{
    public GeoPoint2D geo_point_2d { get; set; }
    public string ptcarid { get; set; }
    public string taftapcode { get; set; }
    public string symbolicname { get; set; }
    public string shortnamefrench { get; set; }
    public string shortnamedutch { get; set; }
    public string longnamefrench { get; set; }
    public string longnamedutch { get; set; }
    public string commercialshortnamefrench { get; set; }
    public string commercialshortnamedutch { get; set; }
    public string commercialmiddlenamefrench { get; set; }
    public string commercialmiddlenamedutch { get; set; }
    public string commerciallongnamefrench { get; set; }
    public string commerciallongnamedutch { get; set; }
    public string classification { get; set; }
    public string class_en { get; set; }
    public string class_fr { get; set; }
}

[Serializable]
public class SncbGeoPropertiesVehiclePosition 
{
    public string trip_id { get; set; }
    public string trip_headsign { get; set; }
    public string name_start { get; set; }
    public string name_end { get; set; }
    public string ptcarid_start { get; set; }
    public string ptcarid_end { get; set; }
}
