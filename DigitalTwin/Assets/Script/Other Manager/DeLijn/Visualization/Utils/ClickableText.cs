using UnityEngine;
using TMPro;

public class ClickableText : MonoBehaviour
{
    private TextMeshProUGUI textMeshPro;

    private void Start()
    {
        textMeshPro = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Vérifie le clic gauche
        {
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(textMeshPro, Input.mousePosition, null);
            if (linkIndex != -1) // Un lien a été cliqué
            {
                TMP_LinkInfo linkInfo = textMeshPro.textInfo.linkInfo[linkIndex];
                Application.OpenURL(linkInfo.GetLinkID()); // Ouvre l'URL dans le navigateur
            }
        }
    }
}
