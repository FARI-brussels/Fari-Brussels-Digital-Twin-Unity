using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TransitRealtime;
using Newtonsoft.Json;
using Google.Protobuf;
using CesiumForUnity;

public class SncbManager : MonoBehaviour
{

    [Header("Api Configuration for RT vehicule position")]
    [SerializeField] private string apiKey = "c6c3b6c061aee8a9e063489669f1222049c8c3901120dd356ffd0cc91c486922892b0801914af0d98297c87da86da70f58407e9d3c08dbdeb70f4e56eb038e96";
    [SerializeField] private string apiUrl = "https://api.mobilitytwin.brussels/scnb/vehicle-position";
    [SerializeField] private string apiUrlShapes = "https://opendata.infrabel.be/api/explore/v2.1/catalog/datasets/lijnsecties/exports/geojson?lang=fr&&timezone=Europe%2FBerlin";
    [SerializeField] private string apiUrlStops = "https://api.mobilitytwin.brussels/infrabel/operational-points";
    //[SerializeField] private string apiUrlGTFS_RT = "https://sncb-opendata.hafas.de/gtfs/realtime/c21ac6758dd25af84cca5b707f3cb3de";
    [SerializeField] private float refreshInterval = 5f;

    [Header("Reference")]
    [SerializeField] private Material defaultMaterialForLine;

    [Header("Prefab")]
    [SerializeField] private GameObject prefabTrain;
    [SerializeField] private GameObject prefabStop;

    [Header("Cesium")]
    public CesiumGeoreference georeference;
    private GeoJsonCesiumSpawner geoJsonCesiumSpawner = new GeoJsonCesiumSpawner();
    public Cesium3DTileset tileset;

    private ApiClient apiClient;

    //Parent to store segments, stops and vehicles
    private GameObject sncbParent;
    private GameObject linesParent;
    private GameObject stopsParent;
    private GameObject vehiclesParent;

    //Var to store the shapes & stops collection
    private JsonCollection<JsonFeature<SncbGeoPropertiesShapes, IGeoGeometry>> geoJsonDataShapes;
    private JsonCollection<JsonFeature<SncbGeoPropertiesStops, IGeoGeometry>> geoJsonDataStops;

    //Utils var for the vehicle
    public Dictionary<string, (GameObject, List<double>)> vehicles = new Dictionary<string, (GameObject, List<double>)>();
    private Dictionary<(string start, string end), List<Vector3>> routesDictionary = new Dictionary<(string start, string end), List<Vector3>>();

    public void InitSncb()
    {
        InitSceneGameObject();

        geoJsonCesiumSpawner.Initialize(georeference);

        //Instantiate ApiClient
        apiClient = new ApiClient(apiKey);

        //Coroutine to Initialize the network
        StartCoroutine(InitializeNetworkSequence());
    }

    private void InitSceneGameObject()
    {
        sncbParent = new GameObject { name = "Sncb Parent" };
        linesParent = new GameObject { name = "Lines" };
        linesParent.transform.parent = sncbParent.transform;
        stopsParent = new GameObject { name = "Stops" };
        stopsParent.transform.parent = sncbParent.transform;
        vehiclesParent = new GameObject { name = "Vehicles" };
        vehiclesParent.transform.parent = sncbParent.transform;
    }


    private IEnumerator InitializeNetworkSequence()
    {
        //Network Initialisation 
        yield return StartCoroutine(CreateSncbNetwork());

        //Full Segments routes
        GenerateFullSegments();

        //Coroutine to fetch data
        StartCoroutine(FetchDataRoutine());
    }

    private void GenerateFullSegments()
    {
        foreach (var feature in geoJsonDataShapes.features)
        {
            //Create a key with the line_id and the direction
            var key = (feature.properties.stationfrom_id, feature.properties.stationto_id);

            //Check if the key is already in the dict
            if (!routesDictionary.ContainsKey(key))
            {
                //Debug.Log($"{feature.properties.ptcarfrom} --> {feature.properties.ptcarto}");
                //If not create the new segments
                routesDictionary[key] = new List<Vector3>();
            }

            /* Extract Gps coordinates and store in unity format
             * !! We exclude the last value because it is the same as the next first value !!
             * Eg : 1st segment : [......;[4.355,50.245]] --> 2nd segment : [[4.355,50.245];......]]
            */

            if (feature.geometry is ILineStringGeometry lineString)
            {
                for (int i = 0; i < lineString.coordinates.Count - 1; i++)
                {
                    //Extract coordinates
                    var coord = lineString.coordinates[i];

                    //Convert to Unity Coodinate System
                    Vector3 position = geoJsonCesiumSpawner.GetCesiumPosition(coord[0], coord[1]);
                    routesDictionary[key].Add(position);
                }
            }
        }
    }

    public IEnumerator FetchDataRoutine()
    {
        while (true)
        {
            // Call API
            yield return StartCoroutine(apiClient.GetApiData<JsonCollection<JsonFeature<SncbGeoPropertiesVehiclePosition, IGeoGeometry>>>(apiUrl, GetSncbVehiclePosition));

            // Wait x sec
            yield return new WaitForSeconds(refreshInterval);
        }

    }
    

    private IEnumerator CreateSncbNetwork()
    {
        bool stopsLoaded = false;  // Flag to check if stops data has been loaded
        bool shapesLoaded = false; // Flag to check if shapes data has been loaded

        // Start coroutine to fetch stop data from API
        yield return StartCoroutine(apiClient.GetApiData<JsonCollection<JsonFeature<SncbGeoPropertiesStops, IGeoGeometry>>>(
            apiUrlStops, (data) => {
                SncbStop(data);  // Process stop data
                stopsLoaded = true;  // Mark stops as loaded
            }
        ));

        
        // Start coroutine to fetch shape data from API
        yield return StartCoroutine(apiClient.GetApiData<JsonCollection<JsonFeature<SncbGeoPropertiesShapes, IGeoGeometry>>>(
            apiUrlShapes, (data) => {
                geoJsonDataShapes = data;  // Store the fetched shape data
                //SncbShapes(data);  // Process shape data
                shapesLoaded = true;  // Mark shapes as loaded
            }
        ));

        // Wait until both stops and shapes data have been fully loaded
        while (!stopsLoaded || !shapesLoaded)
        {
            yield return null;
        }
    }

    //DrawSncbShapes
    private void SncbShapes(JsonCollection<JsonFeature<SncbGeoPropertiesShapes, IGeoGeometry>> data)
    {
        geoJsonDataShapes = data;  // Store the shape data

        if (geoJsonDataShapes != null)
        {
            foreach (var feature in geoJsonDataShapes.features)
            {
                DrawLineString(feature);
            }
        }
    }

    private void DrawLineString(JsonFeature<SncbGeoPropertiesShapes, IGeoGeometry> feature)
    {
        if (feature.geometry is ILineStringGeometry lineString)
        {
            //Instantiate Stib Stop
            GameObject lineObject = new GameObject("Line_" + feature.properties.stationfrom_id);
            LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();

            //Visualization
            lineRenderer.alignment = LineAlignment.TransformZ;
            lineObject.transform.rotation = Quaternion.Euler(90, 0, 0);
            lineRenderer.positionCount = lineString.coordinates.Count;
            lineRenderer.startWidth = 4f;
            lineRenderer.endWidth = 4f;
            lineRenderer.material = defaultMaterialForLine;
            Color lineColor = Color.white;

            lineRenderer.startColor = lineColor;
            lineRenderer.endColor = lineColor;

            //Position and draw
            for (int i = 0; i < lineString.coordinates.Count; i++)
            {
                double[] coord = lineString.coordinates[i];
                Vector3 position = geoJsonCesiumSpawner.GetCesiumPosition(coord[0], coord[1]);
                lineRenderer.SetPosition(i, position);
            }

            //Parent
            lineRenderer.transform.parent = linesParent.transform;
        }
        
    }

    private void SncbStop(JsonCollection<JsonFeature<SncbGeoPropertiesStops, IGeoGeometry>> data)
    {
        geoJsonDataStops = data;
        if (geoJsonDataStops != null)
        {
            foreach (var feature in geoJsonDataStops.features)
            {
                if (feature.geometry is IPointGeometry pointGeometry)
                {
                    if (pointGeometry != null)
                    {
                        if (feature.properties.class_en == "Station" || feature.properties.class_en == "Stop in open track")
                        {
                            double lon = pointGeometry.coordinates[0];
                            double lat = pointGeometry.coordinates[1];
                            //if (lat >= 50.79 && lat <= 50.91 && lon >= 4.25 && lon <= 4.45)
                            //{
                

                            Vector3 position = geoJsonCesiumSpawner.GetCesiumPosition(pointGeometry.coordinates[0], pointGeometry.coordinates[1]);

                            // Create a marker for the stop in Unity
                            CreateMarkeurStop(position, feature);//, stop.stop_name, stop.stop_id, stop.route_short_name, feature);
                            //}   
                        }
                    } 
                }
            }
        }
    }
    private void CreateMarkeurStop(Vector3 position, JsonFeature<SncbGeoPropertiesStops, IGeoGeometry> feature )
    {

        //Instantiate Stib Stop
        GameObject vehicleGameObject = Instantiate(prefabStop);
        Renderer sphereRenderer = vehicleGameObject.GetComponent<Renderer>();
        BoxCollider boxCollider = vehicleGameObject.GetComponent<BoxCollider>();

        ShowInfoOnHoverSncb hoverScript = vehicleGameObject.AddComponent<ShowInfoOnHoverSncb>();
        hoverScript.Initialize(feature);
        ShowInfoOnClickSncb clickScript = vehicleGameObject.AddComponent<ShowInfoOnClickSncb>();
        clickScript.Initialize(feature);

        //Visualization
        vehicleGameObject.name = feature.properties.ptcarid;
        sphereRenderer.material.color = Color.red;

        //Position
        vehicleGameObject.transform.position = position;

        //Size
        vehicleGameObject.transform.localScale = new Vector3(500f, 500f, 500f);

        //Parent
        vehicleGameObject.transform.parent = stopsParent.transform;
    }

    private GameObject CreateMarkeurVecPos(Vector3 position, JsonFeature<SncbGeoPropertiesVehiclePosition, IGeoGeometry> feature)
    {
        var key = (feature.properties.ptcarid_start, feature.properties.ptcarid_end);
        var route = routesDictionary[key];

        Vector3 endPoint = Vector3.zero;
        if (feature.geometry is IPointGeometry pointGeometry)
        {

            endPoint = geoJsonCesiumSpawner.GetCesiumPosition(pointGeometry.coordinates[0], pointGeometry.coordinates[1]);

        }

        //Instantiate sncb vehicle
        GameObject vehicleGameObject = Instantiate(prefabTrain);
        SncbVehicleMover mover = vehicleGameObject.AddComponent<SncbVehicleMover>();

        //Visualization
        vehicleGameObject.name = feature.properties.trip_id;
        //Rotation
        Quaternion rotation = InitVehicleRotation(position, endPoint, route);
        vehicleGameObject.transform.rotation = rotation;
        //Position
        vehicleGameObject.transform.position = position;
        //Parent
        vehicleGameObject.transform.parent = vehiclesParent.transform;

        ShowInfoOnHoverSncb hoverScript = vehicleGameObject.AddComponent<ShowInfoOnHoverSncb>();
        hoverScript.Initialize(feature);
        var clickScript = vehicleGameObject.AddComponent<ShowInfoOnClickSncb>();
        clickScript.Initialize(feature);

        return vehicleGameObject;
    }

    private Quaternion InitVehicleRotation(Vector3 position, Vector3 endPoint, List<Vector3> route)
    {
        int indexDirection = FindClosestPointIndexDirectional(route, position, position, endPoint, true);
        if (indexDirection + 2 < route.Count)
        {
            Vector3 lookDirection = route[indexDirection + 1] - position;
            lookDirection.y = 0f; // Ignore la hauteur
            if (lookDirection != Vector3.zero)
            {
                return Quaternion.LookRotation(lookDirection);
            }
        }
        else
        {
            Vector3 lookDirection = endPoint - position;
            lookDirection.y = 0f; // Ignore la hauteur
            if (lookDirection != Vector3.zero)
            {
                return Quaternion.LookRotation(lookDirection);
            }
        }
        return Quaternion.identity;
    }

    private void GetSncbVehiclePosition(JsonCollection<JsonFeature<SncbGeoPropertiesVehiclePosition, IGeoGeometry>> data)
    {
        if (data != null)
        {
            foreach (var feature in data.features)
            {
                if (feature.geometry is IPointGeometry pointGeometry)
                {
                    if (pointGeometry != null)
                    {
              
                        Vector3 newPositionUcsStop = geoJsonCesiumSpawner.GetCesiumPosition(pointGeometry.coordinates[0], pointGeometry.coordinates[1]);


                        if (vehicles.ContainsKey(feature.properties.trip_id))
                        {
                            
                            Vector3 positioninit = vehicles[feature.properties.trip_id].Item1.transform.position;

                            GameObject vehicle = vehicles[feature.properties.trip_id].Item1;

                            float distance = Vector3.Distance(positioninit, newPositionUcsStop);


                            if (distance < 1500)
                            {
                                Vector3 initPosition = vehicles[feature.properties.trip_id].Item1.transform.position;
                                if (initPosition != newPositionUcsStop)
                                {
                                    List<Vector3> path = FindPath(feature.properties.ptcarid_start, feature.properties.ptcarid_end, initPosition, newPositionUcsStop);

                                    SncbVehicleMover mover = vehicle.GetComponent<SncbVehicleMover>();
                                    if (mover != null && path.Count > 0)
                                    {
                                        //The refresh time = travel time
                                        float travelTime = 20f;
                                        mover.SetPath(path, travelTime);
                                    }
                                }
                            }
                            else
                            {
                                vehicles[feature.properties.trip_id].Item1.transform.position = newPositionUcsStop;
                            }

                                
                            vehicles[feature.properties.trip_id] = (vehicle, pointGeometry.coordinates);
                        }
                        else
                        {
                               
                            // Create a marker for the stop in Unity
                            GameObject newVehicle = CreateMarkeurVecPos(newPositionUcsStop, feature);//, stop.stop_name, stop.stop_id, stop.route_short_name, feature);
                            // Add the new vehicle to the dictionary with its position
                            vehicles.Add($"{feature.properties.trip_id}",
                                (newVehicle, new List<double>
                                    { pointGeometry.coordinates[0], pointGeometry.coordinates[1] }
                                )
                            );
                        }
                    }

                }
            }
        }
    }


    public List<Vector3> FindPath(string ptcarid_start, string ptcarid_end, Vector3 startPoint, Vector3 endPoint)
    {
        List<Vector3> path = new List<Vector3>();

        var key = (ptcarid_start, ptcarid_end);
        if (!routesDictionary.ContainsKey(key))
        {
            Debug.LogError($"No route found for start {ptcarid_start} --> end {ptcarid_end}");
            return new List<Vector3> { startPoint, endPoint };
        }

        var route = routesDictionary[key];

        int indexStart = FindClosestPointIndexDirectional(route, startPoint, startPoint, endPoint, true);
        int indexEnd = FindClosestPointIndexDirectional(route, endPoint, startPoint, endPoint, false);


        
        for (int i = indexStart; i <= indexEnd; i++)
        {
            path.Add(route[i]);
        }
        return path;
    }

    private int FindClosestPointIndexDirectional(List<Vector3> route, Vector3 point, Vector3 startPoint, Vector3 endPoint, bool isStartPoint)
    {
        // Déterminer la direction générale du chemin
        Vector3 pathDirection = endPoint - startPoint;

        int closestIndex = -1;
        float minDistance = float.MaxValue;

        for (int i = 0; i < route.Count; i++)
        {
            float distance = Vector3.Distance(route[i], point);

            if (distance < minDistance)
            {
                // Vérifions si le point est "devant" dans la direction souhaitée
                Vector3 pointDirection = route[i] - point;

                // Pour le point de départ, on veut que le vecteur pointDirection soit dans la même direction que pathDirection
                // Pour le point d'arrivée, c'est l'inverse (on veut un point dans le sens du chemin)
                bool isValid;

                if (isStartPoint)
                {
                    // Pour le point de départ, le point doit être devant dans la direction du chemin
                    isValid = Vector3.Dot(pointDirection, pathDirection) >= 0;
                }
                else
                {
                    // Pour le point d'arrivée, on cherche un point qui soit "devant" ce point final
                    // dans la direction générale du chemin
                    isValid = Vector3.Dot(pointDirection, pathDirection) <= 0;
                }

                if (isValid)
                {
                    minDistance = distance;
                    closestIndex = i;
                }
            }
        }

        // Si aucun point valide n'est trouvé (ce qui est rare mais possible), on revient au point le plus proche
        if (closestIndex == -1)
        {
            return FindClosestPointIndex(route, point);
        }

        return closestIndex;
    }

    // Conservez l'ancienne fonction comme fallback
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
    /*
    // Trouver l'index du point le plus proche
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
    }*/

    

}

