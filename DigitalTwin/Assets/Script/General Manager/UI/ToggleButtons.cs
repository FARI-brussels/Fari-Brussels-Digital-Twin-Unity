using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ToggleButtons : MonoBehaviour
{
    public Button toggleButton;
    public ToggleableButton[] buttonsToToggle;
    public GeneralManager generalManager;

    [System.Serializable]
    public class ToggleableButton
    {
        public Button button;
        [HideInInspector] public bool isActive = false; 
    }

    private bool buttonsVisible = false;

    void Start()
    {
        SetButtonsVisible(false);
        toggleButton.onClick.AddListener(ToggleButtonVisibility);

        foreach (var tb in buttonsToToggle)
        {
            SetButtonState(tb, false); // rouge et inactif
            tb.button.onClick.AddListener(() => ToggleTransportButton(tb));
        }
    }

    private void Update()
    {
        
    }

    void ToggleButtonVisibility()
    {
        buttonsVisible = !buttonsVisible;
        SetButtonsVisible(buttonsVisible);
    }

    void SetButtonsVisible(bool visible)
    {
        foreach (var tb in buttonsToToggle)
        {
            if(tb.button.name != "BuildingsButton")
            {
                tb.button.gameObject.SetActive(visible);
            }
        }
    }

    void ToggleTransportButton(ToggleableButton tb)
    {
        tb.isActive = !tb.isActive;
        SetButtonState(tb, tb.isActive);
        if (tb.button.name == "TrafficTwinButton")
        {
            if (tb.isActive)
            {
                generalManager.ShowTrafficManager();
            }
            else
            {
                generalManager.HideTrafficManager();
            }
        }
        else if (tb.button.name == "StibButton")
        {
            if (tb.isActive)
            {
                generalManager.ShowStibManager();
            }
            else
            {
                generalManager.HideStibManager();
            }
        }
        else if (tb.button.name == "SncbButton")
        {
            if (tb.isActive)
            {
                generalManager.ShowSncbManager();
            }
            else
            {
                generalManager.HideSncbManager();
            }
        }
        else if (tb.button.name == "DeLijnButton")
        {
            if (tb.isActive)
            {
                generalManager.ShowDeLijnManager();
            }
            else
            {
                generalManager.HideDeLijnManager();
            }
        }
        else if (tb.button.name == "BuildingsButton")
        {
            if (tb.isActive)
            {
                generalManager.ShowBuildings();
            }
            else
            {
                generalManager.HideBuildings();
            }
        }
    }

    void SetButtonState(ToggleableButton tb, bool isActive)
    {
        Image buttonImage = tb.button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = isActive ? Color.green : Color.red;
        }
    }
}
