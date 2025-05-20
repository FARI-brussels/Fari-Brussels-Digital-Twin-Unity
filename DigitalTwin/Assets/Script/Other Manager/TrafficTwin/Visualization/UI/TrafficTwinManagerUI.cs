using UnityEngine;
using UnityEngine.UI;
using TMPro; // Pour TextMeshPro si vous l'utilisez
using System;

public class TrafficTwinManagerUI : MonoBehaviour
{
    private bool is_mesh = true;
    private bool is_roadworks = false;
    private bool is_speed = false;
    private bool is_density = true;
    private bool is_first_hour = true;

    // Couleurs pour les états actifs et inactifs
    public Color activeColor = Color.green;
    public Color inactiveColor = Color.red;

    // Références aux boutons et textes (à assigner dans l'inspecteur)
    public Button roadWorksButton;
    public Button densityButton;
    public Button speedButton;
    public Button hoursButton;

    // Références aux textes des boutons Mesh/Lines (si vous utilisez TextMeshPro, vous pouvez changer en TextMeshProUGUI)
    public TextMeshProUGUI meshLinesButtonText;  // Le texte du bouton Mesh/Lines
    public TextMeshProUGUI hoursButtonText;

    public void OnRoadWorksClicked()
    {
        Debug.Log("Button road works");
        is_roadworks = !is_roadworks;
        UpdateVisibility();
        UpdateButtonColors();
    }


    public void OnHoursClicked()
    {
        Debug.Log("Button Hours");
        is_first_hour = !is_first_hour;
        UpdateVisibility();
        UpdateButtonColors();
    }

    public void OnMeshLinesClicked()
    {
        Debug.Log("Button mesh lines");
        is_mesh = !is_mesh;
        UpdateVisibility();
        UpdateButtonColors();
    }

    public void OnDensityClicked()
    {
        Debug.Log("Button density");
        is_density = true;
        is_speed = false;
        UpdateVisibility();
        UpdateButtonColors();
    }

    public void OnSpeedClicked()
    {
        Debug.Log("Button speed");
        is_speed = true;
        is_density = false;
        UpdateVisibility();
        UpdateButtonColors();
    }

    private void UpdateButtonColors()
    {
        // Mise à jour des couleurs des boutons en fonction de leur état
        if (roadWorksButton != null)
        {
            Image roadWorksImage = roadWorksButton.GetComponent<Image>();
            if (roadWorksImage != null)
            {
                roadWorksImage.color = is_roadworks ? activeColor : inactiveColor;
            }
        }

        if (densityButton != null)
        {
            Image densityImage = densityButton.GetComponent<Image>();
            if (densityImage != null)
            {
                densityImage.color = is_density ? activeColor : inactiveColor;
            }
        }

        if (speedButton != null)
        {
            Image speedImage = speedButton.GetComponent<Image>();
            if (speedImage != null)
            {
                speedImage.color = is_speed ? activeColor : inactiveColor;
            }
        }

        if (hoursButtonText != null)
        {
            hoursButtonText.text = is_first_hour ? "0-3600" : "3600-7200";
        }
    }

    private void UpdateVisibility()
    {
        // Gestion des boutons de l'interface
        TrafficTwinManager.Instance.SetActiveByName("Button Speed", !is_mesh);
        TrafficTwinManager.Instance.SetActiveByName("Button Density", !is_mesh);
        TrafficTwinManager.Instance.SetActiveByName("Button Road Works", !is_mesh);

        // Correction : supprimer le mot "Button" en double
        TrafficTwinManager.Instance.SetActiveByName("Button Hours", !is_mesh);

        // Gestion du mode mesh vs lignes
        TrafficTwinManager.Instance.SetActiveByName("Mesh parents", is_mesh);

        // Si on est en mode mesh, on cache tout le reste
        if (is_mesh)
        {
            HideAllDataViews();
            if (meshLinesButtonText != null)
            {
                meshLinesButtonText.text = "Lines";  // Changer le texte en "Lines"
            }
            return;
        }

        // Sinon on est en mode lignes, afficher les données appropriées
        if (meshLinesButtonText != null)
        {
            meshLinesButtonText.text = "Mesh";  // Changer le texte en "Mesh"
        }

        if (is_density)
        {
            // Mode densité avec gestion des heures
            if (is_first_hour)
            {
                // Première tranche horaire (0-3600)
                TrafficTwinManager.Instance.SetActiveByName("Lines Density 0 to 3600 with roads works", is_roadworks);
                TrafficTwinManager.Instance.SetActiveByName("Lines Density 0 to 3600 without roads works", !is_roadworks);

                // Cacher la deuxième tranche
                TrafficTwinManager.Instance.SetActiveByName("Lines Density 3600 to 7200 with roads works", false);
                TrafficTwinManager.Instance.SetActiveByName("Lines Density 3600 to 7200 without roads works", false);
            }
            else
            {
                // Deuxième tranche horaire (3600-7200)
                TrafficTwinManager.Instance.SetActiveByName("Lines Density 3600 to 7200 with roads works", is_roadworks);
                TrafficTwinManager.Instance.SetActiveByName("Lines Density 3600 to 7200 without roads works", !is_roadworks);

                // Cacher la première tranche
                TrafficTwinManager.Instance.SetActiveByName("Lines Density 0 to 3600 with roads works", false);
                TrafficTwinManager.Instance.SetActiveByName("Lines Density 0 to 3600 without roads works", false);
            }

            // Cacher les données de vitesse (toutes tranches)
            TrafficTwinManager.Instance.SetActiveByName("Lines Speed 0 to 3600 with roads works", false);
            TrafficTwinManager.Instance.SetActiveByName("Lines Speed 0 to 3600 without roads works", false);
            TrafficTwinManager.Instance.SetActiveByName("Lines Speed 3600 to 7200 with roads works", false);
            TrafficTwinManager.Instance.SetActiveByName("Lines Speed 3600 to 7200 without roads works", false);
        }
        else if (is_speed)
        {
            // Mode vitesse avec gestion des heures
            if (is_first_hour)
            {
                // Première tranche horaire (0-3600)
                TrafficTwinManager.Instance.SetActiveByName("Lines Speed 0 to 3600 with roads works", is_roadworks);
                TrafficTwinManager.Instance.SetActiveByName("Lines Speed 0 to 3600 without roads works", !is_roadworks);

                // Cacher la deuxième tranche
                TrafficTwinManager.Instance.SetActiveByName("Lines Speed 3600 to 7200 with roads works", false);
                TrafficTwinManager.Instance.SetActiveByName("Lines Speed 3600 to 7200 without roads works", false);
            }
            else
            {
                // Deuxième tranche horaire (3600-7200)
                TrafficTwinManager.Instance.SetActiveByName("Lines Speed 3600 to 7200 with roads works", is_roadworks);
                TrafficTwinManager.Instance.SetActiveByName("Lines Speed 3600 to 7200 without roads works", !is_roadworks);

                // Cacher la première tranche
                TrafficTwinManager.Instance.SetActiveByName("Lines Speed 0 to 3600 with roads works", false);
                TrafficTwinManager.Instance.SetActiveByName("Lines Speed 0 to 3600 without roads works", false);
            }

            // Cacher les données de densité (toutes tranches)
            TrafficTwinManager.Instance.SetActiveByName("Lines Density 0 to 3600 with roads works", false);
            TrafficTwinManager.Instance.SetActiveByName("Lines Density 0 to 3600 without roads works", false);
            TrafficTwinManager.Instance.SetActiveByName("Lines Density 3600 to 7200 with roads works", false);
            TrafficTwinManager.Instance.SetActiveByName("Lines Density 3600 to 7200 without roads works", false);
        }
    }

    private void HideAllDataViews()
    {
        // Cacher toutes les visualisations de données
        TrafficTwinManager.Instance.SetActiveByName("Lines Density 0 to 3600 with roads works", false);
        TrafficTwinManager.Instance.SetActiveByName("Lines Density 0 to 3600 without roads works", false);
        TrafficTwinManager.Instance.SetActiveByName("Lines Speed 0 to 3600 with roads works", false);
        TrafficTwinManager.Instance.SetActiveByName("Lines Speed 0 to 3600 without roads works", false);

        // Ajouter les éléments pour la deuxième tranche horaire (3600-7200)
        TrafficTwinManager.Instance.SetActiveByName("Lines Density 3600 to 7200 with roads works", false);
        TrafficTwinManager.Instance.SetActiveByName("Lines Density 3600 to 7200 without roads works", false);
        TrafficTwinManager.Instance.SetActiveByName("Lines Speed 3600 to 7200 with roads works", false);
        TrafficTwinManager.Instance.SetActiveByName("Lines Speed 3600 to 7200 without roads works", false);
    }


    // Appeler cette méthode au démarrage pour initialiser l'affichage
    private void Start()
    {
        UpdateVisibility();
        UpdateButtonColors(); // Initialiser aussi les couleurs des boutons
    }
}
