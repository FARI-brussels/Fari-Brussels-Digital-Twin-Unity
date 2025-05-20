using System.Collections.Generic;
using System.Xml.Serialization;

using System;
[System.Serializable]
public class VehicleRoute
{
    public string id;
    public float depart;
    public List<string> edges;
}


public class TripInfo
{
    public string id;
    public float depart;
    public string departLane;
    public float departPos;
    public float departSpeed;
    public float departDelay;
    public float arrival;
    public string arrivalLane;
    public float arrivalPos;
    public float arrivalSpeed;
    public float duration;
    public float routeLength;
    public float waitingTime;
    public int waitingCount;
    public float stopTime;
    public float timeLoss;
    public int rerouteNo;
}