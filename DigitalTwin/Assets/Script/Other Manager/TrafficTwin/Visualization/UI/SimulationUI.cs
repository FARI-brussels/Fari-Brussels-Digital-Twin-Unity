using UnityEngine;
using System.Collections;

public class SimulationUI : MonoBehaviour
{
    private enum SimulationState
    {
        Stopped,
        Running,
        Paused
    }

    private float[] speedMultipliers = { 1f, 2f, 10f, 20f, 50f };
    private int currentSpeedIndex = 0;

    private SimulationState currentState = SimulationState.Stopped;

    public void OnStartClicked()
    {
        switch (currentState)
        {
            case SimulationState.Stopped:
                // Premier appui sur Start -> passe à "Restart"
                SimulationManager.Instance.StartButtonText.text = "Restart";
                SimulationManager.Instance.PauseButtonText.text = "Pause";
                SimulationManager.Instance.StartSimulation();
                currentState = SimulationState.Running;
                break;

            case SimulationState.Running:
                // Appui sur "Restart" -> redémarre la simulation mais reste dans le même état
                SimulationManager.Instance.StartSimulation();
                // État reste à Running
                TrafficTwinManager.Instance.DestroyAllChildrenImmediate("Vehicle parents");
                TrafficTwinManager.Instance.InitAllVehicles();
                break;

            case SimulationState.Paused:
                // Appui sur "Start" -> reprend la simulation
                SimulationManager.Instance.StartButtonText.text = "Restart";
                SimulationManager.Instance.PauseButtonText.text = "Pause";
                SimulationManager.Instance.ResumeSimulation();
                currentState = SimulationState.Running;
                break;
        }
    }

    public void OnPauseClicked()
    {
        // Le bouton Pause ne fonctionne que si la simulation est déjà démarrée
        if (currentState == SimulationState.Stopped)
            return;

        if (currentState == SimulationState.Running)
        {
            // Mettre en pause
            SimulationManager.Instance.StartButtonText.text = "Resume";
            SimulationManager.Instance.PauseButtonText.text = "Stop";
            SimulationManager.Instance.PauseSimulation();
            currentState = SimulationState.Paused;
        }
        else if (currentState == SimulationState.Paused)
        {
            // Appui sur "Stop" -> arrête complètement la simulation
            SimulationManager.Instance.StartButtonText.text = "Start";
            SimulationManager.Instance.PauseButtonText.text = "Pause";
            SimulationManager.Instance.StopSimulation();
            currentState = SimulationState.Stopped;
            
            TrafficTwinManager.Instance.DestroyAllChildrenImmediate("Vehicle parents");
            TrafficTwinManager.Instance.InitAllVehicles();

        }
    }

    public void OnSpeedClicked()
    {
        // Passer à la vitesse suivante
        currentSpeedIndex = (currentSpeedIndex + 1) % speedMultipliers.Length;

        // Obtenir le multiplicateur actuel
        float currentMultiplier = speedMultipliers[currentSpeedIndex];

        // Appliquer la vitesse
        SimulationManager.Instance.SetSimulationSpeed(currentMultiplier);

        // Mettre à jour le texte du bouton (optionnel)
        if (SimulationManager.Instance.SpeedButtonText.text != null)
        {
            SimulationManager.Instance.SpeedButtonText.text = "x" + currentMultiplier;
        }
    }
}