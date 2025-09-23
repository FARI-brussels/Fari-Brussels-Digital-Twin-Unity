using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class VehicleMover : MonoBehaviour
{
    private List<Vector3> path;
    private int currentSegment = 0;
    private float segmentProgress = 0f;
    private float movementSpeed; 
    private Vector3 currentStart, currentEnd;
    private float lastSimTime = 0f;
    private bool isFirstUpdate = true;
    public float maxDeltaTime = 0.5f; // Limite maximale pour deltaTime

    private float _duration;
    private float _totalPathLength;

    public void Init(string vehicleId, List<Vector3> fullPath, float duration )
    {
        _duration = Mathf.Max(0.1f, duration);
        path = fullPath;
        if (path == null || path.Count < 2) return;

        // Calcul de la longueur totale du chemin
        _totalPathLength = 0f;
        for (int i = 0; i < path.Count - 1; i++)
        {
            _totalPathLength += Vector3.Distance(path[i], path[i + 1]);
        }

        movementSpeed = _totalPathLength / _duration;

        transform.position = path[0];
        currentStart = path[0];
        currentEnd = path[1];
        lastSimTime = SimulationManager.Instance.simulationTime;
        isFirstUpdate = true;
        SimulationManager.Instance.OnSimulationTimeUpdated += OnSimTimeUpdated;
    }
    
    private void OnDestroy()
    {
        if (SimulationManager.Instance != null)
            SimulationManager.Instance.OnSimulationTimeUpdated -= OnSimTimeUpdated;
    }
    
    private void OnSimTimeUpdated(float simTime)
    {
        // Ignore la première mise à jour pour éviter un grand deltaTime initial
        if (isFirstUpdate)
        {
            lastSimTime = simTime;
            isFirstUpdate = false;
            return;
        }
        
        // Calcul du deltaTime avec une limite maximale
        float deltaTime = Mathf.Min(simTime - lastSimTime, maxDeltaTime);
        lastSimTime = simTime;
        
        // Ignorer les deltaTime négatifs ou trop petits
        if (deltaTime <= 0.0001f) return;
        
        if (path == null || currentSegment >= path.Count - 1) return;
        
        float distanceToTravel = movementSpeed * deltaTime;
        while (distanceToTravel > 0f && currentSegment < path.Count - 1)
        {
            float segmentLength = Vector3.Distance(currentStart, currentEnd);
            
            // Éviter la division par zéro
            if (segmentLength <= 0.001f)
            {
                currentSegment++;
                if (currentSegment < path.Count - 1)
                {
                    currentStart = path[currentSegment];
                    currentEnd = path[currentSegment + 1];
                    segmentProgress = 0f;
                }
                else
                {
                    transform.position = new Vector3(path[path.Count - 1].x, path[path.Count - 1].y + 3.2f, path[path.Count - 1].z);
                    Destroy(gameObject);
                    return;
                }
                continue;
            }
            
            float remainingDistance = segmentLength * (1f - segmentProgress);
            if (distanceToTravel >= remainingDistance)
            {
                // Passe au segment suivant
                distanceToTravel -= remainingDistance;
                currentSegment++;
                if (currentSegment < path.Count - 1)
                {
                    currentStart = path[currentSegment];
                    currentEnd = path[currentSegment + 1];
                    segmentProgress = 0f;
                    transform.position = new Vector3(currentStart.x, currentStart.y + 3.2f, currentStart.z);
                }
                else
                {
                    transform.position = new Vector3(currentEnd.x, currentEnd.y + 3.2f, currentEnd.z);
                    Destroy(gameObject);
                    return;
                }
            }
            else
            {
                segmentProgress += distanceToTravel / segmentLength;
                Vector3 newPos = Vector3.Lerp(currentStart, currentEnd, segmentProgress);
                transform.position = new Vector3(newPos.x, newPos.y + 3.2f, newPos.z);
                distanceToTravel = 0f;
            }
            
            Vector3 direction = currentEnd - currentStart;
            if (direction.magnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }
}