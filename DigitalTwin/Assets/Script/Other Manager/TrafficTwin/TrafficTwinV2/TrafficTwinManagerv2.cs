using UnityEngine;
using System.Collections;
using CesiumForUnity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Xml;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Linq;

[System.Serializable]
public class Vehicle
{
    public float time;
    public string vehicleId;
    public List<Point> path; 
}

[System.Serializable]
public class Point
{
    public Vector3 unityPos ;
    public float speed;
}



public class TrafficTwinManagerv2 : MonoBehaviour
{
    //Cesium 
    [Header("Cesium")]
    public CesiumGeoreference georeference;
    private GeoJsonCesiumSpawner geoJsonCesiumSpawner = new GeoJsonCesiumSpawner();
    public Cesium3DTileset tileset;

    private GameObject prefabVehicle;

    public static TrafficTwinManagerv2 Instance;

    private GameObject trafficTwinParent;
    private GameObject vehicleParent;


    private List<Vehicle> allVehicles = new List<Vehicle>();
    private List<Vehicle> allVehiclesSave = new List<Vehicle>();

    private ApiClientv2 apiClient;
   [SerializeField] private string api_url = "";



    public void InitTrafficTwinv2()
    {
        Debug.Log($"[InitTrafficTwinv2] Starting initialization on {gameObject.name}");

        // CRITQUE : S'assurer que le GameObject ne sera pas détruit
        if (Instance == null)
        {
            Instance = this;
            // APPLIQUER DontDestroyOnLoad IMMÉDIATEMENT
            DontDestroyOnLoad(gameObject);
            Debug.Log($"[InitTrafficTwinv2] Applied DontDestroyOnLoad to {gameObject.name}");
        }

        // Vérifier que les dépendances existent
        if (georeference == null)
        {
            Debug.LogError("CesiumGeoreference is null! Cannot initialize.");
            return;
        }

        try
        {
            geoJsonCesiumSpawner.Initialize(georeference);
            apiClient = new ApiClientv2();
            InitSceneGameObject();

            // Démarrer la simulation APRÈS avoir sécurisé le GameObject
            Debug.Log("[InitTrafficTwinv2] About to start simulation...");
            StartSimulation();
            Debug.Log("[InitTrafficTwinv2] Initialization completed");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in InitTrafficTwinv2: {e.Message}\n{e.StackTrace}");
        }
    }
    //INIT
    private void InitSceneGameObject()
    {
        trafficTwinParent = new GameObject { name = "Traffic Twin v2 parents" };

        vehicleParent = new GameObject { name = "Vehicle parents v2" };
        vehicleParent.transform.parent = trafficTwinParent.transform;
    }

    public void LoadVehiclesFromSave ()
    {
        allVehicles = new List<Vehicle>(allVehiclesSave);
        Debug.Log(allVehicles.Count());

    }

    private void StartSimulation()
    {
        prefabVehicle = Resources.Load<GameObject>("Prefab/Jeep2");
        LoadAllData();

        if (SimulationManager.Instance != null)
        {
            
            SimulationManager.Instance.OnSimulationTimeUpdated += CheckForSpawns;

        }
        else
        {
            Debug.LogError("SimulationManager instance not found. Make sure it exists in the scene.");
        }
    }

    public void refreshAllData()
    {
        LoadAllData();
    }



    private void LoadAllData()
    {
        
        StartCoroutine(apiClient.GetApiData<JObject>(
            api_url, (obj) =>
            {
                Debug.Log("TEST3");
                if (obj?["trips"] == null)
                {
                    Debug.LogWarning("Pas de trips dans le JSON reçu");
                    return;
                }
                allVehiclesSave.Clear();

                foreach (var vehicle in obj["trips"])
                {
                    List<Point> vecPath = new List<Point>();
                    foreach (var point in vehicle["path"])
                    {
                        float lon = point[0].Value<float>();
                        float lat = point[1].Value<float>();
                        Vector3 position = geoJsonCesiumSpawner.GetCesiumPosition(lon, lat) + new Vector3(0f, 2.2f, 0f);
                        vecPath.Add(new Point
                        {
                            speed = point[3].Value<float>(),
                            unityPos = position
                        });
                    }
                    Vehicle vec = new Vehicle
                    {
                        vehicleId = vehicle["id"].Value<string>(),
                        time = vehicle["path"][0][2].Value<float>(),
                        path = vecPath
                    };
                    allVehiclesSave.Add(vec);
                }
                LoadVehiclesFromSave();
            }
            , ApiDataType.Json
        ));


    }



    private void CheckForSpawns(float currentSimTime)
    {
        if (SimulationManager.Instance.CurrentSimulation == SimulationType.simulationv2)
        {
            if (currentSimTime < SimulationManager.Instance.simulationStartTime) return;  // Ignore si avant le temps de simulation
            var vehiclesToSpawn = allVehicles
            .Where(v => v.time <= currentSimTime && v.time >= SimulationManager.Instance.simulationStartTime)
            .ToList();

            foreach (var vehicle in vehiclesToSpawn)
            {
                CreateMarkeurVecPos(vehicle);
                allVehicles.Remove(vehicle);
            }
        }

    }
    private GameObject CreateMarkeurVecPos(Vehicle vehicle)
    {

        //Instantiate Stib Stop
        GameObject vehicleGameObject = Instantiate(prefabVehicle);

        vehicleGameObject.name = vehicle.vehicleId;
        vehicleGameObject.transform.localScale = new Vector3(1f, 1f, 1f);

        vehicleGameObject.transform.position = new Vector3(
            vehicle.path[0].unityPos.x,
            vehicle.path[0].unityPos.y,
            vehicle.path[0].unityPos.z
        );

        SphereCollider sphereCollider = vehicleGameObject.AddComponent<SphereCollider>();
        sphereCollider.radius = 2f;

        vehicleGameObject.transform.parent = vehicleParent.transform;

        

        VehicleMoverv2 controller = vehicleGameObject.AddComponent<VehicleMoverv2>();

        if (controller != null)
        {
            controller.Init(vehicle.vehicleId, vehicle.path);
        }

        return vehicleGameObject;

    }
}
