using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ShowInfoOnHover : MonoBehaviour
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
    private Color textColor = Color.black;

    // **Constructor 
    public void Initialize()
    {

    }

    private void Start()
    {
        //Initialize value
        initialScale = transform.localScale;
        mainCamera = Camera.main;

        //Create Game Object to store the TextMeshPro
        textObject = new GameObject("HoverText");
        textObject.transform.SetParent(transform);
        textObject.transform.localPosition = new Vector3(0, 2f, 0);

        //Configuration TextMeshPro
        textMesh = textObject.AddComponent<TextMeshPro>();
        //Number of the vehicle and it's teminus
        textMesh.text = "Vehicle " + this.gameObject.name;
        textMesh.fontSize = 20;
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
