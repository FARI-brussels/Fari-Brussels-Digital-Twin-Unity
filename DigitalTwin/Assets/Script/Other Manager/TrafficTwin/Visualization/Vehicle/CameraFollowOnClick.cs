using TMPro;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class CameraFollowOnClick : MonoBehaviour
{
    [Header("Camera Settings")]
    public float followSpeed = 5f;  // Speed at which the camera follows the target.
    public Vector3 offset = new Vector3(0, 2, -5);  // Camera position offset when focusing.
    private Transform target;  // The current target being followed.
    private bool isFocused = false;  // Indicates if the camera is currently focused on a target.

    [Header("Exit Focus Settings")]
    private Vector3 exitOffset = new Vector3(0, 1000, -4000);  // Offset to move camera when exiting focus.

    [Header("UI Elements")]
    public TextMeshProUGUI infoText;  // UI text to display target information.

    private TripInfo _vehicleTripInfo;
    /// Initializes the object with data about the vehicle position and its terminus.
    public void Initialize(TripInfo vehicleTripInfo)
    {
        _vehicleTripInfo = vehicleTripInfo;
    }



    private void Update()
    {
        // Allows the user to exit focus by pressing the 'Q' key.
        if (Input.GetKeyDown(KeyCode.Q) && isFocused)
        {
            TrafficTwinManager.Instance.SetActiveByName("Canvas Simulation", true);
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
        if (_vehicleTripInfo == null) return;

        infoText.text = $"<b>Vehicle ID :</b> {_vehicleTripInfo.id}\n" +
                        $"<b>Departure :</b> at {_vehicleTripInfo.depart:0.##}s (depart delay : {_vehicleTripInfo.departDelay:0.##}s, speed : {_vehicleTripInfo.departSpeed:0.##} m/s)\n" +
                        $"<b>Arrival :</b> at {_vehicleTripInfo.arrival:0.##}s (speed : {_vehicleTripInfo.arrivalSpeed:0.##} m/s)\n" +
                        $"<b>Duration :</b> {_vehicleTripInfo.duration:0.##}s\n" +
                        $"<b>Distance :</b> {_vehicleTripInfo.routeLength:0.##} m\n" +
                        $"<b>Waiting Time :</b> {_vehicleTripInfo.waitingTime:0.##}s ({_vehicleTripInfo.waitingCount} time)\n" +
                        $"<b>Time Loss :</b> {_vehicleTripInfo.timeLoss:0.##}s\n" +
                        $"<b>Reroute ? :</b> {_vehicleTripInfo.rerouteNo}";
    }
}
