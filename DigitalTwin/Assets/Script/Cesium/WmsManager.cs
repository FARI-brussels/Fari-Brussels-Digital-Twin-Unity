using CesiumForUnity;

public class WmsManager
{
    private CesiumWebMapServiceRasterOverlay _wmsOverlay;

    public WmsManager(CesiumWebMapServiceRasterOverlay overlay)
    {
        _wmsOverlay = overlay;
    }

    public void CreateWMS(string baseUrl, string layers)
    {
        if (_wmsOverlay == null)
        {
            UnityEngine.Debug.LogError("Aucune instance de WMS fournie.");
            return;
        }

        _wmsOverlay.baseUrl = baseUrl;
        _wmsOverlay.layers = layers;
        _wmsOverlay.Refresh();
    }

    public void SwitchWMS(string baseUrl, string layers)
    {
        if (_wmsOverlay == null)
        {
            UnityEngine.Debug.LogError("Aucune instance de WMS fournie.");
            return;
        }

        _wmsOverlay.baseUrl = baseUrl;
        _wmsOverlay.layers = layers;
        
    }
}
