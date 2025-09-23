using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimulationSelectionUI : MonoBehaviour
{
    public Button simulationv1;
    public Button simulationv2;

    private void Start()
    {
        if (simulationv1 != null)
            simulationv1.onClick.AddListener(() => OnSimulationSelected(SimulationType.simulationv1));

        if (simulationv2 != null)
            simulationv2.onClick.AddListener(() => OnSimulationSelected(SimulationType.simulationv2));

    }

    private void OnSimulationSelected(SimulationType type)
    {
        SimulationManager.Instance.SetSimulationType(type);
    }
}
