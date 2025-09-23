using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VehicleMoverv2 : MonoBehaviour
{
    private List<Point> path;
    private int currentIndex = 0;


    private float speed;
    private Vector3 currentStart, currentEnd;

    private float segmentDistance;
    private float traveledDistance;

    private float lastSimTime = 0f;

    string id = "";

    public void Init(string vehicleId, List<Point> fullPath)
    {
        id = vehicleId;
        path = fullPath;
        if (path == null) return;

        currentIndex = 0;
        currentStart = path[currentIndex].unityPos;
        currentEnd = path[currentIndex + 1].unityPos;
        speed = path[currentIndex + 1].speed;

        transform.position = currentStart;
        traveledDistance = 0f;
        segmentDistance = Vector3.Distance(currentStart, currentEnd);

        lastSimTime = SimulationManager.Instance.simulationTime;
        SimulationManager.Instance.OnSimulationTimeUpdated += OnSimTimeUpdated;
    }

    private void OnDestroy()
    {
        // memory leaks security
        if (SimulationManager.Instance != null)
            SimulationManager.Instance.OnSimulationTimeUpdated -= OnSimTimeUpdated;
    }

    private void OnSimTimeUpdated(float simTime)
    {
        if (path == null || path.Count < 2)
        {
            Destroy(gameObject);
            return;
        }
        if (path == null || currentIndex >= path.Count - 1)
        {
            Destroy(gameObject);
            return;
        };

        float deltaSim = simTime - lastSimTime;
        lastSimTime = simTime;

        if (speed <= 0.01f)
        {
            currentIndex++;
            if (currentIndex < path.Count - 1)
            {
                currentStart = path[currentIndex].unityPos;
                currentEnd = path[currentIndex + 1].unityPos;
                speed = path[currentIndex + 1].speed;
                traveledDistance = 0f;
                segmentDistance = Vector3.Distance(currentStart, currentEnd);
            }
            return;
        }

        traveledDistance += speed * deltaSim;

        float t = Mathf.Clamp01(traveledDistance / segmentDistance);
        Vector3 newPos = Vector3.Lerp(currentStart, currentEnd, t);

        Vector3 dir = (currentEnd - currentStart);
        if (dir != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(dir);
        }

        transform.position = newPos;

        if (t >= 1f)
        {
            currentIndex++;
            if (currentIndex < path.Count - 1)
            {
                currentStart = path[currentIndex].unityPos;
                currentEnd = path[currentIndex + 1].unityPos;
                speed = path[currentIndex + 1].speed;
                traveledDistance = 0f;
                segmentDistance = Vector3.Distance(currentStart, currentEnd);
            }
        }
    }

}
