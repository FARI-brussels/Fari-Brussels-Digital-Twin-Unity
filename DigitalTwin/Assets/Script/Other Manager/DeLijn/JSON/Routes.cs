using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class RouteInfo
{
    public string agency_id;
    public string route_short_name;
    public string route_long_name;
    public string route_desc;
    public string route_type;
    public string route_url;
    public string route_color;
    public string route_text_color;
}

// Structure pour gérer la clé unique dans le JSON
public class RouteWrapper
{
    public RouteInfo route_info;
}
