using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ShowInfoOnHoverStib : MonoBehaviour
{
    //Store initial scale
    private Vector3 initialScale;
    //GameObject for the hover text
    private GameObject textObject;
    //Variable to store TextMeshPro
    private TextMeshPro textMesh;
    //Main Camera
    private Camera mainCamera;
    //TextColor
    private Color textColor;

    //Vehicle variable 
    private JsonFeature<StibGeoPropertiesVehiclePostion, GeoGeometryPoint> feature;
    private JsonFeature<StibGeoPropertiesStops, GeoGeometryPoint> terminus;

    // **Constructor (not real one because it's not possible in MonoBehaviour
    public void Initialize(JsonFeature<StibGeoPropertiesVehiclePostion, GeoGeometryPoint> vecFeature, JsonFeature<StibGeoPropertiesStops, GeoGeometryPoint> lineTerminus)
    {
        //Vehicle feature
        feature = vecFeature;
        //Terminus feature
        terminus = lineTerminus;

        //Change text color if metro (Improve the visualization)
        if (vecFeature.properties.lineId == "1" || vecFeature.properties.lineId == "2" || vecFeature.properties.lineId == "5" || vecFeature.properties.lineId == "6")
            textColor = Color.white;
        else
            textColor = Color.black;
    }

    private void Start()
    {
        //Initialize value
        initialScale = transform.localScale;
        mainCamera = Camera.main;

        //Create Game Object to store the TextMeshPro
        textObject = new GameObject("HoverText");
        textObject.transform.SetParent(transform);
        textObject.transform.localPosition = new Vector3(0, 10f, 0);

        //Configuration TextMeshPro
        textMesh = textObject.AddComponent<TextMeshPro>();
        //Number of the vehicle and it's teminus
        textMesh.text = "Vehicule " + feature.properties.lineId + "-> " + terminus.properties.stop_name; 
        textMesh.fontSize = 30;
        textMesh.color = textColor;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.enableWordWrapping = false;
        //Get RectTransform
        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        //Get the preferred size for a specific text
        Vector2 textSize = textMesh.GetPreferredValues();
        //Ajust size depending on the preferred size 
        rectTransform.sizeDelta = new Vector2(textSize.x, textSize.y);
        //Set the text inactive at the begining
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
