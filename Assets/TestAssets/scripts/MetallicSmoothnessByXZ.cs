using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

public class MetallicSmoothnessByXZ : MonoBehaviour
{
    [SerializeField] private float smoothness = 0.5f;
    [SerializeField] private float metallic = 0.5f;
    
    private Material _matInst;
    private Transform _transform;
    private static readonly int MetallicID = Shader.PropertyToID("_Metallic");
    private static readonly int SmoothnessID = Shader.PropertyToID("_Smoothness");

    // Start is called before the first frame update
    void Awake()
    {
        _matInst = GetComponent<Renderer>().material;
    }

    private void Start()
    {
        Vector3 pos = transform.position;
        metallic = math.remap(-2, 2, 0, 1, pos.x);
        smoothness = math.remap(-2, 2, 0.1f, 0.9f, pos.z);
        _matInst.SetFloat(MetallicID, metallic);
        _matInst.SetFloat(SmoothnessID, smoothness);
    }

    // Update is called once per frame
    void OnDestroy()
    {
        if (_matInst != null)
        {
            Destroy(_matInst);
        }
    }
}
