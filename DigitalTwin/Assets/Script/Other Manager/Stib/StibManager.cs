using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using System;
using CesiumForUnity;
using System.Threading.Tasks;

public class StibManager : MonoBehaviour
{
    //PREFAB
    [Header("Prefab")]
    public GameObject prefabMetro;
    public GameObject prefabBus;
    public GameObject prefabTram;
    public GameObject prefabStop;

    [Header("Shapes")]
    [SerializeField] private string prefabResourcePath = "Prefabs/StibShapes";
    [SerializeField] private string prefabName = "StibShapes";
    private GameObject stibShapesPrefab;


    //Configuration for real time vehicule position
    [Header("Api Configuration for RT vehicule position")]
    [SerializeField] private string apiKey = "c6c3b6c061aee8a9e063489669f1222049c8c3901120dd356ffd0cc91c486922892b0801914af0d98297c87da86da70f58407e9d3c08dbdeb70f4e56eb038e96";
    [SerializeField] private string apiUrl = "https://api.mobilitytwin.brussels/stib/vehicle-position";
    [SerializeField] private string apiUrlShapes = "https://api.mobilitytwin.brussels/stib/segments";
    [SerializeField] private string apiUrlStops = "https://api.mobilitytwin.brussels/stib/stops";
    [SerializeField] private float refreshInterval = 5f;
    private ApiClient apiClient;

    //Default Material for line renderer
    public Material defaultMaterialForLine;

    //Var to store the shapes & stops collection
    private JsonCollection<JsonFeature<StibGeoPropertiesShapes, GeoGeometryLineString>> geoJsonDataShapes;
    private JsonCollection<JsonFeature<StibGeoPropertiesStops, GeoGeometryPoint>> geoJsonDataStops;

    //Data Storage
    private Dictionary<(string line_id, int direction), List<Vector3>> routesDictionary = new Dictionary<(string line_id, int direction), List<Vector3>>();
    public Dictionary<string, (GameObject, List<double>)> vehicles = new Dictionary<string, (GameObject, List<double>)>();

    //Parent to store segments, stops and vehicles
    private GameObject stibParent;
    private GameObject linesParent;
    private GameObject stopsParent;
    private GameObject vehiclesParent;

    [Header("Cesium")]
    public CesiumGeoreference georeference;
    private GeoJsonCesiumSpawner geoJsonCesiumSpawner = new GeoJsonCesiumSpawner();
    public Cesium3DTileset tileset;

    public void InitStib()
    {
        InitSceneGameObject();
        geoJsonCesiumSpawner.Initialize(georeference);
        apiClient = new ApiClient(apiKey);

        StartCoroutine(InitializeNetworkSequence());
    }

    private IEnumerator InitializeNetworkSequence()
    {
        // Step 1 = Create Stib Network
        yield return StartCoroutine(CreateStibNetwork());

        // Step 2: Generate full segments for each lines
        GenerateFullSegments();

        // Step 3: Get RT vehicle position
        StartCoroutine(FetchDataRoutine());
    }

    private void GenerateFullSegments()
    {
        foreach (var feature in geoJsonDataShapes.features)
        {
            //Create a key with the line_id and the direction
            var key = (feature.properties.line_id, feature.properties.direction);

            //Check if the key is already in the dict
            if (!routesDictionary.ContainsKey(key))
            {
                //If not create the new segments
                routesDictionary[key] = new List<Vector3>();
            }

            /* Extract Gps coordinates and store in unity format
             * !! We exclude the last value because it is the same as the next first value !!
             * Eg : 1st segment : [......;[4.355,50.245]] --> 2nd segment : [[4.355,50.245];......]]
            */
            for (int i = 0; i < feature.geometry.coordinates.Count; i++)
            {
                //Extract coordinates
                var coord = feature.geometry.coordinates[i];

                //Convert to Unity Coodinate System
                Vector3 position = geoJsonCesiumSpawner.GetCesiumPosition(coord[0], coord[1]);
                float unityY = IsMetro(feature.properties.line_id) ? position .y - 400 : position.y;
                Vector3 positionUcsStop = new Vector3(position.x, unityY, position.z);
                routesDictionary[key].Add(positionUcsStop);

            }
        }
        /*foreach (var route in routesDictionary)
        {
            var lineId = route.Key.line_id;
            var direction = route.Key.direction;
            var points = route.Value;

            Debug.Log($"Ligne {lineId} - Direction {direction} : {points.Count} points");

            /*foreach (var point in points)
            {
                Debug.Log($"Point: {point}");
            }
        }*/

    }
    public IEnumerator FetchDataRoutine()
    {
        Debug.Log("FetchDataRoutine");
        while (true)
        {
            // Call API
            yield return StartCoroutine(apiClient.GetApiData<JsonCollection<JsonFeature<StibGeoPropertiesVehiclePostion, GeoGeometryPoint>>>(apiUrl, GetStibVehiclePosition));

            // Wait x sec
            yield return new WaitForSeconds(refreshInterval);
        }

    }
    private void GetStibVehiclePosition(JsonCollection<JsonFeature<StibGeoPropertiesVehiclePostion, GeoGeometryPoint>> data)
    {
        // Check if data is null
        if (data == null)
        {
            Debug.LogError("Failed to retrieve STIB data.");
            return;
        }
        //int count = 0;
        // Iterate through each vehicle position data point
        foreach (JsonFeature<StibGeoPropertiesVehiclePostion, GeoGeometryPoint> feature in data.features)
        {
            JsonFeature<StibGeoPropertiesStops, GeoGeometryPoint> terminus = FindTerminus(feature.properties.lineId, feature.properties.direction - 1);

            // Convert the vehicle's GPS position into Unity's coordinate system
            Vector3 newPositionUcsVec = FindUcsPosition(
                feature.properties.lineId,
                feature.properties.direction,
                feature.properties.pointId,
                feature.properties.distanceFromPoint
            );

            Vector3 targetDirection = NextPoint(feature.properties.lineId, feature.properties.direction, newPositionUcsVec);

            // Check if the vehicle already exists in the dictionary
            if (vehicles.ContainsKey(feature.id))
            {
                // Move the existing vehicle to the new position
                MoveVehicle(
                    feature.id,
                    feature.properties.lineId,
                    feature.properties.direction,
                    newPositionUcsVec,
                    feature.geometry.coordinates
                );
            }
            else
            {
                // Create a new vehicle marker in Unity
                GameObject newVehicle = CreateMarkeurVehicle(
                    newPositionUcsVec,
                    feature.properties.lineId,
                    feature.properties.direction,
                    feature,
                    terminus,
                    targetDirection
                );

                // Add the new vehicle to the dictionary with its position
                vehicles.Add(feature.id,
                    (newVehicle, new List<double>
                        { feature.geometry.coordinates[0], feature.geometry.coordinates[1] }
                    )
                );


            }
        }
    }

    private GameObject CreateMarkeurVehicle(Vector3 position, string line_id, int direction, JsonFeature<StibGeoPropertiesVehiclePostion, GeoGeometryPoint> feature, JsonFeature<StibGeoPropertiesStops, GeoGeometryPoint> terminus, Vector3 targetDirection)
    {

        //Instantiate Stib Vehicle
        var vehicle = new VehicleClass(line_id, direction);
        if (vehicle.Type == VehicleType.Metro)
        {
            GameObject vehicleGameObject = Instantiate(prefabMetro);
            Renderer vehicleRenderer = vehicleGameObject.GetComponent<Renderer>();
            VehicleMoverStib mover = vehicleGameObject.AddComponent<VehicleMoverStib>();

            //Visualization
            vehicleGameObject.name = vehicle.Name;
            //vehicleRenderer.material.color = vehicle.Color;

            //Position
            vehicleGameObject.transform.position = position;

            //Size
            vehicleGameObject.transform.localScale = new Vector3(2f, 2f, 2f);

            //Parent
            vehicleGameObject.transform.parent = vehiclesParent.transform;

            Vector3 lookAtTarget = targetDirection;
            lookAtTarget.y = vehicleGameObject.transform.position.y; // On garde la hauteur actuelle

            vehicleGameObject.transform.LookAt(lookAtTarget);


            //Show information hover
            BoxCollider boxCollider = vehicleGameObject.GetComponent<BoxCollider>();

            ShowInfoOnHoverStib hoverScript = vehicleGameObject.AddComponent<ShowInfoOnHoverStib>();
            hoverScript.Initialize(feature, terminus);
            ShowInfoOnClickStib clickScript = vehicleGameObject.AddComponent<ShowInfoOnClickStib>();
            clickScript.Initialize(feature, terminus);

            return vehicleGameObject;
        }
        else if (vehicle.Type == VehicleType.Bus)
        {
            GameObject vehicleGameObject = Instantiate(prefabBus);
            Renderer vehicleRenderer = vehicleGameObject.GetComponent<Renderer>();
            VehicleMoverStib mover = vehicleGameObject.AddComponent<VehicleMoverStib>();

            //Visualization
            vehicleGameObject.name = vehicle.Name;
            //vehicleRenderer.material.color = vehicle.Color;

            //Position
            vehicleGameObject.transform.position = position;

            //Size
            vehicleGameObject.transform.localScale = new Vector3(2f, 2f, 2f);

            //Parent
            vehicleGameObject.transform.parent = vehiclesParent.transform;

            Vector3 lookAtTarget = targetDirection;
            lookAtTarget.y = vehicleGameObject.transform.position.y; // On garde la hauteur actuelle

            vehicleGameObject.transform.LookAt(lookAtTarget);

            //Show information hover
            BoxCollider boxCollider = vehicleGameObject.GetComponent<BoxCollider>();

            ShowInfoOnHoverStib hoverScript = vehicleGameObject.AddComponent<ShowInfoOnHoverStib>();
            hoverScript.Initialize(feature, terminus);
            ShowInfoOnClickStib clickScript = vehicleGameObject.AddComponent<ShowInfoOnClickStib>();
            clickScript.Initialize(feature, terminus);

            return vehicleGameObject;
        }
        else
        {
            GameObject vehicleGameObject = Instantiate(prefabTram);
            Renderer vehicleRenderer = vehicleGameObject.GetComponent<Renderer>();
            VehicleMoverStib mover = vehicleGameObject.AddComponent<VehicleMoverStib>();

            //Visualization
            vehicleGameObject.name = vehicle.Name;
            //vehicleRenderer.material.color = vehicle.Color;

            //Position
            vehicleGameObject.transform.position = position;

            //Size
            vehicleGameObject.transform.localScale = new Vector3(2f, 2f, 2f);

            //Parent
            vehicleGameObject.transform.parent = vehiclesParent.transform;

            Vector3 lookAtTarget = targetDirection;
            lookAtTarget.y = vehicleGameObject.transform.position.y; // On garde la hauteur actuelle

            vehicleGameObject.transform.LookAt(lookAtTarget);


            ShowInfoOnHoverStib hoverScript = vehicleGameObject.AddComponent<ShowInfoOnHoverStib>();
            hoverScript.Initialize(feature, terminus);
            ShowInfoOnClickStib clickScript = vehicleGameObject.AddComponent<ShowInfoOnClickStib>();
            clickScript.Initialize(feature, terminus);

            return vehicleGameObject;
        }
    }

    public List<Vector3> FindPath(string line_id, int direction, Vector3 startPoint, Vector3 endPoint)
    {
        List<Vector3> path = new List<Vector3>();

        // Récupérer le chemin complet pour cette ligne et direction
        var key = (line_id, direction);
        if (!routesDictionary.ContainsKey(key))
        {
            Debug.LogError($"No route found for line {line_id} and direction {direction}");
            return path;
        }

        List<Vector3> fullRoute = routesDictionary[key];

        // Trouver les indices des points les plus proches du départ et de l'arrivée
        int startIndex = FindClosestPointIndex(fullRoute, startPoint);
        int endIndex = FindClosestPointIndex(fullRoute, endPoint);

        // S'assurer qu'on suit la bonne direction
        if (startIndex <= endIndex)
        {
            for (int i = startIndex; i <= endIndex; i++)
            {
                path.Add(fullRoute[i]);
            }
        }
        else
        {
            // Cas où l'utilisateur choisit un point plus avancé sur la ligne que le point de destination
            for (int i = startIndex; i >= endIndex; i--)
            {
                path.Add(fullRoute[i]);
            }
            path.Reverse(); // Remettre dans l'ordre de progression
        }

        // Gestion du cas où la distance entre `startPoint` et `endPoint` est trop courte
        if (Vector3.Distance(startPoint, endPoint) < 0.1f)
        {
            return new List<Vector3> {startPoint};
        }


        return path;
    }

    private void MoveVehicle(string vehicleId, string lineId, int direction, Vector3 newPosition, List<double> coordinates)
    {
        GameObject vehicle = vehicles[vehicleId].Item1;
        Vector3 currentPosition = vehicle.transform.position;

        List<Vector3> path = FindPath(lineId, direction, currentPosition, newPosition);

        VehicleMoverStib mover = vehicle.GetComponent<VehicleMoverStib>();
        if (mover != null && path.Count > 0)
        {
            float travelTime = 20f;
            mover.SetPath(path, travelTime);
        }

        vehicles[vehicleId] = (vehicle, coordinates);
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
    private Vector3 NextPoint(string line_id, int direction, Vector3 position)
    {
        var key = (line_id, direction);
        if (!routesDictionary.ContainsKey(key))
        {
            Debug.LogError($"No route found for line {line_id} and direction {direction}");
            return Vector3.zero;
        }

        List<Vector3> fullRoute = routesDictionary[key];

        float threshold = 1.0f;
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
        Debug.LogWarning($"Aucun point trouvé avec une distance > {threshold}. Fin de la route atteinte.");
        return Vector3.zero;

    }
    private Vector3 FindUcsPosition(string line_id, int direction, int start, int distanceFromPoint)
    {
        //Check if geoJsonDataShapes exists
        if (geoJsonDataShapes == null)
        {
            Debug.LogError("ShapeData est null!");
            return Vector3.zero;
        }

        //Find the correct shapes for the vehicule in a specific position
        JsonFeature<StibGeoPropertiesShapes, GeoGeometryLineString> relevantShape = geoJsonDataShapes.features.Find(
            s => s.properties.line_id == line_id &&
                 s.properties.direction == direction &&
                 s.properties.start == start
        );

        //Check if the relevant Shape is found
        if (relevantShape == null)
        {
            Debug.LogError($"No shape found for the {line_id}, Direction {direction}, Point ID {start}");
            return Vector3.zero;
        }

        // Try calculating the target position with the shape found
        try
        {
            //Function to calculate the target point
            return CalculateTargetPoint(relevantShape, distanceFromPoint);
        }
        catch (Exception e)
        {
            Debug.LogError($"Target point calculation error, message : {e.Message}");
            // Fallback: return first point
            Vector3 position = geoJsonCesiumSpawner.GetCesiumPosition(relevantShape.geometry.coordinates[0][0], relevantShape.geometry.coordinates[0][1]);
            float unityY = IsMetro(line_id) ? position.y - 400 : position.y;
            return new Vector3(position.x, unityY, position.z);
        }
    }

    private Vector3 CalculateTargetPoint(JsonFeature<StibGeoPropertiesShapes, GeoGeometryLineString> shape, int distanceFromPoint)
    {
        // Convert every data from GPS to UCS
        List<Vector3> ucsWayPoints = new List<Vector3>();
        foreach (var coordinate in shape.geometry.coordinates)
        {
            Vector3 position = geoJsonCesiumSpawner.GetCesiumPosition(coordinate[0], coordinate[1]);
            float unityY = IsMetro(shape.properties.line_id) ? position.y - 400 : position.y;
            return new Vector3(position.x, unityY, position.z);
        }

        //Is points found in the shape
        if (ucsWayPoints.Count == 0)
        {
            Debug.LogWarning("No coordinate points found in the shape");
            return Vector3.zero;
        }

        //Is only one point 
        if (ucsWayPoints.Count == 1)
        {
            return ucsWayPoints[0];
        }

        // Total distance calculation between two points
        float totalLength = 0;
        float[] segmentLengths = new float[ucsWayPoints.Count - 1];
        //Calculate the distance for every segments and add to totalLength
        for (int i = 0; i < ucsWayPoints.Count - 1; i++)
        {
            segmentLengths[i] = Vector3.Distance(ucsWayPoints[i], ucsWayPoints[i + 1]);
            totalLength += segmentLengths[i];
        }

        //Determine the percentage of distance to cover
        float distanceToTravel = 0f;

        //Is begining check 
        if (shape.properties.distance > 0)
        {
            //Calculate the distance to travel between start and target
            float percentComplete = Mathf.Clamp01((float)distanceFromPoint / (float)shape.properties.distance);
            distanceToTravel = percentComplete * totalLength;
        }
        else
        {
            // Fallback: Use the distance directly
            float percentComplete = Mathf.Clamp01((float)distanceFromPoint / 100f);
            distanceToTravel = percentComplete * totalLength;
        }

        //Limit Case
        if (distanceToTravel <= 0)
        {
            return ucsWayPoints[0];
        }

        if (distanceToTravel >= totalLength)
        {
            return ucsWayPoints[ucsWayPoints.Count - 1];
        }

        // Parcourir le chemin pour trouver le point spécifique
        float accumulatedDistance = 0;

        for (int i = 0; i < segmentLengths.Length; i++)
        {
            if (accumulatedDistance + segmentLengths[i] >= distanceToTravel)
            {
                // Calculer la position relative dans ce segment
                float segmentPercentage = (distanceToTravel - accumulatedDistance) / segmentLengths[i];

                // Interpoler la position entre les deux points du segment
                return Vector3.Lerp(ucsWayPoints[i], ucsWayPoints[i + 1], segmentPercentage);
            }

            accumulatedDistance += segmentLengths[i];
        }

        // Cas d'erreur: retourner le dernier point
        Debug.LogWarning("La distance calculée dépasse la longueur du chemin");
        return ucsWayPoints[ucsWayPoints.Count - 1];
    }
    private JsonFeature<StibGeoPropertiesStops, GeoGeometryPoint> FindTerminus(string line_id, int direction)
    {

        List<JsonFeature<StibGeoPropertiesStops, GeoGeometryPoint>> relevantStopsList = geoJsonDataStops.features
        .FindAll(s => s.properties.route_short_name == line_id && s.properties.direction == direction);
        return relevantStopsList[relevantStopsList.Count - 1];

    }

    private IEnumerator CreateStibNetwork()
    {
        bool stopsLoaded = false;  // Flag to check if stops data has been loaded
        bool shapesLoaded = false; // Flag to check if shapes data has been loaded

        // Start coroutine to fetch stop data from API
        yield return StartCoroutine(apiClient.GetApiData<JsonCollection<JsonFeature<StibGeoPropertiesStops, GeoGeometryPoint>>>(
            apiUrlStops, (data) => {
                StibStop(data);  // Process stop data
                stopsLoaded = true;  // Mark stops as loaded
            }
        ));

        yield return StartCoroutine(apiClient.GetApiData<JsonCollection<JsonFeature<StibGeoPropertiesShapes, GeoGeometryLineString>>>(
                apiUrlShapes, (data) =>
                {
                    geoJsonDataShapes = data;  // Store the fetched shape data
                    StibShapes(data);
                    shapesLoaded = true;  // Mark shapes as loaded
                }
        ));

        /*stibShapesPrefab = Resources.Load<GameObject>(prefabResourcePath);
        if (stibShapesPrefab != null)
        {
            Debug.Log("StibShapes prefab trouvé, instanciation en cours...");
            GameObject stibShapesInstance = Instantiate(stibShapesPrefab, Vector3.zero, Quaternion.identity);
            stibShapesInstance.transform.parent = stibParent.transform;
            yield return StartCoroutine(apiClient.GetApiData<JsonCollection<JsonFeature<StibGeoPropertiesShapes, GeoGeometryLineString>>>(
                apiUrlShapes, (data) =>
                {
                    geoJsonDataShapes = data; 
                    shapesLoaded = true;
                }
            ));
        }
        else
        {
            float startTime = Time.realtimeSinceStartup;
            // Start coroutine to fetch shape data from API
            yield return StartCoroutine(apiClient.GetApiData<JsonCollection<JsonFeature<StibGeoPropertiesShapes, GeoGeometryLineString>>>(
                apiUrlShapes, (data) =>
                {
                    geoJsonDataShapes = data;  // Store the fetched shape data
                    StibShapes(data);  // Process shape data
                    shapesLoaded = true;  // Mark shapes as loaded
                }
            ));
            SaveAsPrefab(linesParent);
            float duration = Time.realtimeSinceStartup - startTime;
            Debug.Log("Durée Mesh : " + duration + " secondes");
        }*/



        // Wait until both stops and shapes data have been fully loaded
        while (!stopsLoaded || !shapesLoaded)
        {
            yield return null;
        }
    }

    private void StibStop(JsonCollection<JsonFeature<StibGeoPropertiesStops, GeoGeometryPoint>> data)
    {
        geoJsonDataStops = data;

        if (geoJsonDataStops != null)
        {
            foreach (var feature in geoJsonDataStops.features)
            {
                var stop = feature.properties;  // Extract stop properties

                Vector3 positionUcsStop = geoJsonCesiumSpawner.GetCesiumPosition(stop.stop_lon, stop.stop_lat);
                float unityY = IsMetro(stop.route_short_name) ? positionUcsStop.y - 400 : positionUcsStop.y;
                positionUcsStop = new Vector3(positionUcsStop.x, unityY, positionUcsStop.z);

                // Create a marker for the stop in Unity
                CreateMarkeurStop(positionUcsStop, stop.stop_name, stop.stop_id, stop.route_short_name, feature);
            }
        }
    }
    private void CreateMarkeurStop(Vector3 position, String stop_name, int stop_id, string route_short_name, JsonFeature<StibGeoPropertiesStops, GeoGeometryPoint> feature)
    {

        //Instantiate Stib Stop
        var stop = new StopClass(route_short_name, stop_name, stop_id);
        GameObject stopGameObject = Instantiate(prefabStop);
        Renderer gameObjectRenderer = stopGameObject.GetComponent<Renderer>();

        //Visualization
        stopGameObject.name = stop.Name;
        gameObjectRenderer.material = new Material(gameObjectRenderer.material);
        gameObjectRenderer.material.color = stop.Color;

        //Position
        stopGameObject.transform.position = position;

        //Size
        stopGameObject.transform.localScale = new Vector3(500f, 500f, 500f);

        //Parent
        stopGameObject.transform.parent = stopsParent.transform;


        ShowStopInfoOnHoverStib hoverScript = stopGameObject.AddComponent<ShowStopInfoOnHoverStib>();
        hoverScript.Initialize(feature);
    }

    private void StibShapes(JsonCollection<JsonFeature<StibGeoPropertiesShapes, GeoGeometryLineString>> data)
    {
        geoJsonDataShapes = data;  // Store the shape data

        if (geoJsonDataShapes != null)
        {
            foreach (var feature in geoJsonDataShapes.features)
            {
                if (IsMetro(feature.properties.line_id))
                {
                    DrawLine(feature);
                }
            }
        }
    }

    private void StibShapes2(JsonCollection<JsonFeature<StibGeoPropertiesShapes, GeoGeometryLineString>> data)
    {
        geoJsonDataShapes = data;  // Store the shape data
        if (geoJsonDataShapes == null) return;

        // Création d'une liste pour stocker les données préparées
        List<LineRenderData> linesToRender = new List<LineRenderData>(geoJsonDataShapes.features.Count);

        // Préparer les données pour chaque ligne de manière séquentielle mais optimisée
        foreach (var feature in geoJsonDataShapes.features)
        {
            // Déterminer la couleur une seule fois
            Color lineColor = Color.white;
            if (ColorUtility.TryParseHtmlString(feature.properties.color, out Color parsedColor))
            {
                lineColor = parsedColor;
            }

            // Pré-allouer un tableau pour toutes les positions
            Vector3[] positions = new Vector3[feature.geometry.coordinates.Count];

            // Calculer toutes les positions en une seule passe
            for (int i = 0; i < feature.geometry.coordinates.Count; i++)
            {
                double[] coord = feature.geometry.coordinates[i];
                Vector3 positionCesium = geoJsonCesiumSpawner.GetCesiumPosition(coord[0], coord[1]);
                float unityY = IsMetro(feature.properties.line_id) ? positionCesium.y - 400 : positionCesium.y;
                positions[i] = new Vector3(positionCesium.x, unityY, positionCesium.z);
            }

            // Stocker les données calculées
            linesToRender.Add(new LineRenderData
            {
                Id = feature.id,
                Positions = positions,
                Color = lineColor,
                IsMetro = IsMetro(feature.properties.line_id)
            });
        }

        Material sharedMaterial = defaultMaterialForLine;

        StartCoroutine(CreateLineRenderersBatched(linesToRender, sharedMaterial));
    }

    // Coroutine pour créer les LineRenderers par lots
    private IEnumerator CreateLineRenderersBatched(List<LineRenderData> linesToRender, Material sharedMaterial)
    {
        int batchSize = 20; // Ajuster selon les performances
        int totalLines = linesToRender.Count;

        for (int i = 0; i < totalLines; i += batchSize)
        {
            int currentBatchSize = Mathf.Min(batchSize, totalLines - i);

            for (int j = 0; j < currentBatchSize; j++)
            {
                int index = i + j;
                if (index >= linesToRender.Count) break;

                var lineData = linesToRender[index];
                CreateLineRenderer(lineData, sharedMaterial);
            }

            // Rendre la main à Unity après chaque lot pour éviter les blocages
            yield return null;
        }
    }

    private void CreateLineRenderer(LineRenderData lineData, Material material)
    {
        GameObject lineObject = new GameObject("Line_" + lineData.Id);
        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();

        // Configuration du LineRenderer
        lineRenderer.alignment = LineAlignment.TransformZ;
        lineObject.transform.rotation = Quaternion.Euler(90, 0, 0);
        lineRenderer.positionCount = lineData.Positions.Length;
        lineRenderer.startWidth = 3f;
        lineRenderer.endWidth = 3f;
        lineRenderer.material = material;
        lineRenderer.startColor = lineData.Color;
        lineRenderer.endColor = lineData.Color;

        // Définir toutes les positions en une seule fois est plus efficace
        lineRenderer.SetPositions(lineData.Positions);

        // Parenter
        lineRenderer.transform.parent = linesParent.transform;
    }

    // Classe pour stocker les données préparées
    private class LineRenderData
    {
        public string Id { get; set; }
        public Vector3[] Positions { get; set; }
        public Color Color { get; set; }
        public bool IsMetro { get; set; }
    }

    private void DrawLine(JsonFeature<StibGeoPropertiesShapes, GeoGeometryLineString> feature)
    {
        //Instantiate Stib Stop
        GameObject lineObject = new GameObject("Line_" + feature.id);
        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();

        //Visualization
        lineRenderer.alignment = LineAlignment.TransformZ;
        lineObject.transform.rotation = Quaternion.Euler(90, 0, 0);
        lineRenderer.positionCount = feature.geometry.coordinates.Count;
        lineRenderer.startWidth = 3f;
        lineRenderer.endWidth = 3f;
        lineRenderer.material = defaultMaterialForLine;
        Color lineColor = Color.white;
        if (ColorUtility.TryParseHtmlString(feature.properties.color, out Color parsedColor))
        {
            lineColor = parsedColor;
        }
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;

        //Position and draw
        for (int i = 0; i < feature.geometry.coordinates.Count; i++)
        {
            double[] coord = feature.geometry.coordinates[i];
            Vector3 positionCesium = geoJsonCesiumSpawner.GetCesiumPosition(coord[0], coord[1]);
            float unityY = IsMetro(feature.properties.line_id) ? positionCesium.y - 400 : positionCesium.y;
            positionCesium = new Vector3(positionCesium.x, unityY, positionCesium.z);
            lineRenderer.SetPosition(i, positionCesium);
        }

        //Parent
        lineRenderer.transform.parent = linesParent.transform;
    }
    private bool IsMetro(string line_id)
    {
        if (line_id == "1" || line_id == "2" || line_id == "5" || line_id == "6")
            return true;
        else
            return false;
    }

    private void InitSceneGameObject()
    {
        stibParent = new GameObject { name = "Stib Parent" };
        linesParent = new GameObject { name = "Lines" };
        linesParent.transform.parent = stibParent.transform;
        stopsParent = new GameObject { name = "Stops" };
        stopsParent.transform.parent = stibParent.transform;
        vehiclesParent = new GameObject { name = "Vehicles" };
        vehiclesParent.transform.parent = stibParent.transform;
    }
    private void SaveAsPrefab(GameObject objectToSave)
    {
        #if UNITY_EDITOR
        // Cette partie ne fonctionne que dans l'éditeur Unity
        string savePath = "Assets/Resources/Prefabs/";

        // Créer le dossier s'il n'existe pas
        if (!System.IO.Directory.Exists(savePath))
        {
            System.IO.Directory.CreateDirectory(savePath);
        }

        // Sauvegarder le prefab
        UnityEditor.PrefabUtility.SaveAsPrefabAsset(objectToSave, savePath + prefabName + ".prefab");
        Debug.Log("StibShapes prefab sauvegardé à: " + savePath + prefabName + ".prefab");
        #else
        Debug.LogWarning("La sauvegarde des prefabs ne fonctionne que dans l'éditeur Unity.");
        #endif
    }
    public List<JsonFeature<StibGeoPropertiesStops, GeoGeometryPoint>> FindTrajectory(string line_id, int direction, int point_id)
    {

        List<JsonFeature<StibGeoPropertiesStops, GeoGeometryPoint>> relevantStopsList = geoJsonDataStops.features.FindAll(s => s.properties.route_short_name == line_id && s.properties.direction == direction);

        int pointIndex = relevantStopsList.FindIndex(s => s.properties.stop_id == point_id);

        if (pointIndex != -1)
        {
            return relevantStopsList.GetRange(0, pointIndex);
        }
        else
        {
            return new List<JsonFeature<StibGeoPropertiesStops, GeoGeometryPoint>>();
        }
    }

}
