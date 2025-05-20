using UnityEngine;
using System.Collections;
using CesiumForUnity;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Xml;
using System.Globalization;

public class GeneralManager : MonoBehaviour
{
    public Cesium3DTileset tileset;
    public GameObject buildings;
    public TrafficTwinManager trafficTwinManager;
    public StibManager stibManager;
    public SncbManager sncbManager;
    public DeLijnManager deLijnManager;
   

    private WmsManager wmsManager;
    public CesiumWebMapServiceRasterOverlay wmsOverlay;

    public string urlWmsUrbis = "https://geoservices-urbis.irisnet.be/geoserver/ows";
    public string urlWmsBruMob = "https://data.mobility.brussels/geoserver/bm_public_transport/wms";

    private bool trafficIsActive = false;
    private bool stibIsActive = false;
    private bool sncbIsActive = false;
    private bool delijnIsActive = false;

    void Start()
    {
        StartCoroutine(BackgroundProcess());
        wmsManager = new WmsManager(wmsOverlay);
    }

    IEnumerator BackgroundProcess()
    {
        yield return StartCoroutine(WaitForCesiumLoad());

        MeshFilter[] meshFilters = FindObjectsOfType<MeshFilter>();
        Debug.Log($"Number of mesh : {meshFilters.Length}");
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
                    if (hit.collider != null && (hit.collider.gameObject.name.Contains("Belgium_Collision_")))
                    {
                        
                        successfulHits++;
                        Debug.DrawRay(origin, direction * hit.distance, Color.green, 1f);
                        Debug.Log("Type de collider = : " + hit.collider);
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
        LoadAllManager();
    }

    Vector3[] GetRaycastTestPoints()
    {
        float heightOffset = 500f; // Hauteur au-dessus de la caméra pour commencer les raycasts

        // Positions basées sur la position de la caméra, mais avec une hauteur supplémentaire
        return new Vector3[]
        {
        new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y + heightOffset, Camera.main.transform.position.z),
        new Vector3(Camera.main.transform.position.x + 1000, Camera.main.transform.position.y + heightOffset, Camera.main.transform.position.z + 1000),
        new Vector3(Camera.main.transform.position.x - 1000, Camera.main.transform.position.y + heightOffset, Camera.main.transform.position.z - 1000),
        new Vector3(Camera.main.transform.position.x - 1000, Camera.main.transform.position.y + heightOffset, Camera.main.transform.position.z + 1000),
        new Vector3(Camera.main.transform.position.x + 1000, Camera.main.transform.position.y + heightOffset, Camera.main.transform.position.z - 1000)
        };
    }

    private void LoadAllManager()
    {
        trafficTwinManager.InitTrafficTwin();
        stibManager.InitStib();
        sncbManager.InitSncb();
        deLijnManager.InitDeLijn();

        SetActiveByName("Canvas Simulation", false);
        SetActiveByName("Canvas Vehicle", false);
        SetActiveByName("Traffic Twin parents", false);

        SetActiveByName("Stib Parent", false);
        SetActiveByName("CanvasStib", false);

        SetActiveByName("Sncb Parent", false);
        SetActiveByName("CanvasStib", false);

        SetActiveByName("DeLijn Parent", false);
        SetActiveByName("CanvasStib", false);

        buildings.SetActive(false);


    }
    private void SetActiveByName(string objectName, bool isActive)
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


    public void ShowTrafficManager()
    {
        trafficIsActive = true;
        SetActiveByName("Canvas Simulation", true);
        SetActiveByName("Canvas Vehicle", true);
        SetActiveByName("Traffic Twin parents", true);
        UpdateOverlayState();
    }
    public void HideTrafficManager()
    {
        trafficIsActive = false;
        SetActiveByName("Canvas Simulation", false);
        SetActiveByName("Canvas Vehicle", false);
        SetActiveByName("Traffic Twin parents", false);
        UpdateOverlayState();
    }

    public void ShowStibManager()
    {
        stibIsActive = true;
        SetActiveByName("Stib Parent", true);
        SetActiveByName("CanvasStib", true);
        UpdateOverlayState();
    }
    public void HideStibManager()
    {
        stibIsActive = false;
        SetActiveByName("Stib Parent", false);
        SetActiveByName("CanvasStib", false);
        UpdateOverlayState();
    }
    public void ShowSncbManager()
    {
        sncbIsActive = true;
        SetActiveByName("Sncb Parent", true);
        SetActiveByName("CanvasStib", true);
        UpdateOverlayState();

    }
    public void HideSncbManager()
    {
        sncbIsActive = false;
        SetActiveByName("Sncb Parent", false);
        SetActiveByName("CanvasStib", false);
        UpdateOverlayState();
    }
    public void ShowDeLijnManager()
    {
        delijnIsActive = true;
        SetActiveByName("DeLijn Parent", true);
        SetActiveByName("CanvasStib", true);
        UpdateOverlayState();

    }
    public void HideDeLijnManager()
    {
        delijnIsActive = false;
        SetActiveByName("DeLijn Parent", false);
        SetActiveByName("CanvasStib", false);
        UpdateOverlayState();
    }

    public void ShowBuildings()
    {
        buildings.SetActive(true);
        UpdateOverlayState();
    }
    public void HideBuildings()
    {
        buildings.SetActive(false);
        UpdateOverlayState();
    }

    private void UpdateOverlayState()
    {
        // Cas 1 & 2 : Aucun transport en commun => fond de carte Urbis
        if (!stibIsActive && !sncbIsActive && !delijnIsActive)
        {
            wmsManager.SwitchWMS(urlWmsUrbis, "BaseMaps:UrbISFrenchLabeledColor");
            return;
        }

        // Cas 3 : Un ou plusieurs transports activés => fond BruMob
        string newLayer = "";
        List<string> layers = new List<string>();

        if (stibIsActive)
            layers.Add("stib_lines");

        if (sncbIsActive)
            layers.Add("infrabel_lines");

        if (delijnIsActive)
            layers.Add("delijn_lines");

        newLayer = string.Join(",", layers);
        wmsManager.SwitchWMS(urlWmsBruMob, newLayer);
    }


}


