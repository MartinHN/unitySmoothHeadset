using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using System.Net;
using System;
using System.Linq;

using Klak.Spout;

public class SpoutCams : MonoBehaviour
{
    public GameObject spoutCamPrefab;
    public Camera viewCam;

    [System.Serializable]
    public class SpoutCam
    {
        public string name;
        public int hfov;
        public int hAngleFromMain;
        public int vAngleFromMain;

        public GameObject generateObject(SpoutCams cams)
        {
            var listO = cams.camList;
            GameObject go = Instantiate(cams.spoutCamPrefab, cams.transform.position, Quaternion.Euler(0, hAngleFromMain, vAngleFromMain));
            go.name = name;
            var texture = new RenderTexture(listO.resW, listO.resH, 24, RenderTextureFormat.ARGB32);

            var cam = go.GetComponent<Camera>();
            cam.targetTexture = texture;
            cam.fieldOfView = Camera.HorizontalToVerticalFieldOfView(hfov, cam.aspect);

            var sender = go.GetComponent<SpoutSender>();
            sender.sourceTexture = texture;
            sender.spoutName = name;

            return go;
        }
    };

    [System.Serializable]
    public class SpoutCamList
    {
        public String quality = "default";
        public float maxMillionPoints = 0;// 0 will leave current unchanged, usually 10;
        public int minNodeSize = 0;// 0 will leave current unchanged, usually 50;
        public int numNodePerFrame = 0;// 0 will leave current unchanged, usually 10;

        public int resW = 1920;
        public int resH = 1080;
        public int mainFOV = 0;
        public bool useDome = false;
        public float mainCamPointsWeight = 1;
        public List<SpoutCam> cams = new List<SpoutCam>();
        public static SpoutCamList fromJSON(string jsonString)
        {
            return JsonUtility.FromJson<SpoutCamList>(jsonString);
        }

        public string toJSON()
        {
            return JsonUtility.ToJson(this, true);
        }
    };

    public SpoutCamList camList = new SpoutCamList();
    public bool load = false;

    public bool write = false;
    public string confPath = "configuration/mappingConf.json";


    public bool lockRotation = true;

    // Start is called before the first frame update
    void Start()
    {
        if (viewCam == null)
        {
            viewCam = Camera.main;
        }

        curAngleY = viewCam.transform.eulerAngles.y;
        loadConfig();
    }

    void clear()
    {
        var tempList = transform.Cast<Transform>().ToList();
        foreach (var child in tempList)
        {
            DestroyImmediate(child.gameObject);
        }

    }

    void loadConfig()
    {
        clear();
        Debug.Log("loading " + confPath);
        string jsonfile;
        using (StreamReader reader = new StreamReader(confPath, Encoding.Default))
        {
            jsonfile = reader.ReadToEnd();
            reader.Close();
        }

        Debug.Log("read \n" + jsonfile);
        camList = SpoutCamList.fromJSON(jsonfile);

        // var pcSet = FindObjectOfType<BAPointCloudRenderer.CloudController.DynamicPointCloudSet>();
        // var vcams = pcSet.userCameras;
        // while (vcams.Count > 1) vcams.RemoveAt(vcams.Count - 1);

        foreach (var c in camList.cams)
        {
            var o = c.generateObject(this);
            o.transform.parent = transform;
            o.transform.localPosition = new Vector3(0, 0, 0);
            o.transform.localRotation = Quaternion.Euler(0, c.hAngleFromMain, c.vAngleFromMain);
            
        }
      
        if (camList.mainFOV > 0)
        {
            var cam = Camera.main;
            Camera.main.fieldOfView = Camera.HorizontalToVerticalFieldOfView(camList.mainFOV, cam.aspect);

        }

        if (camList.resW > 0)
        {
            Screen.SetResolution(camList.resW, camList.resH, Screen.fullScreenMode);
        }


        if (camList.quality != "default")
        {
            int level = QualitySettings.names.Length - 1;
            if (camList.quality == "medium")
                level = QualitySettings.names.Length / 2;
            else if (camList.quality == "low")
                level = 0;

            Debug.Log("applying qual :" + QualitySettings.names[level]);
            QualitySettings.SetQualityLevel(level, true);
        }


    }

    // Update is called once per frame
    void Update()
    {
        if (load || Input.GetKeyDown("r"))
        {
            load = false;
            loadConfig();
            Debug.Log(camList?.toJSON());
        }

        if (Input.GetKeyDown("escape"))
        {
            Screen.fullScreenMode = FullScreenMode.Windowed;
        }

        if (Input.GetKeyDown("f"))
        {
            Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
        }
        // if (camList.useDome && Input.GetKeyDown("d")){
        //     var domeCam = FindObjectOfType<DomeController>().projectionCamera;
        //     int domeDisp = domeCam.targetDisplay;
        //     setDomeDisplay( 1-domeDisp);
        // }
        if (write)
        {
            write = false;
            camList = new SpoutCamList();
            var cam = new SpoutCam();
            camList.cams.Add(cam);
            var json = camList.toJSON();
            Debug.Log(json);

        }
        if (lockRotation)
        {
            
            var vt = viewCam.transform;

            curAngleY %= 360;
            curAngleY += getYDiff() * Time.deltaTime * smoothRotation;
            transform.eulerAngles = new Vector3(0, curAngleY, 0);


            // be sure to stay in headset position
            transform.position = vt.position;

        }


    }

    float getYDiff()
    {
        float y = viewCam.transform.eulerAngles.y;
        float d = y - curAngleY;
        if (d > 180) d = -(360 - d);
        else if (d < -180) d = (360 + d);
        return d;
    }

   
    public float curAngleY = 0;

    public float smoothRotation = .1f;


}
