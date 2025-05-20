using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public class GTFSParserDeLijn
{
    public JsonCollection<JsonFeature<DeLijnShapeProperties, GeoGeometryLineString>> ParseShapes(string csvContent)
    {
        var lines = csvContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2) // Vérifie qu'il y a au moins une ligne d'en-tête et une donnée
        {
            Debug.LogError("Le fichier CSV est vide ou ne contient que l'en-tête.");
            return null;
        }

        var header = lines[0].Trim().Split(',').Select(h => h.Replace("\"", "")).ToArray();
        var shapeIndex = Array.IndexOf(header, "shape_id");
        var latIndex = Array.IndexOf(header, "shape_pt_lat");
        var lonIndex = Array.IndexOf(header, "shape_pt_lon");
        var seqIndex = Array.IndexOf(header, "shape_pt_sequence");

        if (shapeIndex == -1 || latIndex == -1 || lonIndex == -1 || seqIndex == -1)
        {
            Debug.LogError("L'en-tête du fichier CSV ne contient pas les colonnes attendues.");
            return null;
        }

        var shapeGroups = lines.Skip(1)
            .Select(line => line.Split(','))
            .Where(parts => parts.Length > Math.Max(shapeIndex, Math.Max(latIndex, Math.Max(lonIndex, seqIndex)))) // Vérifie que toutes les colonnes existent
            .Where(parts =>
            {
            // Nettoyage des guillemets pour les coordonnées
            string latString = parts[latIndex].Trim('"');
                string lonString = parts[lonIndex].Trim('"');

            // Validation des coordonnées et de la séquence
            bool isLatValid = double.TryParse(latString, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
                bool isLonValid = double.TryParse(lonString, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
                bool isSeqValid = int.TryParse(parts[seqIndex], out _);

                if (!isLatValid || !isLonValid || !isSeqValid)
                {
                    Debug.LogWarning($"Ligne ignorée : {string.Join(",", parts)} (format invalide)");
                    return false;
                }

                return true;
            })
            .GroupBy(parts => parts[shapeIndex]); // Regrouper par shape_id

        var features = new List<JsonFeature<DeLijnShapeProperties, GeoGeometryLineString>>();

        foreach (var group in shapeGroups)
        {
            var coordinates = group
                .OrderBy(parts => int.Parse(parts[seqIndex])) // Trier par shape_pt_sequence
                .Select(parts =>
                {
                // Nettoyage des guillemets pour les coordonnées
                string latString = parts[latIndex].Trim('"');
                    string lonString = parts[lonIndex].Trim('"');

                // Conversion en double après nettoyage
                double latitude = double.Parse(latString, CultureInfo.InvariantCulture);
                    double longitude = double.Parse(lonString, CultureInfo.InvariantCulture);

                    return new double[] { longitude, latitude };
                }).ToList();

            var geometry = new GeoGeometryLineString
            {
                type = "LineString",
                coordinates = coordinates
            };

            var feature = new JsonFeature<DeLijnShapeProperties, GeoGeometryLineString>
            {
                id = group.Key,
                type = "Feature",
                properties = new DeLijnShapeProperties { shape_id = group.Key },
                geometry = geometry
            };

            features.Add(feature);
        }

        return new JsonCollection<JsonFeature<DeLijnShapeProperties, GeoGeometryLineString>>
        {
            type = "FeatureCollection",
            features = features
        };
    }


    public JsonCollection<JsonFeature<DeLijnStopProperties, GeoGeometryPoint>> ParseStops(string csvContent)
    {
        var lines = csvContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var header = lines[0].Trim().Split(',').Select(h => h.Replace("\"", "")).ToArray();

        var stopIndex = Array.IndexOf(header, "stop_id");
        var nameIndex = Array.IndexOf(header, "stop_name");
        var latIndex = Array.IndexOf(header, "stop_lat");
        var lonIndex = Array.IndexOf(header, "stop_lon");
        var codeIndex = Array.IndexOf(header, "stop_code");
        var urlIndex = Array.IndexOf(header, "stop_url");
        var wheelchairIndex = Array.IndexOf(header, "wheelchair_boarding");

        var features = lines.Skip(1)
            .Select(line => line.Split(','))
            .Where(parts => parts.Length >= 6)  // On suppose qu'il y a au moins 6 colonnes
            .Select(parts =>
            {
                double latitude, longitude;

                // Nettoyage des guillemets autour des coordonnées
                string latString = parts[latIndex].Trim('"');
                string lonString = parts[lonIndex].Trim('"');

                // Validation des coordonnées (lat, lon)
                bool isLatValid = double.TryParse(latString, NumberStyles.Any, CultureInfo.InvariantCulture, out latitude);
                bool isLonValid = double.TryParse(lonString, NumberStyles.Any, CultureInfo.InvariantCulture, out longitude);

                // Si l'une des coordonnées est invalide, on ignore cette ligne
                if (!isLatValid || !isLonValid)
                {
                    // Log ou ignorer la ligne
                    Debug.LogWarning($"Coordonnées invalides pour le stop {parts[stopIndex]}: {parts[latIndex]}, {parts[lonIndex]}");
                    return null;
                }

                var geometry = new GeoGeometryPoint
                {
                    type = "Point",
                    coordinates = new List<double>
                {
                    longitude,
                    latitude
                }
                };

                return new JsonFeature<DeLijnStopProperties, GeoGeometryPoint>
                {
                    id = parts[stopIndex],
                    type = "Feature",
                    properties = new DeLijnStopProperties
                    {
                        stop_id = parts[stopIndex],
                        stop_name = parts[nameIndex],
                        stop_code = parts[codeIndex],
                        stop_url = parts[urlIndex],
                        wheelchair_accessible = parts[wheelchairIndex] == "1" // Si 1 alors true, sinon false
                },
                    geometry = geometry
                };
            }).ToList();

        return new JsonCollection<JsonFeature<DeLijnStopProperties, GeoGeometryPoint>>
        {
            type = "FeatureCollection",
            features = features
        };
    }


}
