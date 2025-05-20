using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using System.Xml;
using System.Linq;
using System.Xml.Serialization;
using System.Globalization;
using CesiumForUnity;
using System.Threading.Tasks;

public class TrafficTwinManager2 : MonoBehaviour
{
    public static TrafficTwinManager2 Instance;

    public Cesium3DTileset tileset;

    private GameObject prefabVehicle;

    [Header("Reference")]
    [SerializeField] private GameObject unityReferencePoint;

    [Header("Shapes file Location")]
    [SerializeField] private string shapesPath_0_3600_wout_rw = "";
    [SerializeField] private string shapesPath_3600_7200_wout_rw = "";
    [SerializeField] private string shapesPath_0_3600_w_rw = "";
    [SerializeField] private string shapesPath_3600_7200_w_rw = "";
    [SerializeField] private string shapesPath_0_3600_diff = "";
    [SerializeField] private string shapesPath_3600_7200_diff= "";

    private JsonCollection<JsonFeature<EdgesGeoJsonProperties, GeoGeometryLineString>> geoJsonShapes_0_3600_diff;
    private JsonCollection<JsonFeature<EdgesGeoJsonProperties, GeoGeometryLineString>> geoJsonShapes_3600_7200_diff;
    private JsonCollection<JsonFeature<EdgesGeoJsonProperties, GeoGeometryLineString>> geoJsonShapes_0_3600_wrw;
    private JsonCollection<JsonFeature<EdgesGeoJsonProperties, GeoGeometryLineString>> geoJsonShapes_3600_7200_wrw;
    private JsonCollection<JsonFeature<EdgesGeoJsonProperties, GeoGeometryLineString>> geoJsonShapes_0_3600_woutrw;
    private JsonCollection<JsonFeature<EdgesGeoJsonProperties, GeoGeometryLineString>> geoJsonShapes_3600_7200_woutrw;


    [Header("XML file Location")]
    [SerializeField] private string xmlFileRoutes = "";
    [SerializeField] private string xmlFileTripInfoWoutRoadWorks = "";
    private List<TripInfo> allVehiclesTripinfo = new List<TripInfo>();
    private List<VehicleRoute> allVehiclesRoutes = new List<VehicleRoute>();

    private List<VehicleRoute> allVehicles = new List<VehicleRoute>();
    private Dictionary<string, List<Vector3>> edgeUnityCoordinates = new Dictionary<string, List<Vector3>>();


    private GPSConverter gpsConverter = new GPSConverter();
    GeoJsonCesiumSpawner geoJsonCesiumSpawner = new GeoJsonCesiumSpawner();
   

    public Material defaultMaterialForLine;
    public Material materialForLine;


    private GameObject linesParent_density_0_3600_wrw;
    private GameObject linesParent_density_3600_7200_wrw;
    private GameObject linesParent_density_0_3600_woutrw;
    private GameObject linesParent_density_3600_7200_woutrw;
    private GameObject linesParent_speed_0_3600_wrw;
    private GameObject linesParent_speed_3600_7200_wrw;
    private GameObject linesParent_speed_0_3600_woutrw;
    private GameObject linesParent_speed_3600_7200_woutrw;
    private GameObject meshParent;

    private GameObject vehicleParent;

    public CesiumGeoreference georeference;

    private void Awake()
    {
        
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);  // Empêche la destruction de l'objet lors du changement de scène
        }
        else
        {
            Destroy(gameObject);  // Si une instance existe déjà, détruit le nouvel objet
        }

        geoJsonCesiumSpawner.Initialize(georeference);

        //Instantiate Parent to store segments, stops and vehicles
        linesParent_density_0_3600_wrw = new GameObject { name = "Lines Density 0 to 3600 with roads works" };
        linesParent_density_0_3600_wrw.SetActive(false);

        linesParent_density_3600_7200_wrw = new GameObject { name = "Lines Density 3600 to 7200 with roads works" };
        linesParent_density_3600_7200_wrw.SetActive(false);

        linesParent_density_0_3600_woutrw = new GameObject { name = "Lines Density 0 to 3600 without roads works" };
        linesParent_density_0_3600_woutrw.SetActive(false);

        linesParent_density_3600_7200_woutrw = new GameObject { name = "Lines Density 3600 to 7200 without roads works" };
        linesParent_density_3600_7200_woutrw.SetActive(false);

        linesParent_speed_0_3600_wrw = new GameObject { name = "Lines Speed 0 to 3600 with roads works" };
        linesParent_speed_0_3600_wrw.SetActive(false);

        linesParent_speed_3600_7200_wrw = new GameObject { name = "Lines Speed 3600 to 7200 with roads works" };
        linesParent_speed_3600_7200_wrw.SetActive(false);

        linesParent_speed_0_3600_woutrw = new GameObject { name = "Lines Speed 0 to 3600 without roads works" };
        linesParent_speed_0_3600_woutrw.SetActive(false);

        linesParent_speed_3600_7200_woutrw = new GameObject { name = "Lines Speed 3600 to 7200 without roads works" };
        linesParent_speed_3600_7200_woutrw.SetActive(false);


        meshParent = new GameObject { name = "Mesh parents" };
        vehicleParent = new GameObject { name = "Vehicle parents" };
    }


    void Start()
    {
        if (tileset == null)
        {
            Debug.LogError("Aucun CesiumTileset trouvé!");
            return;
        }

        StartCoroutine(WaitForCesiumLoad());
    }

    IEnumerator WaitForCesiumLoad()
    {
        if (tileset == null)
        {
            Debug.LogError("Le tileset est null dans WaitForCesiumLoad");
            yield break; // Sortez de la coroutine
        }

        Debug.Log("Début du chargement : " + tileset.ComputeLoadProgress());

        // Attendre que le chargement soit terminé (quand ComputeLoadProgress = 0)
        while (tileset != null && tileset.ComputeLoadProgress() > 0.001f) // Utiliser une petite valeur pour gérer les imprécisions de virgule flottante
        {
            // Optionnel : Afficher la progression du chargement
            Debug.Log("Progression : " + (1 - tileset.ComputeLoadProgress()) * 100 + "%");
            yield return null;
        }

        yield return StartCoroutine(VerifyMeshesWithRaycasts());
        Debug.Log("Meshes vérifiés, démarrage de la simulation");
        StartSim();
        Debug.Log("Chargement terminé");
    }

    IEnumerator VerifyMeshesWithRaycasts()
    {
        // Obtention des points de test
        Vector3[] raycastOrigins = GetRaycastTestPoints();
        int consecutiveSuccessfulChecks = 0;
        int requiredSuccessfulChecks = 3;

        while (consecutiveSuccessfulChecks < requiredSuccessfulChecks)
        {
            int successfulHits = 0;
            foreach (Vector3 origin in raycastOrigins)
            {
                Vector3 direction = Vector3.down;
                RaycastHit hit;

                // Effectuer le raycast
                if (Physics.Raycast(origin, direction, out hit, 20000f))
                {
                    // Vérifier si le hit est un objet Cesium (ou un autre critère de votre choix)
                    if (hit.collider != null && (hit.collider.gameObject.name.Contains("Cesium") ||
                                               hit.transform.root.name.Contains("Cesium")))
                    {
                        successfulHits++;
                        Debug.DrawRay(origin, direction * hit.distance, Color.green, 1f);
                        Debug.Log($"Hit sur {hit.collider.gameObject.name} à une distance de {hit.distance}");
                    }
                    else
                    {
                        Debug.DrawRay(origin, direction * hit.distance, Color.yellow, 1f);
                        Debug.Log($"Hit sur un objet non-Cesium: {hit.collider.gameObject.name}");
                    }
                }
                else
                {
                    Debug.DrawRay(origin, direction * 20000f, Color.red, 1f);
                    Debug.Log("Aucun hit détecté");
                }
            }

            float hitPercentage = (float)successfulHits / raycastOrigins.Length;
            Debug.Log($"Test de raycasts: {successfulHits}/{raycastOrigins.Length} hits ({hitPercentage * 100:F1}%) sur les meshes Cesium");

            if (hitPercentage > 0.8f)
            {
                consecutiveSuccessfulChecks++;
                Debug.Log($"Vérification réussie {consecutiveSuccessfulChecks}/{requiredSuccessfulChecks}");
            }
            else
            {
                consecutiveSuccessfulChecks = 0;
                Debug.Log("Meshes encore en chargement, nouvelle tentative dans 1 seconde");
            }

            yield return new WaitForSeconds(1f);
        }
    }

    Vector3[] GetRaycastTestPoints()
    {
        float heightOffset = 500f; // Hauteur au-dessus de la caméra pour commencer les raycasts

        // Positions basées sur la position de la caméra, mais avec une hauteur supplémentaire
        return new Vector3[]
        {
        new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y + heightOffset, Camera.main.transform.position.z),
        new Vector3(Camera.main.transform.position.x + 100, Camera.main.transform.position.y + heightOffset, Camera.main.transform.position.z + 100),
        new Vector3(Camera.main.transform.position.x - 100, Camera.main.transform.position.y + heightOffset, Camera.main.transform.position.z - 100),
        new Vector3(Camera.main.transform.position.x - 100, Camera.main.transform.position.y + heightOffset, Camera.main.transform.position.z + 100),
        new Vector3(Camera.main.transform.position.x + 100, Camera.main.transform.position.y + heightOffset, Camera.main.transform.position.z - 100)
        };
    }


    public void StartSim()
    {

        prefabVehicle = Resources.Load<GameObject>("Prefab/Jeep2");

        TextAsset jsonContentAsset = Resources.Load<TextAsset>(shapesPath_0_3600_diff);
        geoJsonShapes_0_3600_diff = LoadGeoJsonData<JsonFeature<EdgesGeoJsonProperties, GeoGeometryLineString>>(jsonContentAsset);
        
        if (geoJsonShapes_0_3600_diff != null)
        {
            foreach (var feature in geoJsonShapes_0_3600_diff.features)
            {
                DrawRoadMesh(feature);
            }
        }

        /*LoadAllEdgesData(shapesPath_0_3600_w_rw, geoJsonShapes_0_3600_wrw, linesParent_density_0_3600_wrw, linesParent_speed_0_3600_wrw);
        LoadAllEdgesData(shapesPath_3600_7200_w_rw, geoJsonShapes_3600_7200_wrw, linesParent_density_3600_7200_wrw, linesParent_speed_3600_7200_wrw);
        LoadAllEdgesData(shapesPath_0_3600_wout_rw, geoJsonShapes_0_3600_woutrw, linesParent_density_0_3600_woutrw, linesParent_speed_0_3600_woutrw);
        LoadAllEdgesData(shapesPath_3600_7200_wout_rw, geoJsonShapes_3600_7200_woutrw, linesParent_density_3600_7200_woutrw, linesParent_speed_3600_7200_woutrw);
        */
        StartSim2();
        
        LoadRoutesFromXML();
        InitAllVehicles();
        LoadTripInfoFromXML();
        

        if (SimulationManager.Instance != null)
        {
            SimulationManager.Instance.OnSimulationTimeUpdated += CheckForSpawns;
        }
        else
        {
            Debug.LogError("SimulationManager instance not found. Make sure it exists in the scene.");
        }

    }

    private async void StartSim2()
    {
        TextAsset jsonContentAsset = Resources.Load<TextAsset>(shapesPath_0_3600_w_rw);
        string jsonContent = jsonContentAsset.text;
        await LoadAllEdgesDataAsync(jsonContent, geoJsonShapes_0_3600_wrw, linesParent_density_0_3600_wrw, linesParent_speed_0_3600_wrw);

        jsonContentAsset = Resources.Load<TextAsset>(shapesPath_3600_7200_w_rw);
        jsonContent = jsonContentAsset.text;
        await LoadAllEdgesDataAsync(jsonContent, geoJsonShapes_3600_7200_wrw, linesParent_density_3600_7200_wrw, linesParent_speed_3600_7200_wrw);

        jsonContentAsset = Resources.Load<TextAsset>(shapesPath_0_3600_wout_rw);
        jsonContent = jsonContentAsset.text;
        await LoadAllEdgesDataAsync(jsonContent, geoJsonShapes_0_3600_woutrw, linesParent_density_0_3600_woutrw, linesParent_speed_0_3600_woutrw);

        jsonContentAsset = Resources.Load<TextAsset>(shapesPath_3600_7200_wout_rw);
        jsonContent = jsonContentAsset.text;
        await LoadAllEdgesDataAsync(jsonContent, geoJsonShapes_3600_7200_woutrw, linesParent_density_3600_7200_woutrw, linesParent_speed_3600_7200_woutrw);

    }

    public void InitAllVehicles()
    {
        allVehicles = new List<VehicleRoute>(allVehiclesRoutes);
    }

    //DATA

    public async Task LoadAllEdgesDataAsync(string jsonContent, JsonCollection<JsonFeature<EdgesGeoJsonProperties, GeoGeometryLineString>> data, GameObject linesParentsDensity, GameObject linesParentsSpeed)
    {
        // 1. Charger le fichier JSON et le parser en arrière-plan
        var parsedData = await Task.Run(() =>
            LoadGeoJsonDataAsync<JsonFeature<EdgesGeoJsonProperties, GeoGeometryLineString>>(jsonContent)
        );

        if (parsedData != null)
        {
            // 2. Boucler sur chaque feature sur le thread principal (car on crée des GameObject Unity)
            foreach (var feature in parsedData.features)
            {
                DrawLine(feature, linesParentsDensity, is_density: true);
                DrawLine(feature, linesParentsSpeed, is_density: false);
                await Task.Yield(); // Pour éviter de bloquer l'UI frame par frame
            }
        }
    }

    private void LoadAllEdgesData(string path, JsonCollection<JsonFeature<EdgesGeoJsonProperties, GeoGeometryLineString>> data, GameObject linesParentsDensity, GameObject linesParentsSpeed)
    {
        TextAsset jsonContentAsset = Resources.Load<TextAsset>(path);
        data = LoadGeoJsonData<JsonFeature<EdgesGeoJsonProperties, GeoGeometryLineString>>(jsonContentAsset);
        if (data != null)
        {
            
            foreach (var feature in data.features)
            {
                DrawLine(feature, linesParentsDensity, is_density: true);
                DrawLine(feature, linesParentsSpeed, is_density: false);
            }
        }
    }

    private void LoadRoutesFromXML()
    {
        XmlDocument xmlDoc = new XmlDocument();
        TextAsset xmlFileRoute = Resources.Load<TextAsset>(xmlFileRoutes);
        xmlDoc.LoadXml(xmlFileRoute.text);

        XmlNodeList vehicleNodes = xmlDoc.GetElementsByTagName("vehicle");

        foreach (XmlNode vehicle in vehicleNodes)
        {
            VehicleRoute vec = new VehicleRoute();
            vec.id = vehicle.Attributes["id"].Value;
            string departString = vehicle.Attributes["depart"].Value;
            float.TryParse(departString, NumberStyles.Float, CultureInfo.InvariantCulture, out float departValue);
            vec.depart = departValue;

            XmlNode routeNode = vehicle.SelectSingleNode("route");
            string edgesAttr = routeNode.Attributes["edges"].Value;
            vec.edges = new List<string>(edgesAttr.Split(' '));
            allVehiclesRoutes.Add(vec);
        }
    }

    private void LoadTripInfoFromXML()
    {
        XmlDocument xmlDoc = new XmlDocument();
        TextAsset xmlFileTripInfoWoutRoadWork = Resources.Load<TextAsset>(xmlFileTripInfoWoutRoadWorks);
        xmlDoc.LoadXml(xmlFileTripInfoWoutRoadWork.text);

        XmlNodeList tripInfoNodes = xmlDoc.GetElementsByTagName("tripinfo");

        foreach (XmlNode trip in tripInfoNodes)
        {
            TripInfo tripInfo = new TripInfo();


            tripInfo.id = trip.Attributes["id"].Value;

            float.TryParse(trip.Attributes["depart"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out tripInfo.depart);
            tripInfo.departLane = trip.Attributes["departLane"].Value;
            float.TryParse(trip.Attributes["departPos"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out tripInfo.departPos);
            float.TryParse(trip.Attributes["departSpeed"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out tripInfo.departSpeed);
            float.TryParse(trip.Attributes["departDelay"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out tripInfo.departDelay);
            float.TryParse(trip.Attributes["arrival"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out tripInfo.arrival);
            tripInfo.arrivalLane = trip.Attributes["arrivalLane"].Value;
            float.TryParse(trip.Attributes["arrivalPos"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out tripInfo.arrivalPos);
            float.TryParse(trip.Attributes["arrivalSpeed"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out tripInfo.arrivalSpeed);
            float.TryParse(trip.Attributes["duration"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out tripInfo.duration);
            float.TryParse(trip.Attributes["routeLength"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out tripInfo.routeLength);
            float.TryParse(trip.Attributes["waitingTime"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out tripInfo.waitingTime);

            // int.Parse remplacé par TryParse aussi pour sécuriser
            int.TryParse(trip.Attributes["waitingCount"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out tripInfo.waitingCount);
            float.TryParse(trip.Attributes["stopTime"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out tripInfo.stopTime);
            float.TryParse(trip.Attributes["timeLoss"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out tripInfo.timeLoss);
            int.TryParse(trip.Attributes["rerouteNo"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out tripInfo.rerouteNo);


            // Stocker dans ta liste
            allVehiclesTripinfo.Add(tripInfo);
        }
    }

    private JsonCollection<TFeature> LoadGeoJsonData<TFeature>(TextAsset jsonContentAsset)
    {

        if (jsonContentAsset == null)
        {
            Debug.LogError($"Fichier GeoJSON introuvable dans Resources: {jsonContentAsset}");
            return null;
        }

        string jsonContent = jsonContentAsset.text;

        return JsonConvert.DeserializeObject<JsonCollection<TFeature>>(jsonContent);


    }

    private JsonCollection<TFeature> LoadGeoJsonDataAsync<TFeature>(string jsonContent)
    {

        if (jsonContent == null)
        {
            Debug.LogError($"Fichier GeoJSON introuvable dans Resources: {jsonContent}");
            return null;
        }

        return JsonConvert.DeserializeObject<JsonCollection<TFeature>>(jsonContent);
    }

    //VISUALIZATION

    private GameObject CreateMarkeurVecPos(VehicleRoute vehicle)
    {

        TripInfo vehicleTripInfo = GetTripInfoById(vehicle.id);
        //Instantiate Stib Stop
        GameObject vehicleGameObject = Instantiate(prefabVehicle);
    
        vehicleGameObject.name = vehicle.id;
        vehicleGameObject.transform.localScale = new Vector3(2f, 2f, 2f);

        Vector3 position = vehicleGameObject.transform.position;
        position.y = 1.6f;  // Définit la hauteur souhaitée
        vehicleGameObject.transform.position = position;

        VehicleMover controller = vehicleGameObject.AddComponent<VehicleMover>();

        if (controller != null)
        {
            controller.Init(vehicle.id, generatePath(vehicle), GetDurationById(vehicle.id));
        }

        SphereCollider sphereCollider = vehicleGameObject.AddComponent<SphereCollider>();
        sphereCollider.radius = 2f;
        ShowInfoOnHover hoverScript = vehicleGameObject.AddComponent<ShowInfoOnHover>();
        hoverScript.Initialize();
        ShowInfoOnClick clickScript = vehicleGameObject.AddComponent<ShowInfoOnClick>();
        clickScript.Initialize(vehicleTripInfo);

        vehicleGameObject.transform.parent = vehicleParent.transform;

        return vehicleGameObject;
    }

    private void DrawRoadMesh(JsonFeature<EdgesGeoJsonProperties, GeoGeometryLineString> feature)
    {
        List<Vector2> uvs = new List<Vector2>();

        GameObject roadObject = new GameObject("RoadMesh_" + feature.properties.id);
        MeshFilter meshFilter = roadObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = roadObject.AddComponent<MeshRenderer>();
        meshRenderer.material = defaultMaterialForLine; // Tu peux remplacer par un matériau routier

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        float roadWidth = 3f;

        List<Vector3> points = new List<Vector3>();
        foreach (var coord in feature.geometry.coordinates)
        {
            //Vector3 point = gpsConverter.GPStoUCS(unityReferencePoint.transform.position, coord[1], coord[0]);
            Vector3 point = geoJsonCesiumSpawner.GetCesiumPosition(coord[0], coord[1]);
            points.Add(point);
        }

        if (points.Count < 2)
        {
            Destroy(roadObject);
            return;
        }

        for (int i = 0; i < points.Count - 1; i++)
        {

            Vector3 p1 = points[i];
            Vector3 p2 = points[i + 1];

            Vector3 direction = (p2 - p1).normalized;
            Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized * roadWidth * 0.5f;

            // Crée 4 coins du rectangle
            Vector3 v0 = p1 - perpendicular;
            Vector3 v1 = p1 + perpendicular;
            Vector3 v2 = p2 - perpendicular;
            Vector3 v3 = p2 + perpendicular;

            // Longueur du segment
            float segmentLength = Vector3.Distance(p1, p2);
            float cumulativeLength = uvs.Count > 0 ? uvs[uvs.Count - 1].y : 1f;

            // Ajouter UVs : (u,v) => (0,v) pour un côté, (1,v) pour l'autre
            uvs.Add(new Vector2(0, cumulativeLength));
            uvs.Add(new Vector2(1, cumulativeLength));
            uvs.Add(new Vector2(0, cumulativeLength + segmentLength));
            uvs.Add(new Vector2(1, cumulativeLength + segmentLength));

            int vertIndex = vertices.Count;

            // Ajoute les vertices
            vertices.Add(v0);
            vertices.Add(v1);
            vertices.Add(v2);
            vertices.Add(v3);

            // Triangle 1
            triangles.Add(vertIndex + 0);
            triangles.Add(vertIndex + 1);
            triangles.Add(vertIndex + 2);

            // Triangle 2
            triangles.Add(vertIndex + 2);
            triangles.Add(vertIndex + 1);
            triangles.Add(vertIndex + 3);
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();


        meshFilter.mesh = mesh;
        roadObject.transform.parent = meshParent.transform;

        // Stocker les points centraux si nécessaire
        edgeUnityCoordinates[feature.properties.id] = points;
    }


    /*void SpawnLine(List<double[]> coords)
    {
        GameObject lineObj = new GameObject("GeoJSON_Line");
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = lineMaterial;
        lr.positionCount = coords.Count;
        lr.widthMultiplier = 5f;

        for (int i = 0; i < coords.Count; i++)
        {
            double3 ecef = Wgs84ToEcef(coords[i][0], coords[i][1], coords[i].Length > 2 ? coords[i][2] : 0);
            double3 unityPosition = georeference.TransformEarthCenteredEarthFixedPositionToUnity(ecef);
            Vector3 unityVectorPosition = new Vector3((float)unityPosition.x, GetYPositionOnTerrain(coords[i][0], coords[i][1]) + 2f, (float)unityPosition.z);
            lr.SetPosition(i, unityVectorPosition);
        }

        lineObj.transform.parent = transform;
    }*/

    private void DrawLine(JsonFeature<EdgesGeoJsonProperties, GeoGeometryLineString> feature, GameObject linesParent, bool is_density)
    {
        // Instantiate Line Object
        GameObject lineObject = new GameObject("Line_" + feature.properties.id);
        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();

        // Largeur constante pour toutes les lignes
        float lineWidth = 6f;

        // Configuration de base de la ligne
        lineRenderer.positionCount = feature.geometry.coordinates.Count;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

       
        float factor = 0f;

        if (is_density)
        {
            // Calculer le facteur de densité pour la couleur
            factor = Mathf.Clamp(feature.properties.density, 0f, 10f) / 10f;
        }
        else
        {
            factor = Mathf.Clamp(feature.properties.speed, 0f, 10f) / 10f;
        }

        // Couleur basée sur la densité (vert = faible densité, rouge = haute densité)
        Color lineColor = Color.Lerp(Color.green, Color.red, factor);

        Material lineMaterial = new Material(materialForLine);
        // Appliquer la couleur au matériau
        lineMaterial.color = lineColor;
        // Assigner le matériau à la ligne
        lineRenderer.material = lineMaterial;
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;

        List<Vector3> validPositions = new List<Vector3>();
        foreach (var coord in feature.geometry.coordinates)
        {
            double latitude = coord[1];
            double longitude = coord[0];
            Vector3 position = geoJsonCesiumSpawner.GetCesiumPosition(longitude,latitude);
            //Vector3 position = gpsConverter.GPStoUCS(unityReferencePoint.transform.position, latitude, longitude);
            validPositions.Add(position);
        }

        // Vérifie si on a des points valides à dessiner
        if (validPositions.Count > 1)
        {
            lineRenderer.positionCount = validPositions.Count;
            lineRenderer.SetPositions(validPositions.ToArray());
            lineRenderer.transform.parent = linesParent.transform;
            edgeUnityCoordinates[feature.properties.id] = validPositions;
        }
        else
        {
            // Supprime l'objet si aucune position valide
            Destroy(lineObject);
        }
    }


    //UTILS


    public void SetActiveByName(string objectName, bool isActive)
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        GameObject obj = System.Array.Find(allObjects, obj => obj.name == objectName);

        // Si le GameObject est trouvé
        if (obj != null)
        {
            // Active ou désactive le GameObject en fonction du paramètre isActive
            obj.SetActive(isActive);
        }
        else
        {
            // Affiche un message d'erreur si le GameObject n'est pas trouvé
            Debug.LogWarning("GameObject avec le nom '" + objectName + "' n'a pas été trouvé !");
        }
    }

    public void DestroyAllChildrenImmediate(string objectName)
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        GameObject parent = System.Array.Find(allObjects, obj => obj.name == objectName);

        // Parcourir tous les enfants en commençant par la fin
        while (parent.transform.childCount > 0)
        {
            DestroyImmediate(parent.transform.GetChild(0).gameObject);
        }
    }

    public float GetDurationById(string vehicleId)
    {
        foreach (TripInfo trip in allVehiclesTripinfo)
        {
            if (trip.id == vehicleId)
            {
                return trip.duration;
            }
        }

        // Retourne une valeur par défaut si l'ID n'est pas trouvé
        Debug.LogWarning($"Véhicule avec ID {vehicleId} non trouvé. Utilisation d'une durée par défaut.");
        return 1.0f; // Vous pouvez changer cette valeur par défaut selon vos besoins
    }

    public TripInfo GetTripInfoById(string vehicleId)
    {
        foreach (TripInfo trip in allVehiclesTripinfo)
        {
            if (trip.id == vehicleId)
            {
                return trip;
            }
        }

        // Retourne une valeur par défaut si l'ID n'est pas trouvé
        Debug.LogWarning($"Véhicule avec ID {vehicleId} non trouvé. Utilisation d'une durée par défaut.");
        return null; // Vous pouvez changer cette valeur par défaut selon vos besoins
    }

    private List<Vector3> generatePath(VehicleRoute vehicle)
    {
        List<Vector3> path = new List<Vector3>();

        foreach (var edge in vehicle.edges)
        {
            foreach (var unitPos in edgeUnityCoordinates[edge])
            {
                Vector3 modifiedPos = new Vector3(unitPos.x,unitPos.y + 3.2f, unitPos.z);
                path.Add(modifiedPos);
            }
        }

        return path;
    }

    private void CheckForSpawns(float currentSimTime)
    {
        if (currentSimTime < SimulationManager.Instance.simulationStartTime) return;  // Ignore si avant le temps de simulation

        var vehiclesToSpawn = allVehicles
            .Where(v => v.depart <= currentSimTime && v.depart >= SimulationManager.Instance.simulationStartTime)
            .ToList();

        // Instancier les véhicules qui répondent à cette condition
        foreach (var vehicle in vehiclesToSpawn)
        {
            CreateMarkeurVecPos(vehicle);
            allVehicles.Remove(vehicle);
        }
    }


}
