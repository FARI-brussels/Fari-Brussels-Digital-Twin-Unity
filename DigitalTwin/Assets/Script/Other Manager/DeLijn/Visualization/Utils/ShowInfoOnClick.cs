using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class ShowInfoOnClickDL : MonoBehaviour
{
    //Camera follow script
    private CameraFollowOnClickDL cameraFollow;
    private bool _isStop;
    private string _stopId;
    private string _stopName;
    private string _stopUrl;
    private bool _wheelChairAccessible;
    private List<double> _coordinates;
    private string _vehicleId;
    private string _tripHeadSign;
    private string _tripShortName;
    private string _direction;
    private string _nameRoute;
    private string _urlRoute;

    public void Initialize(bool isStop = false, string stopId = "", string stopName= "", string stopUrl = "", bool wheelChairAccessible = false, List<double> coordinates = null, string vehicleId = "", string tripHeadSign = "", string tripShortName = "", string direction = "", string nameRoute = "",string urlRoute = "")
    {
        _isStop = isStop;

        _stopId = stopId;
        _stopName = stopName;
        _stopUrl = stopUrl;
        _wheelChairAccessible = wheelChairAccessible;
        _coordinates = coordinates;

        _vehicleId = vehicleId;
        _tripHeadSign = tripHeadSign;
        _tripShortName = tripShortName;
        _direction = direction;
        _nameRoute = nameRoute;
        _urlRoute = urlRoute;
    }

    private void Start()
    {
        //Instatiate the main camera
        cameraFollow = Camera.main.GetComponent<CameraFollowOnClickDL>();
    }

    private void OnMouseDown()
    {
        if (cameraFollow != null)
        {
            if(_isStop)
            {
                cameraFollow.Initialize(_isStop, _stopId, _stopName, _stopUrl, _wheelChairAccessible, _coordinates);
            }
            else
            {
                cameraFollow.Initialize(_isStop, vehicleId: _vehicleId, tripHeadSign:_tripHeadSign, tripShortName: _tripShortName, direction: _direction, nameRoute: _nameRoute,urlRoute:_urlRoute);
            }
            cameraFollow.SetTarget(transform);
        }
    }
}
