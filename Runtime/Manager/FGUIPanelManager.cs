using System;
using System.Collections.Generic;
using FairyGUI;
using RebornFrame.Core;
using UnityEngine;

public class FGUIPanelManager : RebornFrame.Utils.Singleton<FGUIPanelManager>
{
    public SplitModeEnum CurrentSplitMode => _CurrentSplitMode;
    
    private SplitModeEnum _CurrentSplitMode = SplitModeEnum.Single;
    
    private ScreenOrientation nowOrientation;

    public int DisplayCount = 1;
    public FGUIPanelManager()
    {
        nowOrientation = Screen.orientation;
        AppInstance.Instance.UpdateActions -= Update;
        AppInstance.Instance.UpdateActions += Update;
        // AppInstance.Instance.LateUpdateActions -= LateUpdate;
        // AppInstance.Instance.LateUpdateActions += LateUpdate;
        // ScreenWidth = Screen.width;
        // ScreenHeight = Screen.height;
        // GRoot.inst.MakeFullScreen();
    }

    private Dictionary<string, int> packageLoaded = new Dictionary<string, int>()
    {
       
    };
    
    private Dictionary<string, UIPackage> packages = new Dictionary<string, UIPackage>()
    {
        
    };

    private string GlobalController = "";
    private int GlobalControllerValue = -1;

    private Dictionary<int, ArrayListT<FguiPanelBase>> panels = new Dictionary<int, ArrayListT<FguiPanelBase>>();
    private Dictionary<int, FguiPanelBase> _currentPanel = new Dictionary<int, FguiPanelBase>();

    private Dictionary<int, Stack<FguiPanelBase>> panelStack = new Dictionary<int, Stack<FguiPanelBase>>();


    private List<GComponent> rebornFrameRoot = new List<GComponent>();

    public FguiPanelBase GetCurrentPanel(int displayId = 0) =>
        (_currentPanel == null || !_currentPanel.ContainsKey(displayId)) ? null : _currentPanel[displayId];

    public Vector2[] ScreenSize = new Vector2[5];

    void Update()
    {
        if (Screen.orientation != nowOrientation)
        {
            var enumerator = panels.Values.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var panelList = enumerator.Current;

                for (int i = 0; i < panelList.Count; i++)
                {
                    var panel = panelList[i];
                    // var index = Math.Max(0, panelList[i].DisplayID - 1);
                    //panelList[i].FGUIComponent.SetSize(ScreenSize[index].x, ScreenSize[index].y);
                    // panelList[i].FGUIComponent.SetSize(Display.displays[index].renderingWidth,
                    //     Display.displays[index].renderingHeight);
                    ChangePanelSizeByCurrentSpliteMode(panel);
                }
            }

            nowOrientation = Screen.orientation;
        }
    }

    public float DefaultCameraSize = 5;

    public Camera CreateCamera(string name, int cullingMask)
    {
        GameObject cameraObject = GameObject.Find(name);
        if (cameraObject == null)
        {
            cameraObject = new GameObject(name);
        }

        UnityEngine.Object.DontDestroyOnLoad(cameraObject);
        Camera camera = cameraObject.GetComponent<Camera>();
        if (camera == null)
        {
            camera = cameraObject.AddComponent<Camera>();
        }

        camera.depth = 1;
        camera.cullingMask = cullingMask;
        camera.clearFlags = CameraClearFlags.Depth;
        camera.orthographic = true;
        camera.orthographicSize = DefaultCameraSize;
        camera.nearClipPlane = -30;
        camera.farClipPlane = 30;

#if UNITY_5_4_OR_NEWER
        camera.stereoTargetEye = StereoTargetEyeMask.None;
#endif

#if UNITY_5_6_OR_NEWER
        camera.allowHDR = false;
        camera.allowMSAA = false;
#endif

        cameraObject.AddComponent<StageCamera>();
        return camera;
    }

    
    public void ChangeSplitMode(SplitModeEnum mode)
    {
        _CurrentSplitMode = mode;
        foreach (var panelMap in panels)
        {
            for (var i = 0; i < panelMap.Value.Count; i++)
            {
                var panel = panelMap.Value[i];
                if(panel.KeepFullScrenn)
                    continue;
                ChangePanelSizeByCurrentSpliteMode(panel);
            }
        }
    }

    public void ChangePanelSizeByCurrentSpliteMode(FguiPanelBase panel)
    {
        switch (CurrentSplitMode)
        {
            case SplitModeEnum.Single:
                panel.FGUIComponent.SetSize(GRoot.inst.width,GRoot.inst.height);
                break;
            case SplitModeEnum.Double:
                panel.FGUIComponent.SetSize(GRoot.inst.width/2,GRoot.inst.height);
                break;
            case SplitModeEnum.VerticalDouble:
                panel.FGUIComponent.SetSize(GRoot.inst.width,GRoot.inst.height/2);
                break;
            case SplitModeEnum.UpTriple:
                if (panel.DisplayID == 1)
                {
                    panel.FGUIComponent.SetSize(GRoot.inst.width,GRoot.inst.height/2);
                }
                else
                {
                    panel.FGUIComponent.SetSize(GRoot.inst.width/2,GRoot.inst.height/2);
                }
                break;
            case SplitModeEnum.DownTriple:
                if (panel.DisplayID == 3)
                {
                    panel.FGUIComponent.SetSize(GRoot.inst.width,GRoot.inst.height/2);
                }
                else
                {
                    panel.FGUIComponent.SetSize(GRoot.inst.width/2,GRoot.inst.height/2);
                }
                break;
            case SplitModeEnum.Quadruple:
                panel.FGUIComponent.SetSize(GRoot.inst.width/2,GRoot.inst.height/2);
                break;
        }
    }
        
        
    public T ShowPanel<T>(string packageName, string panelName, object[] data = null,
        ShowTypeEnum showType = ShowTypeEnum.HideCurrent, int DisplayID = 0, Camera displayCamera = null,
        int orderIndex = OrderIndex.Normal) where T : FguiPanelBase, new()
    {
        T panel = new T();
        if (string.IsNullOrEmpty(packageName))
        {
            packageName = panel.GetPackageName();
        }

        if (string.IsNullOrEmpty(panelName))
        {
            panelName = panel.GetResName();
        }

        if (!_currentPanel.ContainsKey(DisplayID))
        {
            _currentPanel.Add(DisplayID, null);
        }

        if (!panels.ContainsKey(DisplayID))
        {
            panels.Add(DisplayID, new ArrayListT<FguiPanelBase>());
        }

        if (!panelStack.ContainsKey(DisplayID))
        {
            panelStack.Add(DisplayID, new Stack<FguiPanelBase>());
        }

        if (orderIndex == OrderIndex.Normal)
        {
            switch (showType)
            {
                case ShowTypeEnum.CloseCurrent:
                    if (_currentPanel[DisplayID] != null)
                    {
                        Close(_currentPanel[DisplayID]);
                    }

                    break;
                case ShowTypeEnum.OverCurrent:
                    break;
                case ShowTypeEnum.HideCurrent:
                    if (_currentPanel[DisplayID] != null)
                    {
                        _currentPanel[DisplayID].OnHide();
                        _currentPanel[DisplayID].UpdateThisFrame = false;
                        _currentPanel[DisplayID].LateUpdateThisFrame = false;
                        _currentPanel[DisplayID].FGUIComponent.visible = false;
                        panelStack[DisplayID].Push(_currentPanel[DisplayID]);
                    }

                    break;
            }
        }
       LoadPackage(packageName);

        panel.PackageName = packageName;
        panel.PanelName = panelName;
        panel.DisplayID = DisplayID;

        var flag = rebornFrameRoot.Count == 0;
        GComponent root = null;
        for (var i = 0; i < rebornFrameRoot.Count; i++)
        {
            var order = int.Parse(rebornFrameRoot[i].name);
            if (orderIndex == order)
            {
                root = rebornFrameRoot[i];
                flag = false;
                break;
            }

            flag = orderIndex > order;
            if (!flag)
            {
                root = new GComponent();
                root.name = orderIndex.ToString();
                GRoot.inst.AddChildAt(root, i);
                rebornFrameRoot.Insert(i, root);
                break;
            }
        }

        if (flag)
        {
            root = new GComponent();
            root.name = orderIndex.ToString();
            GRoot.inst.AddChild(root);
            rebornFrameRoot.Add(root);
        }


        panel.FGUIComponent = UIPackage.CreateObject(packageName, panelName).asCom;
        GRoot.inst.SetContentScaleFactor((int)panel.FGUIComponent.width, (int)panel.FGUIComponent.height, UIContentScaler.ScreenMatchMode.MatchWidthOrHeight);
        if (GlobalControllerValue != -1)
        {
            _SetGlobalController(panel.FGUIComponent, GlobalController, GlobalControllerValue);
        }

        //将panel放到对应的root下
        //Debug.Log(panel.FGUIComponent.displayObject.gameObject.transform.parent.name);
        panel.FGUIComponent.SetSize(GRoot.inst.width,GRoot.inst.height);
        
       
        panel.FGUIComponent.fairyBatching = false;
        panel.FGUIComponent.displayObject.layer = 5 + DisplayID;
        
        panel.FGUIComponent.AddRelation(GRoot.inst, RelationType.Size);
        root.AddChild(panel.FGUIComponent);
        
        panels[DisplayID].Add(panel);
        if (orderIndex == OrderIndex.Normal)
        {
            panelStack[DisplayID].Push(panel);
            _currentPanel[DisplayID] = panel;
        }

        //多显示器
        if (displayCamera != null)
        {
            panel.FGUIComponent.container.renderCamera = displayCamera;
            panel.FGUIComponent.container.renderMode = RenderMode.ScreenSpaceCamera;
        }
        
        panel.Start(data);
        
        if(!panel.KeepFullScrenn)
            ChangePanelSizeByCurrentSpliteMode(panel);
        
        panel.OnShow();
        return panel;
    }

    public void Back(int DisplayID = 0)
    {
        if (_currentPanel.ContainsKey(DisplayID) && _currentPanel[DisplayID] != null)
        {
            Close(_currentPanel[DisplayID]);
            var next = GetNextToppestPanel(DisplayID);
            if (next != null)
            {
                _currentPanel[DisplayID] = next;
                _currentPanel[DisplayID].FGUIComponent.visible = true;
                _currentPanel[DisplayID].OnShow();
            }
            else
            {
                _currentPanel[DisplayID] = null;
            }
        }
    }

    FguiPanelBase GetNextToppestPanel(int DisplayID = 0)
    {
        while (panelStack[DisplayID].Count > 0)
        {
            var panel = panelStack[DisplayID].Pop();
            if (!panel.isClosed)
            {
                panelStack[DisplayID].Push(panel);
                return panel;
            }
        }

        return null;
    }

    /// <summary>
    /// 加载FGUI的包
    /// </summary>
    /// <param name="packageName">包名</param>
    /// <param name="times">加载次数</param>
    public void LoadPackage(string packageName,int times = 1)
    {
        if (!packageLoaded.ContainsKey(packageName))
        {
#if USE_AB
#if UNITY_EDITOR && !SIMULATE_PHONE 
            var package = UIPackage.AddPackage($"Assets/Arts/UI/{packageName}/{packageName}");
#else
            var package = UIPackage.AddPackage(ABPackageManager.Instance.LoadAssetBundle($"arts_ui_{packageName}"));
#endif
#else
            var package = UIPackage.AddPackage($"UI/{packageName}/{packageName}");
#endif
            packageLoaded.Add(packageName, times);
            packages.Add(packageName,package);
            //加载依赖
            foreach (var packageDependency in package.dependencies)
            {
                LoadPackage(packageDependency["name"]);
            }
        }
        else
        {
            packageLoaded[packageName] += times;
        }
    }
    
    public void RefreshSize()
    {
        var enumerator = panels.Values.GetEnumerator();
        while (enumerator.MoveNext())
        {
            var panelList = enumerator.Current;
            for (int i = 0; i < panelList.Count; i++)
            {
                var index = panelList[i].DisplayID;
                panelList[i].FGUIComponent.SetSize(ScreenSize[index].x, ScreenSize[index].y);
            }
        }
    }

    /// <summary>
    /// 关闭面板（如果有重名可能会误关）
    /// 警告！！！这是不安全的关闭方法，如果关闭的是当前面板请使用Back方法
    /// </summary>
    /// <param name="panelName"></param>
    public void Close(string packageName, string panelName)
    {
        var enumerator = panels.Values.GetEnumerator();
        while (enumerator.MoveNext())
        {
            var panelList = enumerator.Current;
            for (int i = 0; i < panelList.Count; i++)
            {
                var panel = panelList[i];
                if (panel.PackageName == packageName && panel.PanelName == panelName)
                {
                    Close(panel);
                }
            }
        }
    }

    /// <summary>
    /// 警告！！！这是不安全的关闭方法，如果关闭的是当前面板请使用Back方法
    /// </summary>
    /// <param name="panel"></param>
    public void Close(FguiPanelBase panel)
    {
        if (panel.isClosed)
            return;

        panels[panel.DisplayID].Remove(panel);
        panel.BaseClose();
        panel.FGUIComponent.Dispose();

        RemovePackage(panel.PackageName);
        panel.isClosed = true;
    }

    void RemovePackage(string packageName)
    {
        packageLoaded[packageName]--;
        if (packageLoaded[packageName] <= 0)
        {
            var package = packages[packageName];
            
            packageLoaded.Remove(packageName);
            packages.Remove(packageName);
            UIPackage.RemovePackage(packageName);
#if USE_AB && SIMULATE_PHONE
            ABPackageManager.Instance.UnLoadABPackage($"arts_ui_{packageName}");
#endif
            
            //移除依赖
            foreach (var packageDependency in package.dependencies)
            {
                RemovePackage(packageDependency["name"]);
            }
        }
    }
    
    
    
    public void CloseAll(int displayID)
    {
        if (panels.ContainsKey(displayID))
        {
            var panelList = panels[displayID];
            for (int i = panelList.Count - 1; i >= 0; i--)
            {
                var panel = panelList[i];
                Debug.Log(panel.PanelName);
                Close(panel);
            }

            _currentPanel[displayID] = null;
        }
    }

    public T GetPanel<T>() where T : class
    {
        var enumerator = panels.Values.GetEnumerator();
        while (enumerator.MoveNext())
        {
            var panelList = enumerator.Current;
            for (int i = 0; i < panelList.Count; i++)
            {
                if (panelList[i] is T)
                    return panelList[i] as T;
            }
        }

        return null;
    }

    public void SetGlobalController(string controllerName, int value)
    {
        var enumerator = panels.Values.GetEnumerator();
        while (enumerator.MoveNext())
        {
            var panelList = enumerator.Current;
            for (int i = 0; i < panelList.Count; i++)
            {
                _SetGlobalController(panelList[i].FGUIComponent, controllerName, value);
            }
        }

        GlobalController = controllerName;
        GlobalControllerValue = value;
    }

    private void _SetGlobalController(GComponent com, string ControllerName, int value)
    {
        var controller = com.GetController(ControllerName);
        if (controller != null)
        {
            controller.SetSelectedIndex(value);
        }

        for (var i = 0; i < com._children.Count; i++)
        {
            if (com._children[i] is GComponent gcom)
            {
                _SetGlobalController(gcom, ControllerName, value);
            }
        }
    }

    public FguiPanelBase GetPanel(int displayId, string packageName, string panelName)
    {
        var panelList = panels[displayId];
        for (int i = panelList.Count - 1; i >= 0; i--)
        {
            var panel = panelList[i];
            if (panel.PackageName == packageName && panel.PanelName == panelName)
            {
                return panel;
            }
        }

        return null;
    }

    public T GetPanel<T>(int displayId) where T : class
    {
        if (!panels.ContainsKey(displayId))
            return null;
        
        var panelList = panels[displayId];
        for (int i = panelList.Count - 1; i >= 0; i--)
        {
            if (panelList[i] is T)
                return panelList[i] as T;
        }

        return null;
    }
    
    public void Clear()
    {
        packageLoaded.Clear();
        packages.Clear();
        panels.Clear();
        panelStack.Clear();
        _currentPanel.Clear();
    }

    public (int,float,float) RedirectScreenPosition(float x, float y, bool reverseY = true){
        var halfWidth = Screen.width / 2;
        var halfHeight = Screen.height / 2;
        int splitIndex = 0;
        switch (CurrentSplitMode)
        {
            case SplitModeEnum.Single:
                break;
            case SplitModeEnum.Double:
                splitIndex = x > halfWidth ? 2 : 1;
                x = x % halfWidth;
                break;
            case SplitModeEnum.VerticalDouble:
                splitIndex = y > halfHeight ? 2 : 1;
                y %= halfHeight;
                break;
            case SplitModeEnum.UpTriple:
                splitIndex = y > halfHeight ? (x > halfWidth ? 3 : 2) : 1;
                if(y > halfHeight){
                    x %= halfWidth;
                    y %= halfHeight;
                }
                break;
            case SplitModeEnum.DownTriple:
                splitIndex = y > halfHeight ? 3 : (x > halfWidth ? 2 : 1);
                if(y > halfHeight){
                    y %= halfHeight;
                }else{
                    x %= halfWidth;
                }
                break;
            case SplitModeEnum.Quadruple:
                splitIndex = y > halfHeight ? (x > halfWidth ? 4 : 3) : (x > halfWidth ? 2 : 1);
                x %= halfWidth;
                y %= halfHeight;
                break;
        }
        if(reverseY){
            y = Screen.height - y;
        }
        return (splitIndex, x, y);
    }

    public (int,Vector2) RedirectScreenPosition(Vector2 stagePoint, bool reverseY = true)
    {
        var(splitIndex,x,y) = RedirectScreenPosition(stagePoint.x, stagePoint.y, reverseY);
        return (splitIndex, new Vector2(x,y));
    }
}

public enum ShowTypeEnum
{
    CloseCurrent, //关闭现在的面板
    OverCurrent, //在现在的面板上面叠加一层
    HideCurrent, //隐藏现在的
}


public static class OrderIndex
{
    public const int BackGround = 0;
    public const int Normal = 1;
    public const int Front = 2;
    public const int MostFront = 3;
    public const int MostMostFront = 4;
    public const int MostMostMostFront = 5;
}


public enum SplitModeEnum
{
    Single,//单屏
    Double,//双屏
    VerticalDouble,//竖屏双屏
    UpTriple,//上全屏下两屏
    DownTriple,//上两屏下全屏
    Quadruple,//四屏
}
