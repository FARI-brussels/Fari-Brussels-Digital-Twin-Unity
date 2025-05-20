using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ShowStopInfoOnHoverStib : MonoBehaviour
{
    //Store initial variable
    private Vector3 initialScale;
    private GameObject textObject;
    private TextMeshPro textMesh;
    private Camera mainCamera;

    private JsonFeature<StibGeoPropertiesStops, GeoGeometryPoint> feature;
    private Color textColor;


    public void Initialize(JsonFeature<StibGeoPropertiesStops, GeoGeometryPoint> stopFeature)
    {
        feature = stopFeature;
        if (stopFeature.properties.route_short_name == "1" || stopFeature.properties.route_short_name == "2" || stopFeature.properties.route_short_name == "5" || stopFeature.properties.route_short_name == "6")
            textColor = Color.white;
        else
            textColor = Color.black;
    }

    private void Start()
    {
        initialScale = transform.localScale;
        mainCamera = Camera.main;

        textObject = new GameObject("HoverText");
        textObject.transform.SetParent(transform);
        textObject.transform.localPosition = new Vector3(0, -0.07f, 0);

        textMesh = textObject.AddComponent<TextMeshPro>();
        textMesh.text = "Stops : " + feature.properties.route_short_name + " -> " + feature.properties.stop_name;
        textMesh.fontSize = 30;
        textMesh.color = textColor;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.enableWordWrapping = false;
        // Ajuster la taille du RectTransform pour correspondre à la taille du texte
        RectTransform rectTransform = textObject.GetComponent<RectTransform>();

        // Calculer la taille du texte
        Vector2 textSize = textMesh.GetPreferredValues();

        // Ajuster la taille du RectTransform en fonction de la taille du texte
        rectTransform.sizeDelta = new Vector2(textSize.x, textSize.y);

        textObject.SetActive(false);
    }
    private void Update()
    {
        if (textObject.activeSelf)
        {
            textObject.transform.LookAt(mainCamera.transform);
            textObject.transform.Rotate(0, 180, 0);
        }
    }

    //Detect mouse enter
    private void OnMouseEnter()
    {
        IncreaseScale(true);
        textObject.SetActive(true);
    }

    //Detect mouse exit
    private void OnMouseExit()
    {
        IncreaseScale(false);
        textObject.SetActive(false);
    }

    private void IncreaseScale(bool status)
    {
        Vector3 finaleScale = initialScale;

        if (status)
        {
            finaleScale = initialScale * 1.3f;
        }
        transform.localScale = finaleScale;
    }


}
