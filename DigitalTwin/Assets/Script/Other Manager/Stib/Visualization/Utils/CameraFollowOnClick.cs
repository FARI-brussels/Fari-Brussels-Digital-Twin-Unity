using TMPro;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class CameraFollowOnClickStib : MonoBehaviour
{
    [Header("Camera Settings")]
    public float followSpeed = 5f;  // Speed at which the camera follows the target.
    public Vector3 offset = new Vector3(0, 600, -2000);  // Camera position offset when focusing.
    private Transform target;  // The current target being followed.
    private bool isFocused = false;  // Indicates if the camera is currently focused on a target.

    [Header("Exit Focus Settings")]
    private Vector3 exitOffset = new Vector3(0, 1000, -4000);  // Offset to move camera when exiting focus.

    [Header("UI Elements")]
    public TextMeshProUGUI infoText;  // UI text to display target information.

    // Stores vehicle and terminus information for the selected object.
    private JsonFeature<StibGeoPropertiesVehiclePostion, GeoGeometryPoint> feature;
    private JsonFeature<StibGeoPropertiesStops, GeoGeometryPoint> terminus;
    private StibManager stibManager;


    /// Initializes the object with data about the vehicle position and its terminus.
    public void Initialize(JsonFeature<StibGeoPropertiesVehiclePostion, GeoGeometryPoint> vecFeature, JsonFeature<StibGeoPropertiesStops, GeoGeometryPoint> lineTerminus)
    {
        feature = vecFeature;
        terminus = lineTerminus;
    }

    private void Start()
    {
        stibManager = GameObject.Find("StibManager").GetComponent<StibManager>();
    }

    private void Update()
    {
        // Allows the user to exit focus by pressing the 'Q' key.
        if (Input.GetKeyDown(KeyCode.Q) && isFocused)
        {
            ExitFocus();
        }

        // If a target is set and camera is focused, update camera position and display info.
        if (target != null && isFocused)
        {
            Vector3 desiredPosition = target.TransformPoint(offset);  // Converts offset to world coordinates.

            // Smooth camera movement toward the target.
            transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

            // Make the camera look at the target.
            transform.LookAt(target);

            // Update target information on the UI.
            DisplayTargetInfo();
        }
    }

    /// Sets the selected object as the target for the camera focus.
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        isFocused = true;
    }

    /// Moves the camera away from the target when exiting focus.
    private void ExitFocus()
    {
        if (target == null) return;

        Vector3 exitPosition = target.position + exitOffset;

        // Smooth transition to the exit position.
        transform.position = Vector3.Lerp(transform.position, exitPosition, followSpeed * Time.deltaTime);

        isFocused = false;  // Disable focus mode.
        infoText.text = "";  // Clear UI text.
        target = null;  // Remove the target reference.
    }

    /// Displays vehicle and terminus information on the UI.
    private void DisplayTargetInfo()
    {
        if (target != null && feature != null && terminus != null)
        {
            List<JsonFeature<StibGeoPropertiesStops, GeoGeometryPoint>> stopsBeforePoint = stibManager.FindTrajectory(feature.properties.lineId, feature.properties.direction - 1, feature.properties.pointId);

            infoText.text = $"Vehicle: {feature.properties.lineId}\nTerminus: {terminus.properties.stop_name}\n" +
                $"Last coordinates : \n" +
                $"        Latitude : {feature.geometry.coordinates[1]}\n" +
                $"        Longitude : {feature.geometry.coordinates[0]}" +
                $"\nDistance travel from the beginning : {feature.properties.distance * 0.611} m" +
                $"\nLast measure : {DateTimeOffset.FromUnixTimeSeconds((long)feature.properties.timestamp).DateTime.ToString("yyyy-MM-dd HH:mm:ss")}" +
                $"\n\nTrajectory:\n" +
                $"{string.Join(" --> ", stopsBeforePoint.Select(stop => stop.properties.stop_name))}";
        }
        else
        {
            infoText.text = "No information available.";
        }
    }

   
}
