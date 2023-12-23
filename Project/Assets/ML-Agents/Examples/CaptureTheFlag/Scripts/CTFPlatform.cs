using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CTFPlatform : MonoBehaviour
{
    public GameObject blueFlag;
    public GameObject redFlag;

    public bool blueFlagCaptured;
    public bool redFlagCaptured;

    private void Start()
    {
        blueFlagCaptured = false;
        redFlagCaptured = false;
    }
}
