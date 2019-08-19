// Copyright(C) 2019 GforceX
// https//https://github.com/Gforcex
// Email: gforcex@163.com


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PickUpMaterial : MonoBehaviour
{
    public delegate void OnPickUp(Material mat);

    private static readonly float ScaleCoordinate = 0.25f;
    private static readonly int MaxMaterialCount = 43;
    private static Material[] _pickupMats = new Material[MaxMaterialCount];

    private CommandBuffer _command;
    private RenderTexture _pickupRT;
    private Texture2D _pickupTex;
    private AsyncGPUReadbackRequest _request;
    private OnPickUp _callback;
    private bool _requesting = false;
    
    private GameObject _pickupObj;
    private Camera _pickupCam;

    private Renderer[] _allRenderers;
    private Dictionary<int, Material> _matIdDic = new Dictionary<int, Material>();
    
    /// <summary>
    /// set pickup obj
    /// </summary>
    /// <param name="bePickedRoot"></param>
    /// <param name="call"></param>
    public void SetPickupInfo(GameObject bePickedRoot, OnPickUp call)
    {
        _callback = call;
        _pickupObj = bePickedRoot;
        _allRenderers = bePickedRoot.GetComponentsInChildren<Renderer>();
    }

    /// <summary>
    /// pick up material
    /// </summary>
    /// <param name="cam"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public void Pickup(Camera cam, int x, int y)
    {
        if (_requesting)
            return;

        x = (int)((float)x * ScaleCoordinate);
        y = (int)((float)y * ScaleCoordinate);
        y = _pickupRT.height - y;

        _pickupCam = cam;

        drawRenderers();

        if (SystemInfo.supportsAsyncGPUReadback)
        {
            _requesting = true;
            _request = AsyncGPUReadback.Request(_pickupRT, 0, x, 1, y, 1, 0, 1, callAction);
        }
        else
        {

            if (_pickupTex == null)
                _pickupTex = new Texture2D(1, 1);

            RenderTexture old = RenderTexture.active;
            RenderTexture.active = _pickupRT;
            _pickupTex.ReadPixels(new Rect(x, y, 1, 1), 0, 0);
            _pickupTex.Apply();
            RenderTexture.active = old;

            callBack(_pickupTex.GetPixel(0, 0));
        }
        
    }

    private void drawRenderers()
    {
        _matIdDic.Clear();
        _command.Clear();

        _command.SetRenderTarget(_pickupRT);
        _command.ClearRenderTarget(true, true, Color.black);

        //float cameraAspect = (float)_pickupCam.pixelWidth / (float)_pickupCam.pixelHeight;
        //Matrix4x4 projectionMatrix = Matrix4x4.Perspective(_pickupCam.fieldOfView, cameraAspect,
        //                        _pickupCam.nearClipPlane, _pickupCam.farClipPlane);
        //_command.SetViewProjectionMatrices(_pickupCam.worldToCameraMatrix, projectionMatrix);
        _command.SetViewProjectionMatrices(_pickupCam.worldToCameraMatrix, GL.GetGPUProjectionMatrix(_pickupCam.projectionMatrix, true));       
        //_command.SetViewport(_pickupCam.rect);

        int matID = 0;
        int rsCount = _allRenderers.Length;
        for (int i = 0; i < rsCount; ++i)
        {
            SkinnedMeshRenderer smr = _allRenderers[i] as SkinnedMeshRenderer;
            if (smr != null)
            {
                for (int j = 0; j < smr.sharedMaterials.Length; ++j)
                {
                    if (smr.sharedMaterials[j] == null) continue;

                    _command.DrawRenderer(smr, _pickupMats[matID], j);
                    setMaterial(_pickupMats[matID], smr.sharedMaterials[j]);
                    _matIdDic.Add(matID, smr.sharedMaterials[j]);
                    matID++;

                }
            }
            else
            {
                Renderer r = _allRenderers[i];
                if (r != null)
                {
                    for (int j = 0; j < r.sharedMaterials.Length; ++j)
                    {
                        if (r.sharedMaterials[j] == null) continue;

                        _command.DrawRenderer(r, _pickupMats[matID], j);
                        setMaterial(_pickupMats[matID], r.sharedMaterials[j]);
                        _matIdDic.Add(matID, r.sharedMaterials[j]);
                        matID++;
                    }
                }
            }
        }

        Graphics.ExecuteCommandBuffer(_command);
    }

    private void setMaterial(Material desc, Material src)
    {
        string baseMap = "_BaseMap";
        string srcMap = "_BaseMap";
        bool find = true;
        if (src.GetTexture(baseMap) == null)
        {
            if (src.GetTexture("_MainTex") != null)
            {
                srcMap = "_MainTex";
            }
            else
            {
                find = false;
                desc.SetTexture("_BaseMap", null);
            }

        }

        if(find)
        {
            desc.SetTexture(baseMap, src.GetTexture(srcMap));
            desc.SetTextureOffset(baseMap, src.GetTextureOffset(srcMap));
            desc.SetTextureScale(baseMap, src.GetTextureScale(srcMap));
        }
    }

    private void callBack(Color32 color)
    {
        int matId = (int)color.r - 1;

        Material pickMat = null;
        if (matId >= 0 && matId < _matIdDic.Count)
            pickMat = _matIdDic[matId];

        Debug.Log(color);
        if (pickMat != null)
            Debug.Log(pickMat.name);

        if (_callback != null)
            _callback(pickMat);
    }
    private void OnEnable()
    {
        if (_command == null)
        {
            _command = new CommandBuffer();
            _command.name = "Pickup";
        }

        int rtW = (int)((float)Screen.width * ScaleCoordinate);
        int rtH = (int)((float)Screen.height * ScaleCoordinate);

        if(_pickupRT != null && (rtW != _pickupRT.width || rtH != _pickupRT.height))
        {
            DestroyImmediate(_pickupRT);
            _pickupRT = null;
        }

        if(_pickupRT == null)
        {
            //if(SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8))
            //{
            //    _pickupRT = new RenderTexture(rtW, rtH, 16, RenderTextureFormat.R8);
            //}
            //else
            {
                _pickupRT = new RenderTexture(rtW, rtH, 16, RenderTextureFormat.Default);
            }
            
            _pickupRT.autoGenerateMips = false;
            _pickupRT.antiAliasing = 1;
            _pickupRT.filterMode = FilterMode.Point;
        }

        Shader shader = Shader.Find("Hidden/PickUpMaterial");

        for(int i = 0; i < MaxMaterialCount; ++i)
        {
            _pickupMats[i] = new Material(shader);
            _pickupMats[i].SetInt("_MaterialID",  i + 1);
        }
    }

    private void callAction<T>(T obj)
    {
        if (_request.done && _requesting)
        {
            _requesting = false;

            Unity.Collections.NativeArray<Color32> buffer = _request.GetData<Color32>();
            if (buffer != null && buffer.Length > 0)
            {
                callBack(buffer[0]);
            }
        }
    }

    private void onUpdate()
    {
        if (_request.done && _requesting)
        {
            _requesting = false;

            Unity.Collections.NativeArray<Color32> buffer = _request.GetData<Color32>();
            if (buffer != null && buffer.Length > 0)
            {
                callBack(buffer[0]);
            }
        }
    }

    private void OnDestroy()
    {
        if (_pickupRT != null)
            DestroyImmediate(_pickupRT);
        _pickupRT = null;

        if(_command != null)
            _command.Release();
    }
    private void LateUpdate()
    {
        //for test
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 pos = Input.mousePosition;
            SetPickupInfo(GameObject.Find("GameObject"), null);
            Pickup(Camera.main, (int)pos.x, (int)pos.y);
        }

        //----------------------------------------
        //onUpdate();
    }
}
