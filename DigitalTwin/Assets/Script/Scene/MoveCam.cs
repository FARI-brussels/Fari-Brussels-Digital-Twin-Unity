using UnityEngine;
using Unity.Mathematics;
using CesiumForUnity;
using System.Collections.Generic;

public class BelgiumTileScanner : MonoBehaviour
{
    public CesiumGeoreference georeference;
    public Cesium3DTileset cesiumTileset;
    public Camera cameraToMove;
    public float height = 1000f;
    public Material fallbackMaterial;

    // Configuration de la zone complète
    public double minLon = 4.19, maxLon = 4.55;
    public double minLat = 50.74, maxLat = 50.91;
    public double step = 0.01;

    // Limite de triangles par mesh (sous la limite de 2,097,152)
    public int maxTrianglesPerMesh = 1000000;

    private double currentLon, currentLat;
    private bool scanning = true;

    // Collection principale des meshs
    private List<MeshFilter> collected = new List<MeshFilter>();

    // Compteur pour nommer les objets générés
    private int meshCounter = 0;
    private bool processingBatch = false;

    void Start()
    {
        currentLon = minLon;
        currentLat = minLat;
    }

    void Update()
    {
        if (scanning)
        {
            MoveCameraAndCollect();
        }
        else if (!processingBatch && collected.Count > 0)
        {
            ProcessCurrentBatch();
        }
    }

    void MoveCameraAndCollect()
    {
        double3 ecef = Wgs84ToEcef(currentLon, currentLat, height);
        double3 unityPos = georeference.TransformEarthCenteredEarthFixedPositionToUnity(ecef);
        cameraToMove.transform.position = new Vector3((float)unityPos.x, (float)unityPos.y, (float)unityPos.z);
        cameraToMove.transform.eulerAngles = new Vector3(90f, 0f, 0f); // Regard vers le bas

        // Collecte des meshes visibles
        MeshFilter[] filters = cesiumTileset.GetComponentsInChildren<MeshFilter>();
        int triangleCount = 0;

        // Compter les triangles actuels dans la collection
        foreach (var mf in collected)
        {
            if (mf != null && mf.sharedMesh != null)
            {
                triangleCount += mf.sharedMesh.triangles.Length / 3;
            }
        }

        // Ajouter de nouveaux meshes tout en vérifiant la limite
        foreach (var mf in filters)
        {
            if (!collected.Contains(mf) && mf.sharedMesh != null)
            {
                CesiumGlobeAnchor anchor = mf.GetComponent<CesiumGlobeAnchor>();
                if (anchor != null)
                {
                    double lon = anchor.longitudeLatitudeHeight.x;
                    double lat = anchor.longitudeLatitudeHeight.y;

                    if (lon >= minLon && lon <= maxLon && lat >= minLat && lat <= maxLat)
                    {
                        int meshTriangles = mf.sharedMesh.triangles.Length / 3;

                        // Si l'ajout de ce mesh dépasserait la limite, traiter le batch actuel d'abord
                        if (triangleCount + meshTriangles > maxTrianglesPerMesh && collected.Count > 0)
                        {
                            processingBatch = true;
                            Debug.Log($"Traitement d'un batch de {collected.Count} meshes avec {triangleCount} triangles");
                            ProcessCurrentBatch();
                            triangleCount = 0;
                            collected.Clear();
                            processingBatch = false;
                        }

                        // Ajouter le mesh actuel à la collection
                        collected.Add(mf);
                        triangleCount += meshTriangles;
                    }
                }
            }
        }

        // Passage à la grille suivante
        currentLon += step;
        if (currentLon > maxLon)
        {
            currentLon = minLon;
            currentLat += step;
        }

        if (currentLat > maxLat)
        {
            scanning = false;
            Debug.Log($"Scan terminé. {collected.Count} tiles restantes à traiter dans le dernier batch.");
        }
    }

    void ProcessCurrentBatch()
    {
        if (collected.Count == 0)
        {
            Debug.LogWarning("Aucun mesh à fusionner.");
            return;
        }

        Debug.Log($"Traitement du batch {meshCounter + 1}: {collected.Count} meshes");

        List<CombineInstance> combines = new List<CombineInstance>();
        int totalTriangles = 0;

        foreach (var mf in collected)
        {
            if (mf != null && mf.sharedMesh != null)
            {
                CombineInstance ci = new CombineInstance();
                ci.mesh = mf.sharedMesh;
                ci.transform = mf.transform.localToWorldMatrix;
                combines.Add(ci);
                totalTriangles += mf.sharedMesh.triangles.Length / 3;
            }
        }

        Debug.Log($"Combinaison de {combines.Count} meshes avec {totalTriangles} triangles");

        Mesh finalMesh = new Mesh();
        finalMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        finalMesh.CombineMeshes(combines.ToArray(), true, true);

        // IMPORTANT: Sauvegarde du mesh comme un asset dans le projet
#if UNITY_EDITOR
        // Assurez-vous que le dossier existe
        if (!System.IO.Directory.Exists("Assets/GeneratedMeshes"))
        {
            System.IO.Directory.CreateDirectory("Assets/GeneratedMeshes");
        }

        // Sauvegarde du mesh comme asset
        string meshPath = $"Assets/GeneratedMeshes/Belgium_Mesh_{meshCounter}_{System.DateTime.Now.ToString("yyyyMMdd_HHmmss")}.asset";
        UnityEditor.AssetDatabase.CreateAsset(finalMesh, meshPath);
        UnityEditor.AssetDatabase.SaveAssets();

        // Récupération du mesh sauvegardé pour l'utiliser
        Mesh savedMesh = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        finalMesh = savedMesh; // Utiliser le mesh sauvegardé à la place

        Debug.Log("Mesh sauvegardé à: " + meshPath);
#endif

        // Création du GameObject pour la fusion
        GameObject bakedObject = new GameObject($"Baked_Belgium_Mesh_{meshCounter}");
        bakedObject.transform.position = Vector3.zero;

        MeshFilter mfFinal = bakedObject.AddComponent<MeshFilter>();
        mfFinal.sharedMesh = finalMesh;

        Debug.Log($"Nombre de vertices : {mfFinal.mesh.vertexCount}");
        Debug.Log($"Nombre de triangles : {mfFinal.mesh.triangles.Length / 3}");

        MeshRenderer mr = bakedObject.AddComponent<MeshRenderer>();
        mr.sharedMaterial = fallbackMaterial ?? new Material(Shader.Find("Standard"));

        MeshCollider collider = bakedObject.AddComponent<MeshCollider>();
        collider.sharedMesh = finalMesh;
        // Désactiver l'option Fast Midphase si on approche la limite
        if (finalMesh.triangles.Length / 3 > 1500000)
        {
            collider.cookingOptions &= ~UnityEngine.MeshColliderCookingOptions.UseFastMidphase;
            Debug.Log("Fast Midphase désactivé pour ce collider en raison du nombre élevé de triangles");
        }

        bakedObject.isStatic = true;

        // Création d'un mesh de collision simplifié
        Mesh collisionMesh = CreateSimplifiedCollisionMesh(finalMesh);

        // Sauvegarde du mesh de collision également
#if UNITY_EDITOR
        string collisionMeshPath = $"Assets/GeneratedMeshes/Belgium_CollisionMesh_{meshCounter}_{System.DateTime.Now.ToString("yyyyMMdd_HHmmss")}.asset";
        UnityEditor.AssetDatabase.CreateAsset(collisionMesh, collisionMeshPath);
        UnityEditor.AssetDatabase.SaveAssets();

        Mesh savedCollisionMesh = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(collisionMeshPath);
        collisionMesh = savedCollisionMesh;

        Debug.Log("Mesh de collision sauvegardé à: " + collisionMeshPath);
#endif

        // Création d'un nouveau MeshCollider propre pour la détection des collisions
        GameObject collisionObject = new GameObject($"Collision_Belgium_Mesh_{meshCounter}");
        collisionObject.transform.position = bakedObject.transform.position;
        collisionObject.transform.rotation = bakedObject.transform.rotation;
        collisionObject.transform.localScale = bakedObject.transform.localScale;

        MeshCollider collisionCollider = collisionObject.AddComponent<MeshCollider>();
        collisionCollider.sharedMesh = collisionMesh;
        collisionCollider.isTrigger = true;

        // Désactiver l'option Fast Midphase pour le mesh de collision si nécessaire
        if (collisionMesh.triangles.Length / 3 > 1500000)
        {
            collisionCollider.cookingOptions &= ~UnityEngine.MeshColliderCookingOptions.UseFastMidphase;
            Debug.Log("Fast Midphase désactivé pour le collider de collision");
        }

        // Optionnel: Création d'un prefab à partir des objets créés
#if UNITY_EDITOR
        // Prefab pour l'objet principal
        string prefabPath = $"Assets/GeneratedMeshes/Belgium_Prefab_{meshCounter}_{System.DateTime.Now.ToString("yyyyMMdd_HHmmss")}.prefab";
        UnityEditor.PrefabUtility.SaveAsPrefabAsset(bakedObject, prefabPath);
        Debug.Log("Prefab sauvegardé à: " + prefabPath);

        // Prefab pour l'objet de collision
        string collisionPrefabPath = $"Assets/GeneratedMeshes/Belgium_Collision_Prefab_{meshCounter}_{System.DateTime.Now.ToString("yyyyMMdd_HHmmss")}.prefab";
        UnityEditor.PrefabUtility.SaveAsPrefabAsset(collisionObject, collisionPrefabPath);
        Debug.Log("Prefab de collision sauvegardé à: " + collisionPrefabPath);
#endif

        meshCounter++;
        Debug.Log($"Batch {meshCounter} traité avec succès.");

        // Le batch est traité, on passe à la suite
        collected.Clear();
    }

    // Crée un mesh de collision standard (sans simplification)
    Mesh CreateCollisionMesh(Mesh originalMesh)
    {
        // Créer un nouveau mesh uniquement avec les vertices et triangles de l'original
        Mesh newMesh = new Mesh();
        newMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        newMesh.vertices = originalMesh.vertices;
        newMesh.triangles = originalMesh.triangles;

        // Assurez-vous que le mesh est bien fermé pour le collider
        newMesh.RecalculateBounds();
        newMesh.RecalculateNormals();

        return newMesh;
    }

    // Version qui simplifie le mesh de collision pour éviter les problèmes de limite
    Mesh CreateSimplifiedCollisionMesh(Mesh originalMesh)
    {
        Mesh newMesh = new Mesh();
        newMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        // Si le mesh original est trop complexe, on le simplifie pour la collision
        if (originalMesh.triangles.Length / 3 > 1500000)
        {
            Debug.Log("Simplification du mesh de collision...");

            // Ici, nous pourrions appliquer une simplification plus avancée
            // Mais pour un exemple simple, on peut juste prendre un sous-ensemble des triangles
            // (environ 50% des triangles, récupérés en prenant un triangle sur deux)

            Vector3[] vertices = originalMesh.vertices;
            int[] originalTriangles = originalMesh.triangles;

            // Calcul du nombre de triangles à conserver (50%)
            int originalTriangleCount = originalTriangles.Length / 3;
            int newTriangleCount = Mathf.Min(originalTriangleCount / 2, 1000000);
            int[] newTriangles = new int[newTriangleCount * 3];

            // Copier un triangle sur deux
            for (int i = 0; i < newTriangleCount; i++)
            {
                int srcIndex = i * 2 * 3; // On saute un triangle à chaque fois
                int destIndex = i * 3;

                if (srcIndex + 2 < originalTriangles.Length)
                {
                    newTriangles[destIndex] = originalTriangles[srcIndex];
                    newTriangles[destIndex + 1] = originalTriangles[srcIndex + 1];
                    newTriangles[destIndex + 2] = originalTriangles[srcIndex + 2];
                }
            }

            newMesh.vertices = vertices;
            newMesh.triangles = newTriangles;
        }
        else
        {
            // Si le mesh est assez petit, on l'utilise tel quel
            newMesh.vertices = originalMesh.vertices;
            newMesh.triangles = originalMesh.triangles;
        }

        newMesh.RecalculateBounds();
        newMesh.RecalculateNormals();

        Debug.Log($"Mesh de collision: {newMesh.triangles.Length / 3} triangles");
        return newMesh;
    }

    double3 Wgs84ToEcef(double lonDeg, double latDeg, double height)
    {
        double a = 6378137.0;
        double e2 = 6.69437999014e-3;

        double latRad = math.radians(latDeg);
        double lonRad = math.radians(lonDeg);

        double cosLat = math.cos(latRad);
        double sinLat = math.sin(latRad);
        double cosLon = math.cos(lonRad);
        double sinLon = math.sin(lonRad);

        double N = a / math.sqrt(1 - e2 * sinLat * sinLat);

        double x = (N + height) * cosLat * cosLon;
        double y = (N + height) * cosLat * sinLon;
        double z = (N * (1 - e2) + height) * sinLat;

        return new double3(x, y, z);
    }
}