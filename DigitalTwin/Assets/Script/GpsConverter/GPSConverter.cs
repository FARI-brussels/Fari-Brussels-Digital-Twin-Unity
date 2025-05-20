using UnityEngine;
using System;

public class GPSConverter
{

    // Coordinate of the center of Brussels
    private const double BrusselsLat = 50.8503;
    private const double BrusselsLon = 4.3517;

    private const float EarthRadius = 6371000f;

    public Vector3 GPStoUCS(Vector3 referencePosition,
                             double targetLat, double targetLon, bool is_metro = false)
    {
        // Convertir les latitudes et longitudes en radians
        double refLatRad = BrusselsLat * Mathf.Deg2Rad;
        double refLonRad = BrusselsLon * Mathf.Deg2Rad;
        double targetLatRad = targetLat * Mathf.Deg2Rad;
        double targetLonRad = targetLon * Mathf.Deg2Rad;

        // Différences de latitude et longitude
        double dLat = targetLatRad - refLatRad;
        double dLon = targetLonRad - refLonRad;

        // Haversine formula avec plus de précision
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(refLatRad) * Math.Cos(targetLatRad) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double centralAngle = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        float distance = (float)(EarthRadius * centralAngle);

        // Calcul de l'azimut (direction)
        double y = Math.Sin(dLon) * Math.Cos(targetLatRad);
        double x = Math.Cos(refLatRad) * Math.Sin(targetLatRad) -
                   Math.Sin(refLatRad) * Math.Cos(targetLatRad) * Math.Cos(dLon);

        double bearing = Math.Atan2(y, x);

        // Conversion plus robuste des coordonnées
        float unityX = referencePosition.x + distance * (float)Math.Sin(bearing);
        float unityZ = referencePosition.z + distance * (float)Math.Cos(bearing);

        // Gestion de la profondeur pour le mode métro
        float unityY = is_metro ? referencePosition.y - 400 : referencePosition.y;

        return new Vector3(unityX, unityY, unityZ);
    }


}