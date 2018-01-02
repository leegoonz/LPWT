//2016.4.10
//Authored by leegoonz.(Lee JungPyo / 李正彪)
//contact via leegoon73@gmail.com (EN) / leegoonz@163.com(CN)
//http://www.leegoonz.com

using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

//Author JP.Lee Feb/29/2016
namespace TIANYUUNITY
{
#pragma warning disable 618
#pragma warning disable 414
    class LightProbeBakedWeightTool : EditorWindow
    {
#region Public Variables

        public string GIDataname;
        public string sHSettingsName;
        public TextAsset bakedGIData;
        public SHWeightSettings sHWeightSettings;
        public float bakedGIWeight;
        public float[] bakedProbeWeight;
        public string curSceneName;
        public string headerTile = "LIGHT PROBES WEIGHT TOOL Ver.1.0";

#endregion

#region Private Variables

        GUIStyle headerStyle = new GUIStyle ();
        GUIStyle footerStyle = new GUIStyle ();
        GUIStyle bgBoxStyle = new GUIStyle ();
        GUIStyle titleAStyle = new GUIStyle ();
        GUIStyle titleBStyle = new GUIStyle ();
        GUIStyle titleCStyle = new GUIStyle ();
        GUIStyle buttonAStyle = new GUIStyle ();
        GUIStyle buttonBStyle = new GUIStyle ();
        GUIStyle buttonCStyle = new GUIStyle ();
        GUIStyle buttonDStyle = new GUIStyle ();
        GUIStyle buttonEStyle = new GUIStyle ();

        GUIStyle sliderTroughStyle = new GUIStyle ();
        GUIStyle sliderKnobStyle = new GUIStyle ();
        GUIStyle sectionStyle = new GUIStyle ();

        const float BAKED_PROBE_WEIGHT_ENTRY_HEIGHT = 25.0f;
        public int bakedProbeViewIdx;
        Vector2 bakedProbeWeightScrollPos;

#endregion

        [MenuItem ("Pangu/Artist Tools/Light Probe Weight Window %1", priority = 1)]
        //& is alt , % is ctrl , # is shift
        static void ShowLightProbeBakedWeightWindow ()
        {
            var lightProbeBakedWindow = EditorWindow.GetWindow<LightProbeBakedWeightTool> ("Probe Window");
            Vector2 size = new Vector2 (380, 285);
            lightProbeBakedWindow.minSize = size;
            lightProbeBakedWindow.maxSize = size;
            lightProbeBakedWindow.Show ();
        }

        void OnEnable ()
        {
            curSceneName = Application.loadedLevelName;
            GenerateStyles ();

            // Initialize baked probe weight data
            {
                bakedProbeWeight = new float[LightmapSettings.lightProbes.count];
                for (int i = 0; i < bakedProbeWeight.Length; ++i)
                {
                    bakedProbeWeight[i] = 1.0f;
                }
                bakedProbeViewIdx = -1;
                bakedProbeWeightScrollPos = new Vector2(0.0f, 0.0f);
                SceneView.RepaintAll();
            }

            SceneView.onSceneGUIDelegate += OnSceneGUI;
        }

        void OnDestroy()
        {
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
        }

        void OnSceneGUI(SceneView sceneView)
        {
            if (bakedProbeWeight != null)
            {
                int[] ids = new int[bakedProbeWeight.Length];
                for (int i = 0; i < ids.Length; ++i)
                {
                    ids[i] = GUIUtility.GetControlID(FocusType.Passive);
                }

                if (Event.current.type == EventType.Repaint || Event.current.type == EventType.layout)
                {
                    for (int i = 0; i < bakedProbeWeight.Length; ++i)
                    {
                        Vector3 lightProbeToViewPos = LightmapSettings.lightProbes.positions[i];
                        Color lightProbeToViewCol = Color.yellow * bakedProbeWeight[i]/2.0f + Color.red * (2 - bakedProbeWeight[i])/2.0f;
                        lightProbeToViewCol.a = bakedProbeViewIdx == i ? 0.7f : 0.3f;

                        Handles.color = lightProbeToViewCol;
                        Handles.SphereHandleCap(ids[i], lightProbeToViewPos, Quaternion.identity, 0.15f, Event.current.type);                     
                    }
                }
                else if (Event.current.type == EventType.mouseDown && Event.current.button == 0)
                {
                    for (int i = 0; i < bakedProbeWeight.Length; ++i)
                    {
                        if (HandleUtility.nearestControl == ids[i])
                        {
                            if (bakedProbeViewIdx != i)
                            {
                                bakedProbeViewIdx = i;
                                bakedProbeWeightScrollPos.y = BAKED_PROBE_WEIGHT_ENTRY_HEIGHT * bakedProbeViewIdx;
                                SceneView.RepaintAll();
                            }

                            Vector3 lightProbeToViewPos = LightmapSettings.lightProbes.positions[bakedProbeViewIdx];
                            SceneView.lastActiveSceneView.LookAt(lightProbeToViewPos);
                            break;
                        }
                    }
                }
            }
        }

        //List<Texture2D> textures = new List<Texture2D>();
        //Texture2D bg = null;

        void OnGUI ()
        {

            EditorGUILayout.LabelField (headerTile, headerStyle, GUILayout.Height (30));
            EditorGUILayout.BeginHorizontal ();
            EditorGUILayout.EndHorizontal ();
            EditorGUILayout.BeginVertical (GUILayout.Width (372));
            EditorGUILayout.HelpBox ("Baked lightprobes Spherical Harmonic 27 baked data value have store into the text assets.While editing to light scenes be sure that able to flexable correction to probes values through stored lightprobes sh_27 data with blending of weight data.", MessageType.Info);
            EditorGUILayout.EndHorizontal ();

            EditorGUILayout.BeginHorizontal ();
            GUILayout.Space (10.0f);
            EditorGUILayout.LabelField ("SAVE SH DATA:", titleAStyle, GUILayout.Width (100));

            GIDataname = EditorGUILayout.TextField (curSceneName + "_LP", GUILayout.Width (163), GUILayout.Height (25));

            if (GUILayout.Button ("SAVE", buttonBStyle, GUILayout.Width (96), GUILayout.Height (30)))
            {
                Debug.Log ("Pressed SaveProbeData to XML btn");
                if (GIDataname == string.Empty)
                {
                    Debug.LogWarning ("There is no baked GI name defined, add name to save");
                }
                else
                {
                    saveProbeDataXml (getSavePath (GIDataname, "xml"));
                    AssetDatabase.Refresh ();

                    // Initialize baked probe weight data
                    {
                        Array.Resize(ref bakedProbeWeight, LightmapSettings.lightProbes.count);
                        bakedProbeViewIdx = -1;
                        bakedProbeWeightScrollPos = new Vector2(0.0f, 0.0f);
                        SceneView.RepaintAll();
                    }
                }
            }


            EditorGUILayout.EndHorizontal ();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10.0f);
            EditorGUILayout.LabelField("STORED SH DATA:", titleAStyle, GUILayout.Width(100));
            bakedGIData = EditorGUILayout.ObjectField(bakedGIData, typeof(TextAsset), false, GUILayout.Width(163)) as TextAsset;
            EditorGUILayout.EndHorizontal ();

            if (GUILayout.Button ("RESTORE ORIGINAL SH", buttonAStyle, GUILayout.Width (379), GUILayout.Height (48)))
            {
                Debug.Log ("Restore Original SH");
                calculateProbes (1.0f);
            }

            EditorGUILayout.BeginHorizontal ();
            GUILayout.Space (10.0f);
            EditorGUILayout.LabelField ("SH WEIGHT:", titleAStyle, GUILayout.Width (100));
            bakedGIWeight = GUILayout.HorizontalSlider (bakedGIWeight, 0.0f, 2.0f, sliderTroughStyle, sliderKnobStyle, GUILayout.Width (180));

            EditorGUILayout.EndHorizontal ();

            if (bakedProbeWeight != null)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(10.0f);
                EditorGUILayout.LabelField("SH WEIGHT PER PROBE:", titleAStyle, GUILayout.Width(263));
                if (GUILayout.Button("RESET", buttonBStyle, GUILayout.Width(96), GUILayout.Height(30)))
                {
                    for (int i = 0; i < bakedProbeWeight.Length; ++i)
                    {
                        bakedProbeWeight[i] = 1.0f;
                    }
                    bakedProbeViewIdx = -1;
                    bakedProbeWeightScrollPos = new Vector2(0.0f, 0.0f);
                    SceneView.RepaintAll();
                }
                EditorGUILayout.EndHorizontal();

                bakedProbeWeightScrollPos = EditorGUILayout.BeginScrollView(bakedProbeWeightScrollPos, GUILayout.Width(372), GUILayout.Height(200));
                for (int i = 0; i < bakedProbeWeight.Length; ++i)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(10.0f);
                    if (bakedProbeViewIdx != i)
                    {
                        if (GUILayout.Button("PROBE " + i.ToString(), buttonDStyle, GUILayout.Width(65), GUILayout.Height(BAKED_PROBE_WEIGHT_ENTRY_HEIGHT)))
                        {
                            bakedProbeViewIdx = i;
                            Vector3 lightProbeToViewPos = LightmapSettings.lightProbes.positions[bakedProbeViewIdx];
                            SceneView.lastActiveSceneView.LookAt(lightProbeToViewPos);
                            SceneView.RepaintAll();
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("PROBE " + i.ToString(), buttonEStyle, GUILayout.Width(65), GUILayout.Height(BAKED_PROBE_WEIGHT_ENTRY_HEIGHT)))
                        {
                            Vector3 lightProbeToViewPos = LightmapSettings.lightProbes.positions[bakedProbeViewIdx];
                            SceneView.lastActiveSceneView.LookAt(lightProbeToViewPos);
                            SceneView.RepaintAll();
                        }
                    }
                    GUILayout.Space(30.0f);
                    float newSliderValue = GUILayout.HorizontalSlider(bakedProbeWeight[i], 0.0f, 2.0f, sliderTroughStyle, sliderKnobStyle, GUILayout.Width(180));
                    if (Math.Abs(bakedProbeWeight[i] - newSliderValue) > float.Epsilon)
                    {
                        if (bakedProbeViewIdx != i)
                        {
                            bakedProbeViewIdx = i;
                            Vector3 lightProbeToViewPos = LightmapSettings.lightProbes.positions[bakedProbeViewIdx];
                            SceneView.lastActiveSceneView.LookAt(lightProbeToViewPos);
                        }

                        bakedProbeWeight[i] = newSliderValue;
                        SceneView.RepaintAll();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(10.0f);
                EditorGUILayout.LabelField("SAVE SH WEIGHT:", titleAStyle, GUILayout.Width(100));
                sHSettingsName = EditorGUILayout.TextField(curSceneName + "_LPW", GUILayout.Width(163), GUILayout.Height(25));
                if (GUILayout.Button("SAVE", buttonBStyle, GUILayout.Width(96), GUILayout.Height(30)))
                {
                    SHWeightSettings sHSettingsAsset = AssetDatabase.LoadAssetAtPath(getSavePath(sHSettingsName, "asset"), typeof(SHWeightSettings)) as SHWeightSettings;
                    if (sHSettingsAsset == null)
                    {
                        SHWeightSettings asset = CreateInstance<SHWeightSettings>();
                        asset.bakedGIWeight = bakedGIWeight;
                        asset.bakedProbeWeight = new float[bakedProbeWeight.Length];
                        Array.Copy(bakedProbeWeight, asset.bakedProbeWeight, bakedProbeWeight.Length);
                        AssetDatabase.CreateAsset(asset, getSavePath(sHSettingsName, "asset"));
                    }
                    else
                    {
                        sHSettingsAsset.bakedGIWeight = bakedGIWeight;
                        sHSettingsAsset.bakedProbeWeight = new float[bakedProbeWeight.Length];
                        Array.Copy(bakedProbeWeight, sHSettingsAsset.bakedProbeWeight, bakedProbeWeight.Length);
                    }
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(10.0f);
                EditorGUILayout.LabelField("STORED SH WEIGHT:", titleAStyle, GUILayout.Width(100));
                sHWeightSettings = EditorGUILayout.ObjectField(sHWeightSettings, typeof(SHWeightSettings), false, GUILayout.Width(163)) as SHWeightSettings;
                if (GUILayout.Button("LOAD", buttonBStyle, GUILayout.Width(96), GUILayout.Height(30)))
                {
                    bakedGIWeight = sHWeightSettings.bakedGIWeight;
                    bakedProbeWeight = new float[sHWeightSettings.bakedProbeWeight.Length];
                    Array.Copy(sHWeightSettings.bakedProbeWeight, bakedProbeWeight, sHWeightSettings.bakedProbeWeight.Length);
                    bakedProbeViewIdx = -1;
                    bakedProbeWeightScrollPos = new Vector2(0.0f, 0.0f);
                    SceneView.RepaintAll();
                }
                EditorGUILayout.EndHorizontal();
            }

            //process original sh with multiply GI Weight
            if (GUILayout.Button ("LIGHT PROBES WEIGHT PROCESS", buttonAStyle, GUILayout.Width (379), GUILayout.Height (48)))
            {
                calculateProbes (bakedGIWeight, bakedProbeWeight);
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty (this);
            }

            EditorGUILayout.BeginVertical (bgBoxStyle);
            //Draw Footer Image
            EditorGUILayout.LabelField ("COPYRIGHT ALL RIGHT RESERVED JP.LEE / leegoonz@163.com", footerStyle, GUILayout.Height (20));

            EditorGUILayout.EndVertical ();
            Repaint ();
        }

        void calculateProbes (float bakedGIWeight, float[] bakedGIWeightPerProbe = null)
        {

            SphericalHarmonicsL2[] bakedProbes = LightmapSettings.lightProbes.bakedProbes;
            int probesCount = LightmapSettings.lightProbes.count;
            for (int i = 0; i < probesCount; i++)
            {
                bakedProbes [i].Clear ();
            }
            if (bakedGIData == null)
            {
                Debug.LogError ("null reference for baked GI data");
                return;
            }
            bakedProbes = assignData2bakedProbe (loadProbeDataXml(AssetDatabase.GetAssetPath(bakedGIData)), bakedGIWeight, bakedGIWeightPerProbe, bakedProbes, probesCount);
            LightmapSettings.lightProbes.bakedProbes = bakedProbes;
        }

        static void GetProbeInformation (List<List<float>> probedata)
        {
            SphericalHarmonicsL2[] bakedProbes = LightmapSettings.lightProbes.bakedProbes;

            int probeCount = LightmapSettings.lightProbes.count;
            //get all the coeffiencts
            for (int i = 0; i < probeCount; i++)
            {
                List<float> probeCoefficient = new List<float> ();
                for (int j = 0; j < 3; j++)
                {
                    for (int k = 0; k < 9; k++)
                    {
                        probeCoefficient.Add (bakedProbes [i] [j, k]);
                    }
                }
                probedata.Add (probeCoefficient);
            }
        }

        public static void saveProbeDataXml (string filepath)
        {
            List<List<float>> probedata = new List<List<float>> ();
            GetProbeInformation (probedata);
            //string filepath = Application.dataPath+@"/probeData/test_green.xml";
            XmlDocument xmldoc = new XmlDocument ();
            XmlElement rootNode = xmldoc.CreateElement ("probedata");
            xmldoc.AppendChild (rootNode);
            for (int i = 0; i < probedata.Count; i++)
            {
                string probename = "probe" + i.ToString ();
                XmlElement probeNode = xmldoc.CreateElement (probename); // create the rotation node.
                rootNode.AppendChild (probeNode);
                for (int j = 0; j < probedata [i].Count; j++)
                {
                    XmlElement coefNode = xmldoc.CreateElement ("coefficient"); // create the x node.
                    coefNode.InnerText = (probedata [i] [j]).ToString (); // apply to the node text the values of the variable.
                    probeNode.AppendChild (coefNode);
                }
            }
            xmldoc.Save (filepath); // save file.
        }

        public static List<List<float>> loadProbeDataXml (string filepath)
        {
            //string filepath = Application.dataPath+@"/probeData/test_blue.xml";
            XmlDocument xmldoc = new XmlDocument ();
            List<List<float>> tempProbeData = new List<List<float>> ();
            xmldoc.Load (filepath);
            XmlNodeList rootNodeList = xmldoc.GetElementsByTagName ("probedata");
            foreach (XmlNode rootNode in rootNodeList)
            {
                XmlNodeList probeNodeList = rootNode.ChildNodes;
                foreach (XmlNode probeNode in probeNodeList)
                {
                    List<float> probeCoefficient = new List<float> ();
                    XmlNodeList coefList = probeNode.ChildNodes;
                    foreach (XmlNode coefNode in coefList)
                    {
                        //print (coefNode.InnerText);
                        float coeffcient = float.Parse (coefNode.InnerText);
                        probeCoefficient.Add (coeffcient);
                    }
                    tempProbeData.Add (probeCoefficient);
                }
            }
            return tempProbeData;
        }

        //read data from probeDataXML and assign them to the baked probe
        public SphericalHarmonicsL2[] assignData2bakedProbe (List<List<float>> tempProbeData, float GIweight, float[] GIWeightPerProbe, SphericalHarmonicsL2[] bakedProbes, int probeCount)
        {
            for (int i = 0; i < probeCount; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    for (int k = 0; k < 9; k++)
                    {
                        bakedProbes [i] [j, k] += tempProbeData [i] [9 * j + k] * GIweight;
                    }
                }
            }

            if (GIWeightPerProbe != null)
            {
                for (int i = 0; i < probeCount; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        for (int k = 0; k < 9; k++)
                        {
                            bakedProbes[i][j, k] *= GIWeightPerProbe[i];
                        }
                    }
                }
            }

            return bakedProbes;
        }

        void bakedGIList (TextAsset bakedGIData, float bakedGIWeight)
        {

            TextAsset tempbakedGIData = new TextAsset ();
            float tempbakedGIWeight = new float ();
            bakedGIData = tempbakedGIData;
            bakedGIWeight = tempbakedGIWeight;

        }


        string getXMLPath (TextAsset GIdata)
        {
            Debug.Log (AssetDatabase.GetAssetPath (GIdata));
            return (AssetDatabase.GetAssetPath (GIdata));
            //string filepath = Application.dataPath+@"/probeData/test_blue.xml";
        }

        string getSavePath (string GIDataname, string ExtensionName)
        {
            string scenePath = EditorApplication.currentScene;
            scenePath = scenePath.Substring (0, scenePath.Length - 6);
            string savepath = scenePath + @"/" + GIDataname + "." + ExtensionName;
            return savepath;
        }

        void GenerateStyles ()
        {
            //Header Style
            headerStyle.normal.background = (Texture2D)Resources.Load ("bg_box_002");
            headerStyle.font = (Font)Resources.Load ("Fonts/BitstreamVeraSansMono");
            headerStyle.fontSize = 17;
            headerStyle.normal.textColor = Color.white;
            headerStyle.alignment = TextAnchor.MiddleCenter;
            headerStyle.overflow = new RectOffset (1, 1, 1, 1);
            //headerStyle.border = new RectOffset(10, 10, 10, 10);
            //headerStyle.contentOffset = new Vector2(0f, 5f);


            //BG Box Style
            bgBoxStyle.normal.background = (Texture2D)Resources.Load ("bg_box_001");
            bgBoxStyle.border = new RectOffset (10, 10, 10, 10);
            bgBoxStyle.overflow = new RectOffset (1, 1, 1, 1);

            //footer style
            footerStyle.normal.background = (Texture2D)Resources.Load ("bg_box_002");
            footerStyle.font = (Font)Resources.Load ("Fonts/BitstreamVeraSansMono");
            footerStyle.fontSize = 10;
            footerStyle.normal.textColor = Color.white;
            footerStyle.alignment = TextAnchor.LowerRight;
            //footerStyle.border = new RectOffset(10, 10, 10, 10);
            //footerStyle.contentOffset = new Vector2(-10f, -5f);
            footerStyle.overflow = new RectOffset (1, 1, 1, 1);

            //TitleA Style
            titleAStyle.font = (Font)Resources.Load ("Fonts/BitstreamVeraSansMono");
            titleAStyle.fontSize = 10;
            titleAStyle.normal.textColor = Color.white;

            //TitleB Style
            titleBStyle.font = (Font)Resources.Load ("Fonts/BitstreamVeraSansMono");
            titleBStyle.fontSize = 12;
            titleBStyle.normal.textColor = Color.white;

            //TitleC Style
            titleCStyle.font = (Font)Resources.Load ("Fonts/BitstreamVeraSansMono");
            titleCStyle.fontSize = 12;
            titleCStyle.normal.textColor = Color.white;

            //Button A Style
            buttonAStyle.normal.background = (Texture2D)Resources.Load ("button_bg_001");
            buttonAStyle.hover.background = (Texture2D)Resources.Load ("button_bg_001_hover");
            buttonAStyle.font = (Font)Resources.Load ("Fonts/BitstreamVeraSansMono");
            buttonAStyle.fontSize = 14;
            buttonAStyle.normal.textColor = Color.white;
            buttonAStyle.alignment = TextAnchor.MiddleCenter;
            //buttonAStyle.contentOffset = new Vector2(0f, 2f);
            //buttonAStyle.border = new RectOffset(20, 20, 20, 20);

            //Button B Style
            buttonBStyle.normal.background = (Texture2D)Resources.Load ("button_001");
            buttonBStyle.hover.background = (Texture2D)Resources.Load ("button_001_hover");
            buttonBStyle.font = (Font)Resources.Load ("Fonts/BitstreamVeraSansMono");
            buttonBStyle.fontSize = 12;
            buttonBStyle.normal.textColor = Color.white;
            buttonBStyle.alignment = TextAnchor.MiddleCenter;
            //buttonBStyle.contentOffset = new Vector2(0f, 2f);
            buttonBStyle.border = new RectOffset (20, 20, 20, 20);

            //Button C Style
            buttonCStyle.normal.background = (Texture2D)Resources.Load ("button_002");
            buttonCStyle.hover.background = (Texture2D)Resources.Load ("button_002_hover");
            buttonCStyle.font = (Font)Resources.Load ("Fonts/BitstreamVeraSansMono");
            buttonCStyle.fontSize = 14;
            buttonCStyle.normal.textColor = Color.black;
            buttonCStyle.alignment = TextAnchor.MiddleCenter;
            //buttonBStyle.contentOffset = new Vector2(0f, 2f);
            buttonCStyle.border = new RectOffset (20, 20, 20, 20);

            //Button D Style
            buttonDStyle.normal.background = (Texture2D)Resources.Load("button_001");
            buttonDStyle.hover.background = (Texture2D)Resources.Load("button_001_hover");
            buttonDStyle.font = (Font)Resources.Load("Fonts/BitstreamVeraSansMono");
            buttonDStyle.fontSize = 10;
            buttonDStyle.normal.textColor = Color.white;
            buttonDStyle.alignment = TextAnchor.MiddleCenter;
            buttonDStyle.border = new RectOffset(20, 20, 20, 20);

            //Button E Style
            buttonEStyle.normal.background = (Texture2D)Resources.Load("button_001");
            buttonEStyle.hover.background = (Texture2D)Resources.Load("button_001_hover");
            buttonEStyle.font = (Font)Resources.Load("Fonts/BitstreamVeraSansMono");
            buttonEStyle.fontSize = 10;
            buttonEStyle.normal.textColor = (Color.yellow + Color.red)/2.0f;
            buttonEStyle.alignment = TextAnchor.MiddleCenter;
            buttonEStyle.border = new RectOffset(20, 20, 20, 20);

            //Slider Trough
            sliderTroughStyle.normal.background = (Texture2D)Resources.Load ("slider_trough_001");
            sliderTroughStyle.border = new RectOffset (5, 5, 0, 0);
            sliderTroughStyle.overflow = new RectOffset (0, 0, -2, -3);
            sliderTroughStyle.padding = new RectOffset (-1, -1, 0, 0);
            sliderTroughStyle.stretchWidth = true;
            sliderTroughStyle.fixedHeight = 12;
            sliderTroughStyle.margin = new RectOffset (4, 4, 4, 4);

            //Slider Knob
            sliderKnobStyle.normal.background = (Texture2D)Resources.Load ("slider_knob_001");
            sliderKnobStyle.hover.background = (Texture2D)Resources.Load ("slider_knob_001_hover");
            sliderKnobStyle.border = new RectOffset (1, 1, 1, 1);
            sliderKnobStyle.overflow = new RectOffset (-1, -1, 1, 1);
            sliderKnobStyle.padding = new RectOffset (10, 10, 10, 10);

        }
    }
#pragma warning restore 618
#pragma warning restore 414
}


