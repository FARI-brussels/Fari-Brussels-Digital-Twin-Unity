using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DelijnVehicleMover : MonoBehaviour
{
    private List<Vector3> path = new List<Vector3>();
    private int currentPathIndex = 0;
    private bool isMoving = false;

    private float dynamicSpeed; // Vitesse ajustée selon la distance
    private float rotationSpeed = 5f; // Vitesse de rotation
    private float reachDistance = 0.01f; // Distance pour considérer un point atteint

    void Update()
    {
        if (isMoving && path != null && path.Count > 0 && currentPathIndex < path.Count)
        {
            MoveAlongPath();
        }
    }

    public void SetPath(List<Vector3> newPath, float travelTime)
    {
        if (newPath == null || newPath.Count == 0)
        {
            return;
        }

        path = newPath;
        currentPathIndex = 0;

        // Calcul de la distance totale du chemin
        float totalDistance = 0f;
        for (int i = 0; i < path.Count - 1; i++)
        {
            totalDistance += Vector3.Distance(path[i], path[i + 1]);
        }

        // Déterminer une vitesse réaliste pour parcourir la distance en `travelTime` secondes
        dynamicSpeed = totalDistance / travelTime;

        isMoving = true;
    }

    private void MoveAlongPath()
    {
        // Point actuel à atteindre
        Vector3 targetPoint = path[currentPathIndex];

        // Direction vers le point cible
        Vector3 direction = targetPoint - transform.position;
        direction.y = 0; // Ignorer la différence de hauteur si nécessaire

        // Vérifier si on a atteint le point
        if (direction.magnitude < reachDistance)
        {
            // Passer au point suivant
            currentPathIndex++;

            // Si on a atteint la fin du chemin
            if (currentPathIndex >= path.Count)
            {
                isMoving = false;
                return;
            }
        }

        // Rotation vers la cible
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        transform.position = Vector3.MoveTowards(transform.position, targetPoint, dynamicSpeed * Time.deltaTime);
    }


}
