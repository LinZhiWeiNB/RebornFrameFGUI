using System;
using FairyGUI;
using RebornFrame.Core;
using UnityEngine;

public class FguiPanelBase
{
    //多显示器专用
    public int DisplayID = 0;
    public bool KeepFullScrenn = false;//是否无视分屏

    public GComponent FGUIComponent;
    public string PackageName;
    public string PanelName;
    public bool UpdateThisFrame = false;
    private bool _NeedUpdate = false;

    public bool NeedUpdate
    {
        get => _NeedUpdate;
        set
        {
            if(_NeedUpdate == value) return;
            _NeedUpdate = value;
            if (_NeedUpdate)
            {
                AppInstance.Instance.UpdateActions -= BaseUpdate;
                AppInstance.Instance.UpdateActions += BaseUpdate;
            }
            else
            {
                AppInstance.Instance.UpdateActions -= BaseUpdate;
            }
        }
    }

    public bool LateUpdateThisFrame = false;
    
    private bool _NeedLateUpdate = false;
    public bool NeedLateUpdate
    {
        get => _NeedLateUpdate;
        set
        {
            if(_NeedLateUpdate == value) return;
            _NeedLateUpdate = value;
            if (_NeedLateUpdate)
            {
                AppInstance.Instance.LateUpdateActions -= BaseLateUpdate;
                AppInstance.Instance.LateUpdateActions += BaseLateUpdate;
            }
            else
            {
                AppInstance.Instance.LateUpdateActions -= BaseLateUpdate;
            }
        }
    }
    public bool isClosed = false;
    

    private void BaseUpdate()
    {
        if (!UpdateThisFrame)
        {
            UpdateThisFrame = true;
        }
        else if (NeedUpdate)
        {
            Update();
        }
    }

    private void BaseLateUpdate()
    {
        if (!LateUpdateThisFrame)
        {
            LateUpdateThisFrame = true;
        }
        else if (NeedLateUpdate)
        {
            LateUpdate();
        }
    }

    public void BaseClose()
    {
        NeedUpdate = false;

        NeedLateUpdate = false;

        OnClose();
    }
    
    public virtual void Start(object[] data)
    {
    }

    public virtual void Update()
    {
    }

    public virtual void LateUpdate()
    {
    }

    public virtual void OnShow()
    {
    }

    public virtual void OnHide()
    {
    }

    public virtual void OnClose()
    {
    }

    public virtual string GetPackageName()
    {
        return "";
    }

    public virtual string GetResName()
    {
        return "";
    }
    
}