using TMPro;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

public class CameraFollowOnClickSncb : MonoBehaviour
{
    [Header("Camera Settings")]
    public float followSpeed = 5f;  // Speed at which the camera follows the target.
    public Vector3 offset = new Vector3(0, 2, -5);  // Camera position offset when focusing.
    private Vector3 offsetStop;
    private Transform target;  // The current target being followed.
    private bool isFocused = false;  // Indicates if the camera is currently focused on a target.

    [Header("Exit Focus Settings")]
    public Vector3 exitOffset = new Vector3(0, 100, -400);  // Offset to move camera when exiting focus.

    [Header("UI Elements")]
    public TextMeshProUGUI infoText;  // UI text to display target information.

    private object _feature;

    /// Initializes the object with data about the vehicle position and its terminus.
    public void Initialize(object feature)
    {
        _feature = feature;
        if (_feature is JsonFeature< IGeoGeometry> sncbFeatureStop)
        {
            offsetStop = new Vector3(-offset.x, -offset.y + 2.95f, -offset.z-7.15f);
        }
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
            Vector3 desiredPosition = target.TransformPoint(offset);
            if (_feature is JsonFeature< IGeoGeometry> sncbFeatureStop)
            {
                desiredPosition = target.TransformPoint(offsetStop);
            }
        
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
        if (_feature is JsonFeature< IGeoGeometry> sncbFeatureVehicle)
        {
            if (sncbFeatureVehicle.geometry is IPointGeometry geoloc)
                infoText.text = $"Vehicle : {sncbFeatureVehicle.properties["trip_headsign"].Value<string>()}\n\n" +
                    $"Trip informations : \n" +
                    $"        Full Id : {sncbFeatureVehicle.properties["trip_id"].Value<string>()}\n" +
                    $"        Direction : {sncbFeatureVehicle.properties["trip_headsign"].Value<string>()}\n" +
                    $"\nLast localisation : \n" +
                    $"     Lat :{geoloc.coordinates[1]}\n" +
                    $"     Lon :{geoloc.coordinates[0]}\n\n" +
                    $"The vehicle is between :\n" +
                    $"     Start :  {sncbFeatureVehicle.properties["name_start"].Value<string>()} \n" +
                    $"     Id :  {sncbFeatureVehicle.properties["ptcarid_start"].Value<string>()} \n\n" +
                    $"     End : {sncbFeatureVehicle.properties["name_end"].Value<string>()} \n" +
                    $"     Id :  {sncbFeatureVehicle.properties["ptcarid_end"].Value<string>()}";
        }
        else if (_feature is JsonFeature< IGeoGeometry> sncbFeatureStop)
        {
            if (sncbFeatureStop.geometry is IPointGeometry geoloc)
                infoText.text = $"Stop : {sncbFeatureStop.properties["ptcarid"].Value<string>()}\n\n" +
                    $"Basic Informations : \n" +
                    $"        French name : {sncbFeatureStop.properties["longnamefrench"].Value<string>()}\n" +
                    $"        Dutch name : {sncbFeatureStop.properties["longnamedutch"].Value<string>()}\n" +
                    $"        Classification : {sncbFeatureStop.properties["classification"].Value<string>()}\n" +
                    $"        Taf Tap Code : {sncbFeatureStop.properties["taftapcode"].Value<string>()}\n" +
                    $"        Symbolic name : {sncbFeatureStop.properties["symbolicname"].Value<string>()}\n" +
                    $"\n Localisation : \n" +
                    $"     Lat :{geoloc.coordinates[1]}\n" +
                    $"     Lon :{geoloc.coordinates[0]}\n\n";
        }
    }
}
