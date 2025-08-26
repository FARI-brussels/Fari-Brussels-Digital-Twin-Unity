using UnityEngine;
using TMPro;

public class ShowInfoOnClickStib : MonoBehaviour
{
    //Camera follow script
    private CameraFollowOnClickStib cameraFollow;

    // Vehicle variable 
    private JsonFeature<GeoGeometryPoint> feature;
    private JsonFeature<GeoGeometryPoint> terminus;

    public void Initialize(JsonFeature<GeoGeometryPoint> vecFeature, JsonFeature< GeoGeometryPoint> lineTerminus)
    {
        //Vehicle feature
        feature = vecFeature;
        //Terminus feature
        terminus = lineTerminus;
    }

    private void Start()
    {
        //Instatiate the main camera
        cameraFollow = Camera.main.GetComponent<CameraFollowOnClickStib>();
    }

    private void OnMouseDown()
    {
        if (cameraFollow != null)
        {
            cameraFollow.Initialize(feature, terminus);
            cameraFollow.SetTarget(transform);
        }
    }
}
