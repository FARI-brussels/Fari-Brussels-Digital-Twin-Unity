using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using TransitRealtime;
using Newtonsoft.Json;
using Google.Protobuf;
using CesiumForUnity;
using System.Threading.Tasks;

public class DeLijnManager : MonoBehaviour
{
    /*
    [SerializeField] private string apiUrlGTFS = "https://api.mobilitytwin.brussels/de-lijn/gtfs";
    [SerializeField] private string apiUrlGTFS_RT = "https://api.delijn.be/gtfs/v3/realtime";
    [SerializeField] private string specialDLApiKey = "1c0e66a542a540328da529b63bbbd387";
    [SerializeField] private string apiKey = "c6c3b6c061aee8a9e063489669f1222049c8c3901120dd356ffd0cc91c486922892b0801914af0d98297c87da86da70f58407e9d3c08dbdeb70f4e56eb038e96";
    private ApiClient apiClient;
    private ApiClient apiClientDL;*/

    public GameObject busPrefab;
    public GameObject stopPrefab;
    //Realtime vehicule
    [SerializeField] private string apiUrlGTFS_RT = "https://api.delijn.be/gtfs/v3/realtime";
    [SerializeField] private string apiUrlInternRoutes = "http://localhost:5000/api-test/routes.json";
    [SerializeField] private string apiUrlInternTrips = "http://localhost:5000/api-test/trips.json";
    [SerializeField] private string apiUrlInternStops = "http://localhost:5000/api-test/stops.json";
    [SerializeField] private string apiUrlInternShapes = "http://localhost:5000/api-test/shapes.json";
    [SerializeField] private string specialDLApiKey = "1c0e66a542a540328da529b63bbbd387";
    [SerializeField] private float refreshInterval = 5f;
    private ApiClient apiClientDL;
    private ApiClient localApiClient;

    //Location of the stops and shapes
    [Header("Shapes and Stops file Location")]
    [SerializeField] private string stopsPath = "Assets/Data/GeoJSON/DeLijn/stops.json";
    [SerializeField] private string shapesPath = "Assets/Data/GeoJSON/DeLijn/shapes.json";
    [SerializeField] private string tripsPath = "Assets/Data/GeoJSON/DeLijn/trips.json";
    [SerializeField] private string routesPath = "Assets/Data/GeoJSON/DeLijn/routes.json";
      
    //Default Material for line renderer
    public Material defaultMaterialForLine;

    private string gtfsFilePath;
    private GTFSParserDeLijn gtfsParser;

    private JsonCollection<JsonFeature<DeLijnStopProperties, GeoGeometryPoint>> geoJsonStops;
    private JsonCollection<JsonFeature<DeLijnShapeProperties, GeoGeometryLineString>> geoJsonShapes;

    public Dictionary<string, (GameObject, List<double>)> vehicles = new Dictionary<string, (GameObject, List<double>)>();
    Dictionary<string, TripWrapper> tripsData = new Dictionary<string, TripWrapper>();
    Dictionary<string, RouteInfo> routesData = new Dictionary<string, RouteInfo>();
    private string stopsInfo;
    private string shapesInfo;

    //Parent to store segments, stops and vehicles
    private GameObject deLijnParent;
    private GameObject linesParent;
    private GameObject stopsParent;
    private GameObject vehiclesParent;

    [Header("Cesium")]
    public CesiumGeoreference georeference;
    private GeoJsonCesiumSpawner geoJsonCesiumSpawner = new GeoJsonCesiumSpawner();
    public Cesium3DTileset tileset;

    public void InitDeLijn()
    {
        InitSceneGameObject();
        geoJsonCesiumSpawner.Initialize(georeference);
        apiClientDL = new ApiClient(specialDLApiKey);
        localApiClient = new ApiClient();
        StartCoroutine(InitDeLijnNetwork());
    }
    private IEnumerator InitDeLijnNetwork()
    {
        bool stopsCompleted = false;
        bool shapesCompleted = false;
        bool routesCompleted = false;
        bool tripsCompleted = false;

        StartCoroutine(localApiClient.GetApiData<JsonCollection<JsonFeature<DeLijnStopProperties, GeoGeometryPoint>>>(
            apiUrlInternStops, (data) => {
                geoJsonStops = data;
                if (geoJsonStops != null)
                {
                    foreach (var feature in geoJsonStops.features)
                    {
                        if (feature.geometry.coordinates[1] >= 50.693 && feature.geometry.coordinates[1] <= 51.013 && feature.geometry.coordinates[0] >= 4.212 && feature.geometry.coordinates[0] <= 4.569)
                        {
                            Vector3 positionUcsStop = geoJsonCesiumSpawner.GetCesiumPosition(feature.geometry.coordinates[0], feature.geometry.coordinates[1]);
                            CreateMarkeurStop(positionUcsStop, feature);
                        }
                    }
                }
                stopsCompleted = true;
            }
        ));


        StartCoroutine(localApiClient.GetApiData<JsonCollection<JsonFeature<DeLijnShapeProperties, GeoGeometryLineString>>>(
            apiUrlInternShapes, (data) => {
                geoJsonShapes = data;
                shapesCompleted = true;
            }
        ));

        StartCoroutine(localApiClient.GetApiData<Dictionary<string, RouteInfo>>(
            apiUrlInternRoutes, (data) => {
                routesData = data;
                routesCompleted = true;
            }
        ));
        StartCoroutine(localApiClient.GetApiData<Dictionary<string, TripWrapper>>(
             apiUrlInternTrips, (data) => {
                 tripsData = data;
                 tripsCompleted = true;
             }
         ));

        yield return new WaitUntil(() => stopsCompleted && shapesCompleted && routesCompleted && tripsCompleted);

        if (geoJsonStops != null && geoJsonShapes != null && tripsData != null && routesData != null)
        {
            StartCoroutine(FetchDataRoutine());
        }
        else
        {
            Debug.LogError("Données manquantes. Routine non lancée.");
        }

        /*geoJsonStops = await LoadGeoJsonDataAsync<JsonFeature<DeLijnStopProperties, GeoGeometryPoint>>(stopsPath);
       if (geoJsonStops != null)
       {
           foreach (var feature in geoJsonStops.features)
           {
               if (feature.geometry.coordinates[1] >= 50.693 && feature.geometry.coordinates[1] <= 51.013 && feature.geometry.coordinates[0] >= 4.212 && feature.geometry.coordinates[0] <= 4.569)
               {
                   Vector3 positionUcsStop = geoJsonCesiumSpawner.GetCesiumPosition(feature.geometry.coordinates[0], feature.geometry.coordinates[1]);
                   CreateMarkeurStop(positionUcsStop, feature);
               }
           }
       }*/

        /*geoJsonShapes = await LoadGeoJsonDataAsync<JsonFeature<DeLijnShapeProperties, GeoGeometryLineString>>(shapesPath);

        /*
        float startTime = Time.realtimeSinceStartup;
        if (geoJsonShapes != null)
        {

            foreach (var feature in geoJsonShapes.features)
            {
                DrawLine(feature);
            }
        }
        float duration = Time.realtimeSinceStartup - startTime;
        Debug.Log("Durée Stop : " + duration + " secondes");
        */

        /*tripsData = await LoadTripsJsonAsync(tripsPath);
        routesData = await LoadRoutesJsonAsync(routesPath);*/

    }


    private void InitSceneGameObject()
    {
        deLijnParent = new GameObject { name = "DeLijn Parent" };
        linesParent = new GameObject { name = "Lines" };
        linesParent.transform.parent = deLijnParent.transform;
        stopsParent = new GameObject { name = "Stops" };
        stopsParent.transform.parent = deLijnParent.transform;
        vehiclesParent = new GameObject { name = "Vehicles" };
        vehiclesParent.transform.parent = deLijnParent.transform;
    }

    public IEnumerator FetchDataRoutine()
    {
        while (true)
        {
            // Call API
            yield return StartCoroutine(LoadGTFS_RTData());

            // Wait x sec
            yield return new WaitForSeconds(refreshInterval);
        }

    }
    private IEnumerator LoadGTFS_RTData()
    {
        bool isFinished = false;
        var features = new List<JsonFeature<VehicleProperties, GeoGeometryPoint>>();

        yield return apiClientDL.GetApiData(apiUrlGTFS_RT, (FeedMessage feed) =>
        {
            if (feed != null)
            {
                foreach (var entity in feed.Entity)
                {

                    if (entity.Vehicle != null)
                    {
                        //Debug.Log(JsonConvert.SerializeObject(entity, Formatting.Indented));
                    }
                    if (entity.Vehicle?.Vehicle != null && entity.Vehicle?.Position != null)
                    {

                        var properties = new VehicleProperties
                        {
                            vehicle_id = entity.Vehicle.Vehicle.Id,
                            wheelchair_accessible = entity.Vehicle.Vehicle.HasWheelchairAccessible,
                            trip_id = entity.Vehicle.Trip.TripId
                        };

                        var geometry = new GeoGeometryPoint
                        {
                            type = "Point",
                            coordinates = new List<double>
                        {
                            entity.Vehicle.Position.Longitude,
                            entity.Vehicle.Position.Latitude


                        }
                        };

                        var feature = new JsonFeature<VehicleProperties, GeoGeometryPoint>
                        {
                            id = entity.Vehicle.Vehicle.Id,
                            type = "Feature",
                            properties = properties,
                            geometry = geometry
                        };

                        features.Add(feature);
                    }

                }

                var vehiclePositionGeoJson = new JsonCollection<JsonFeature<VehicleProperties, GeoGeometryPoint>>
                {
                    type = "FeatureCollection",
                    features = features
                };

                

                foreach (var feature in vehiclePositionGeoJson.features)
                {
                    if (feature.geometry.coordinates[1] >= 50.693 && feature.geometry.coordinates[1] <= 51.013 && feature.geometry.coordinates[0] >= 4.212 && feature.geometry.coordinates[0] <= 4.569)
                    {

                        TripInfoDL tripInfo = GetTripInfoById(feature.properties.trip_id, tripsData);

                        if (tripInfo != null)
                        {
                            JsonFeature<DeLijnShapeProperties, GeoGeometryLineString> relevantShape = geoJsonShapes.features.Find(
                               s => s.properties.shape_id == tripInfo.shape_id
                            );
                            if (relevantShape != null)
                            {

                                Vector3 newPositionUcsStop = geoJsonCesiumSpawner.GetCesiumPosition(feature.geometry.coordinates[0], feature.geometry.coordinates[1]);


                                Vector3 targetDirection = NextPoint(relevantShape, newPositionUcsStop);

                                if (vehicles.ContainsKey(feature.properties.vehicle_id))
                                {
                                    GameObject vehicle = vehicles[feature.properties.vehicle_id].Item1;
                                    Vector3 initPosition = vehicles[feature.properties.vehicle_id].Item1.transform.position;

                                    List<Vector3> path = FindPath(relevantShape, initPosition, newPositionUcsStop);
                                    

                                    DelijnVehicleMover mover = vehicle.GetComponent<DelijnVehicleMover>();
                                    if (mover != null && path.Count > 0)
                                    {
                                        //The refresh time = travel time
                                        float travelTime = 20f;
                                        mover.SetPath(path, travelTime);
                                    }
                                    vehicles[feature.properties.vehicle_id] = (vehicle, feature.geometry.coordinates);
                                }
                                else
                                {
                                    // Create a marker for the stop in Unity
                                    GameObject newVehicle = CreateMarkeurVecPos(newPositionUcsStop, feature.properties.vehicle_id,tripInfo, targetDirection);
                                    vehicles.Add($"{feature.properties.vehicle_id}",
                                        (newVehicle, new List<double>
                                            { feature.geometry.coordinates[0], feature.geometry.coordinates[1] }
                                        )
                                    );
                                }

                            }
                        }

                        
                    }
                }
            }
            else
            {
                Debug.LogError("Erreur lors de l'extraction des fichiers GTFS.");
            }

            isFinished = true;
        }, isGTFS_RT: true);

        yield return new WaitUntil(() => isFinished);
    }

    public TripInfoDL GetTripInfoById(string tripId, Dictionary<string, TripWrapper> tripsData)
    {
        if (tripsData.TryGetValue(tripId, out TripWrapper tripWrapper))
        {
            return tripWrapper.trip_info;
        }
        else
        {
            return null;
        }
    }
    public RouteInfo GetRouteInfoById(string routeId, Dictionary<string, RouteInfo> routesData)
    {
       
        if (routesData.TryGetValue(routeId, out RouteInfo routeInfo))
        {

            return routeInfo;
        }
        else
        {
            return null;
        }
    }

    private GameObject CreateMarkeurVecPos(Vector3 position, string vehicle_id,TripInfoDL tripInfo, Vector3 targetDirection )//,string stop_name, int stop_id, string route_short_name, JsonFeature<StibGeoPropertiesStops, GeoGeometryPoint> feature)
    {

        //Instantiate Stib Stop
        GameObject vehicleGameObject = Instantiate(busPrefab);
        DelijnVehicleMover mover = vehicleGameObject.AddComponent<DelijnVehicleMover>();
 

        //Visualization
        vehicleGameObject.name = vehicle_id;

        //Position
        vehicleGameObject.transform.position = position;

        //Size
        vehicleGameObject.transform.localScale = new Vector3(2f, 2f, 2f);

        //Parent
        vehicleGameObject.transform.parent = vehiclesParent.transform;

        Vector3 lookAtTarget = targetDirection;
        lookAtTarget.y = vehicleGameObject.transform.position.y; // On garde la hauteur actuelle

        vehicleGameObject.transform.LookAt(lookAtTarget);

        ShowInfoOnHoverDL hoverScript = vehicleGameObject.AddComponent<ShowInfoOnHoverDL>();
        hoverScript.Initialize();

        RouteInfo routeInfo = GetRouteInfoById(tripInfo.route_id,routesData);
         
        ShowInfoOnClickDL clickScript = vehicleGameObject.AddComponent<ShowInfoOnClickDL>();
        clickScript.Initialize(vehicleId: vehicle_id, tripHeadSign: tripInfo.trip_headsign, tripShortName: tripInfo.trip_short_name, direction: tripInfo.direction_id, nameRoute: routeInfo.route_long_name, urlRoute: routeInfo.route_url);
      
       

        return vehicleGameObject;
    }

    private async Task<Dictionary<string, TripWrapper>> LoadTripsJsonAsync(string jsonFilePath)
    {
        TextAsset jsonContentAsset = Resources.Load<TextAsset>(jsonFilePath);

        if (jsonContentAsset == null)
        {
            Debug.LogError($"Fichier GeoJSON introuvable dans Resources: {jsonFilePath}");
            return null;
        }

        string jsonContent = jsonContentAsset.text;
        return await Task.Run(() =>
        {
            return JsonConvert.DeserializeObject<Dictionary<string, TripWrapper>>(jsonContent);
        });
    }
    public async Task<Dictionary<string, RouteInfo>> LoadRoutesJsonAsync(string jsonFilePath)
    {
        TextAsset jsonContentAsset = Resources.Load<TextAsset>(jsonFilePath);
        if (jsonContentAsset == null)
        {
            Debug.LogError($"Fichier JSON introuvable dans Resources: {jsonFilePath}");
            return null;
        }

        string jsonContent = jsonContentAsset.text;

        return await Task.Run(() =>
        {
            return JsonConvert.DeserializeObject<Dictionary<string, RouteInfo>>(jsonContent);
        });
    }

    public async Task<JsonCollection<TFeature>> LoadGeoJsonDataAsync<TFeature>(string geoJsonFilePath)
    {
        TextAsset jsonContentAsset = Resources.Load<TextAsset>(geoJsonFilePath);
        if (jsonContentAsset == null)
        {
            Debug.LogError($"Fichier GeoJSON introuvable dans Resources: {geoJsonFilePath}");
            return null;
        }

        string jsonContent = jsonContentAsset.text;

        return await Task.Run(() =>
        {
            return JsonConvert.DeserializeObject<JsonCollection<TFeature>>(jsonContent);
        });
    }

    private void CreateMarkeurStop(Vector3 position, JsonFeature<DeLijnStopProperties, GeoGeometryPoint> feature)
    {

        GameObject vehicleGameObject = Instantiate(stopPrefab);
        vehicleGameObject.name = feature.properties.stop_id;
        Renderer sphereRenderer = vehicleGameObject.GetComponent<Renderer>();
        sphereRenderer.material.color = Color.yellow;

        //Position
        vehicleGameObject.transform.position = position;

        //Size
        vehicleGameObject.transform.localScale = new Vector3(500f, 500f, 500f);

        //Parent
        vehicleGameObject.transform.parent = stopsParent.transform;

        ShowStopInfoOnHoverDL hoverScript = vehicleGameObject.AddComponent<ShowStopInfoOnHoverDL>();
        hoverScript.Initialize(feature.properties.stop_name, feature.properties.stop_id);
        ShowInfoOnClickDL clickScript = vehicleGameObject.AddComponent<ShowInfoOnClickDL>();
        clickScript.Initialize(isStop: true,stopId: feature.properties.stop_id, stopName:feature.properties.stop_name, stopUrl:feature.properties.stop_url,wheelChairAccessible:feature.properties.wheelchair_accessible,coordinates: feature.geometry.coordinates);
    }

    //Function to drawn the stib network
    private void DrawLine(JsonFeature<DeLijnShapeProperties, GeoGeometryLineString> feature)
    {
        //Instantiate Stib Stop
        GameObject lineObject = new GameObject("Line_" + feature.properties.shape_id);
        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();

        //Visualization
        lineRenderer.positionCount = feature.geometry.coordinates.Count;
        lineRenderer.startWidth = 3f;
        lineRenderer.endWidth = 3f;
        lineRenderer.material = defaultMaterialForLine;
        Color lineColor = Color.white;
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;

        List<Vector3> validPositions = new List<Vector3>();
        foreach (var coord in feature.geometry.coordinates)
        {
            double latitude = coord[1];
            double longitude = coord[0];

            // Vérifie si le point est dans les limites de Bruxelles
            if (latitude >= 50.693 && latitude <= 51.013 && longitude >= 4.212 && longitude <= 4.569)
            {
                Vector3 position = geoJsonCesiumSpawner.GetCesiumPosition(longitude, latitude);
                validPositions.Add(position);
            }
        }

        // Vérifie si on a des points valides à dessiner
        if (validPositions.Count > 1)
        {
            lineRenderer.positionCount = validPositions.Count;
            lineRenderer.SetPositions(validPositions.ToArray());
            lineRenderer.transform.parent = linesParent.transform;
        }
        else
        {
            // Supprime l'objet si aucune position valide
            Destroy(lineObject);
        }

    }

    public List<Vector3> FindPath(JsonFeature<DeLijnShapeProperties, GeoGeometryLineString> relevantShape, Vector3 startPoint, Vector3 endPoint)
    {
        List<Vector3> path = new List<Vector3>();
        Dictionary<string, List<Vector3>> routesDictionary = new Dictionary<string, List<Vector3>>();

        string key = relevantShape.properties.shape_id;
        routesDictionary[key] = new List<Vector3>();
        foreach (var coord in relevantShape.geometry.coordinates)
        {
            Vector3 position = geoJsonCesiumSpawner.GetCesiumPosition(coord[0], coord[1]);
            routesDictionary[key].Add(position);

        }

        int indexStart = FindClosestPointIndex(routesDictionary[key], startPoint);
        int indexEnd = FindClosestPointIndex(routesDictionary[key], endPoint);

        if (indexStart < indexEnd)
        {
            // Parcours dans le sens avant
            for (int i = indexStart; i <= indexEnd; i++)
            {
                path.Add(routesDictionary[key][i]);
            }
        }
        else if (indexStart > indexEnd)
        {
            // Parcours dans le sens inverse
            for (int i = indexStart; i >= indexEnd; i--)
            {
                path.Add(routesDictionary[key][i]);
            }
        }
        else
        {
            // indexStart == indexEnd
            // Dans ce cas, ajoutez simplement le point unique
            path.Add(routesDictionary[key][indexStart]);
        }

        
        return path;
    }

    private Vector3 NextPoint(JsonFeature<DeLijnShapeProperties, GeoGeometryLineString> relevantShape, Vector3 position)
    {
        Dictionary<string, List<Vector3>> routesDictionary = new Dictionary<string, List<Vector3>>();

        string key = relevantShape.properties.shape_id;
        routesDictionary[key] = new List<Vector3>();
        foreach (var coord in relevantShape.geometry.coordinates)
        {
            Vector3 positionCes = geoJsonCesiumSpawner.GetCesiumPosition(coord[0], coord[1]);
            routesDictionary[key].Add(positionCes);
        }
        List<Vector3> fullRoute = routesDictionary[key];
        float threshold = 2.0f;
        int startIndex = FindClosestPointIndex(fullRoute, position) + 1;

        while (startIndex < fullRoute.Count)
        {
            float distance = Vector3.Distance(position, fullRoute[startIndex]);
            if (distance >= threshold)
            {
                Vector3 pointAtStartIndex = new Vector3(fullRoute[startIndex].x, fullRoute[startIndex].y, fullRoute[startIndex].z);
                return pointAtStartIndex;
            }

            startIndex++;
        }
        return Vector3.zero;

    }

    private bool DetermineOptimalDirection(List<Vector3> route, int startIndex, int endIndex)
    {
        // Calculer la distance totale dans les deux sens
        float forwardDistance = CalculatePathDistance(route, startIndex, endIndex, true);
        float reverseDistance = CalculatePathDistance(route, startIndex, endIndex, false);

        // Choisir le sens avec la distance minimale
        return forwardDistance <= reverseDistance;
    }

    private float CalculatePathDistance(List<Vector3> route, int startIndex, int endIndex, bool forward)
    {
        float totalDistance = 0f;

        if (forward)
        {
            for (int i = startIndex; i < endIndex; i++)
            {
                totalDistance += Vector3.Distance(route[i], route[i + 1]);
            }
        }
        else
        {
            for (int i = startIndex; i > endIndex; i--)
            {
                totalDistance += Vector3.Distance(route[i], route[i - 1]);
            }
        }

        return totalDistance;
    }

    private int FindClosestPointIndex(List<Vector3> route, Vector3 point)
    {
        int closestIndex = 0;
        float minDistance = float.MaxValue;

        for (int i = 0; i < route.Count; i++)
        {
            float distance = Vector3.Distance(route[i], point);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }
}

/* Code if you use GTFS data */
/*
 * 
void Awake()
{
    gtfsFilePath = Path.Combine(Application.dataPath, "Data/GTFS/DeLijn");
}

void Start()
{

    Code for API
    apiClient = new ApiClient(apiKey);
    apiClientDL = new ApiClient(specialDLApiKey);
    gtfsParser = new GTFSParserDeLijn();
    StartCoroutine(InitializeData());
    StartCoroutine(LoadGTFS_RTData());

}
private IEnumerator InitializeData()
{


    yield return StartCoroutine(LoadGTFSData());
    string stopsGeoJson = JsonConvert.SerializeObject(gtfsParser.ParseStops(stopsInfo), Formatting.Indented);
    string shapesGeoJson = JsonConvert.SerializeObject(gtfsParser.ParseShapes(shapesInfo), Formatting.Indented);
    Debug.Log(stopsGeoJson);
    Debug.Log(shapesGeoJson);
}

private IEnumerator LoadGTFSData()
{
    bool isFinished = false;

    yield return apiClient.GetApiData(apiUrlGTFS, (Dictionary<string, string> extractedFiles) =>
    {
        if (extractedFiles != null)
        {
            Debug.Log("Fichiers extraits avec succès !");
            foreach (var file in extractedFiles)
            {
                if (file.Key == "stops.txt")
                {
                    Debug.Log($"Nom du fichier: {file.Key}");
                    Debug.Log($"Contenu du fichier: {file.Value}");
                    stopsInfo = file.Value;
                }

                if (file.Key == "shapes.txt")
                {
                    Debug.Log($"Nom du fichier: {file.Key}");
                    Debug.Log($"Contenu du fichier: {file.Value}");
                    shapesInfo = file.Value;
                }

            }
        }
        else
        {
            Debug.LogError("Erreur lors de l'extraction des fichiers GTFS.");
        }

        isFinished = true;
    }, isGTFS: true, localGTFSFolder : gtfsFilePath);

    yield return new WaitUntil(() => isFinished);
}

private IEnumerator LoadGTFS_RTData()
{
    bool isFinished = false;

    yield return apiClientDL.GetApiData(apiUrlGTFS_RT, (FeedMessage feed) =>
    {
        if (feed != null)
        {
            Debug.Log("Fichiers extraits avec succès !");
        }
        else
        {
            Debug.LogError("Erreur lors de l'extraction des fichiers GTFS.");
        }

        isFinished = true;
    }, isGTFS_RT: true);

    yield return new WaitUntil(() => isFinished);
}*/



