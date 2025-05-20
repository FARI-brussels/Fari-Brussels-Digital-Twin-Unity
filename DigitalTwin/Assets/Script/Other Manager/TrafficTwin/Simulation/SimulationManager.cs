using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.Events;

public class SimulationManager : MonoBehaviour
{
    public static SimulationManager Instance;

    public float simulationTime { get; private set; } = 0f;
    public float simulationSpeed = 1f;
    public bool isRunning = false;

    public float maxSimulationTime = 7200f; 
    public float simulationStartTime = 10f;

    public delegate void SimulationTimeUpdated(float simulationTime);
    public event SimulationTimeUpdated OnSimulationTimeUpdated;

    public TextMeshProUGUI timerText;
    public TextMeshProUGUI StartButtonText;
    public TextMeshProUGUI PauseButtonText;
    public TextMeshProUGUI SpeedButtonText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;  
        }
        else
        {
            Destroy(gameObject);  // Si une instance existe déjà, détruit le nouvel objet
        }
    }
    private void Start()
    {
        UpdateTimerDisplay();
    }

    // Update is called once per frame
    void Update()
    {
        if (!isRunning) return;

        simulationTime += Time.deltaTime * simulationSpeed;

        // Notifie les objets abonnés (ex: spawner, UI)
        OnSimulationTimeUpdated?.Invoke(simulationTime);

        UpdateTimerDisplay();

        if (simulationTime >= maxSimulationTime)
        {
            isRunning = false;
            Debug.Log("Simulation terminée !");
        }
    }

    public void StartSimulationAtTime(float startTime)
    {
        simulationTime = startTime; // Réinitialise le temps de simulation au temps spécifié
        isRunning = true;
    }


    private void UpdateTimerDisplay()
    {
        int hours = Mathf.FloorToInt(simulationTime / 3600);  // 3600 secondes = 1 heure
        int minutes = Mathf.FloorToInt((simulationTime % 3600) / 60);  // Reste des minutes après avoir extrait les heures
        int seconds = Mathf.FloorToInt(simulationTime % 60);  // Reste des secondes après avoir extrait les minutes

        // Formate le texte du timer pour afficher les heures, minutes et secondes
        timerText.text = string.Format("{0:D2}:{1:D2}:{2:D2}", hours, minutes, seconds);
    }


    public void StartSimulation()
    {
        simulationTime = 0f;
        isRunning = true;
    }

    public void SetSimulationSpeed(float speed)
    {
        simulationSpeed = speed;
    }

    public void PauseSimulation()
    {
        if (isRunning == false)
        {
            isRunning = true;
        }
        else
        {
            isRunning = false;
        }
        
    }

    public void StopSimulation()
    {
        simulationTime = 0f;
        UpdateTimerDisplay();
        isRunning = false;
    }

    public void ResumeSimulation()
    {
        isRunning = true;
    }


}
