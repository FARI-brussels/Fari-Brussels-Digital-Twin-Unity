using UnityEngine;
using CesiumForUnity;
using System.Collections.Generic;
using Newtonsoft.Json;
using Unity.Mathematics;
using System.Collections;
using System;

public class GeoJsonCesiumSpawner
{
    private CesiumGeoreference georeference;

    public void Initialize(CesiumGeoreference _georeference)
    {
        georeference = _georeference;
    }

    public Vector3 GetCesiumPosition(double lon, double lat, double height = 0f)
    {
        double3 ecef = Wgs84ToEcef(lon, lat, height == 0 ? height : 0);
        double3 unityPosition = georeference.TransformEarthCenteredEarthFixedPositionToUnity(ecef);
        Vector3 unityVectorPosition = new Vector3(0, 0, 0);
        if (height == 0)
        {
            float terrainHeight = GetYPositionOnTerrain(lon, lat);

            unityVectorPosition = new Vector3((float)unityPosition.x, terrainHeight, (float)unityPosition.z);
        }
        else
        {
            unityVectorPosition = new Vector3((float)unityPosition.x, (float)height, (float)unityPosition.z);
        }

        return unityVectorPosition;


    }

    private double3 Wgs84ToEcef(double lonDeg, double latDeg, double height)
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

    private float GetYPositionOnTerrain(double longitude, double latitude)
    {
        // Conversion lat/lon vers ECEF
        double3 ecef = Wgs84ToEcef(longitude, latitude, 0);

        // Conversion ECEF vers Unity
        double3 unityPosition = georeference.TransformEarthCenteredEarthFixedPositionToUnity(ecef);
        Vector3 positionUnity = new Vector3((float)unityPosition.x, (float)unityPosition.y, (float)unityPosition.z);

        Vector3 positionAboveTerrain = new Vector3(positionUnity.x, positionUnity.y + 30000f, positionUnity.z);
        Vector3 directionToCenter = Vector3.down;

        RaycastHit[] hits = Physics.RaycastAll(positionAboveTerrain, directionToCenter, 200000f);

        if (hits.Length > 0)
        {
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        }

        foreach (RaycastHit hit in hits)
        {
            Collider col = hit.collider;
            if (col is MeshCollider)
            {
                // Retourner la hauteur Y du premier collider "valide"
                return hit.point.y;
            }
        }
        return (float)unityPosition.y + 0.1f;

        /*
        double3 earthCenterECEF = new double3(0, 0, 0);
        double3 earthCenterUnity = georeference.TransformEarthCenteredEarthFixedPositionToUnity(earthCenterECEF);
        Vector3 positionCenterUnity = new Vector3((float)earthCenterUnity.x, (float)earthCenterUnity.y, (float)earthCenterUnity.z);

        Vector3 positionAboveTerrain = new Vector3((float)unityPosition.x, (float)unityPosition.y + 10000f, (float)unityPosition.z);
        Vector3 directionToCenter = (positionCenterUnity - positionAboveTerrain).normalized;

        // Raycast pour trouver TOUS les objets touchés
        RaycastHit[] hits = Physics.RaycastAll(positionAboveTerrain, directionToCenter, 200000f);

        if (hits.Length > 0)
        {
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        }

        foreach (RaycastHit hit in hits)
        {
            Collider col = hit.collider;
            if (col is MeshCollider)
            {
                // Retourner la hauteur Y du premier collider "valide"
                return hit.point.y;
            }
        }
        // Si rien trouvé de valide
        return (float)unityPosition.y + 0.1f;*/
    }

}
