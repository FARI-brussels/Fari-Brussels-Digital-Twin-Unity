using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class TripInfoDL
{
    public string route_id;
    public string service_id;
    public string trip_headsign;
    public string trip_short_name;
    public string direction_id;
    public string block_id;
    public string shape_id;
}

// Structure pour gérer la clé unique dans le JSON
public class TripWrapper
{
    public TripInfoDL trip_info;
}
