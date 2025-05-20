using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ShowStopInfoOnHoverDL : MonoBehaviour
{
    //Store initial variable
    private Vector3 initialScale;
    private GameObject textObject;
    private TextMeshPro textMesh;
    private Camera mainCamera;
    private string _name;
    private string _id;

    public void Initialize(string name, string id)
    {
        _name = name;
        _id = id;
    }

    private void Start()
    {
        initialScale = transform.localScale;
        mainCamera = Camera.main;

        textObject = new GameObject("HoverText");
        textObject.transform.SetParent(transform);
        textObject.transform.localPosition = new Vector3(0, -0.05f, 0);

        textMesh = textObject.AddComponent<TextMeshPro>();
        textMesh.text = $"Stops {_id} : " + _name;
        textMesh.fontSize = 30;
        textMesh.color = Color.black;
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
