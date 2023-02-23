using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[DisallowMultipleComponent]
public class RandomTintColor : MonoBehaviour
{
    private static readonly int _tintID = Shader.PropertyToID("_Tint");
    private Material _matInst;
    
    // Start is called before the first frame update
    public void Awake()
    {
        _matInst = GetComponent<Renderer>().material;
    }
    
    
    // Write a method that updates the tint color of the material instance for every 3 seconds, using Coroutine
    private float _updateColorTimer = 0;
    private float _updateColorInterval = 3;

    public void Start()
    {
        _matInst.SetColor(_tintID, Random.ColorHSV());
    }

    public void Update()
    {
        float deltaTime = Time.deltaTime;
        _updateColorTimer += deltaTime;
        if (_updateColorTimer >= _updateColorInterval)
        {
            _updateColorTimer -= _updateColorInterval;
            _matInst.SetColor(_tintID, Random.ColorHSV());
        }
    }


    public void OnDestroy()
    {
        if (_matInst != null)
        {
            Destroy(_matInst);
        }
    }
}
