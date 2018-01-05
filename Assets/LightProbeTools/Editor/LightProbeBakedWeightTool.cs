//2016.4.10
//Authored by leegoonz.(Lee JungPyo / 李正彪)
//contact via leegoon73@gmail.com (EN) / leegoonz@163.com(CN)
//http://www.leegoonz.com

using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using System.Xml;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

//Author JP.Lee Feb/29/2016
namespace TIANYUUNITY {
    class LightProbeBakedWeightTool : EditorWindow {
        #region Public Variables

        private string GIDataname;
        private string sHSettingsName;
        private TextAsset bakedGIData;
        private SHWeightSettings sHWeightSettings;
        private float bakedGIWeight = 1;
        private float[] bakedProbeWeight;
        private string curSceneName;
        const string headerTile = "LIGHT PROBES WEIGHT TOOL Ver.1.0";
        const string headerMessage = "Baked lightprobes Spherical Harmonic 27 baked data value have store into the text assets.While editing to light scenes be sure that able to flexable correction to probes values through stored lightprobes sh_27 data with blending of weight data.";

        #endregion

        #region Private Variables

        readonly GUIStyle headerStyle = new GUIStyle("LODLevelNotifyText");// new GUIStyle("TL Selection H2");
        readonly GUIStyle footerStyle = new GUIStyle("GUIEditor.BreadcrumbLeft");

        const float BAKED_PROBE_WEIGHT_ENTRY_HEIGHT = 25.0f;
        private int bakedProbeViewIdx;
        Vector2 bakedProbeWeightScrollPos;

        GUIStyle probeButtonActive;
        readonly GUILayoutOption probeBtnWidth = GUILayout.Width(100);
        readonly GUILayoutOption weightWidth = GUILayout.Width(130);
        readonly GUILayoutOption labelWidth = GUILayout.Width(150);
        readonly GUILayoutOption titleHeight = GUILayout.Height(30);
        readonly private GUIContent labeCopyright = new GUIContent("COPYRIGHT ALL RIGHT RESERVED JP.LEE / leegoonz@163.com");
        GUILayoutOption copyrightWidth;
        #endregion

        [MenuItem("Pangu/Artist Tools/Light Probe Weight Window %1", priority = 1)]
        //& is alt , % is ctrl , # is shift
        static void ShowLightProbeBakedWeightWindow() {
            var lightProbeBakedWindow = EditorWindow.GetWindow<LightProbeBakedWeightTool>("Probe Window");
            lightProbeBakedWindow.minSize = Vector2.one * 460;
            lightProbeBakedWindow.Show();
        }

        void OnEnable() {
            curSceneName = SceneManager.GetSceneAt(0).name;
            GenerateStyles();

            // Initialize baked probe weight data
            {
                bakedProbeWeight = new float[LightmapSettings.lightProbes.count];
                for (int i = 0; i < bakedProbeWeight.Length; ++i) {
                    bakedProbeWeight[i] = 1.0f;
                }
                bakedProbeViewIdx = -1;
                bakedProbeWeightScrollPos = new Vector2(0.0f, 0.0f);
                SceneView.RepaintAll();
            }

            SceneView.onSceneGUIDelegate += OnSceneGUI;
        }

        void OnDestroy() {
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
        }

        void OnSceneGUI(SceneView sceneView) {
            if (bakedProbeWeight != null) {
                int[] ids = new int[bakedProbeWeight.Length];
                for (int i = 0; i < ids.Length; ++i) {
                    ids[i] = GUIUtility.GetControlID(FocusType.Passive);
                }

                if (Event.current.type == EventType.Repaint || Event.current.type == EventType.Layout) {
                    for (int i = 0; i < bakedProbeWeight.Length; ++i) {
                        Vector3 lightProbeToViewPos = LightmapSettings.lightProbes.positions[i];
                        Color lightProbeToViewCol = Color.yellow * bakedProbeWeight[i] / 2.0f + Color.red * (2 - bakedProbeWeight[i]) / 2.0f;
                        lightProbeToViewCol.a = bakedProbeViewIdx == i ? 0.7f : 0.3f;

                        Handles.color = lightProbeToViewCol;
                        Handles.SphereHandleCap(ids[i], lightProbeToViewPos, Quaternion.identity, 0.15f, Event.current.type);
                    }
                } else if (Event.current.type == EventType.MouseDown && Event.current.button == 0) {
                    for (int i = 0; i < bakedProbeWeight.Length; ++i) {
                        if (HandleUtility.nearestControl == ids[i]) {
                            if (bakedProbeViewIdx != i) {
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

        void OnGUI() {
            OnGUI_Header();
            OnGUI_TopMenu();

            if (bakedProbeWeight != null) {
                OnGUI_Body_Top();
                OnGUI_Body_ProbeList();
                OnGUI_Body_Bottom();
            }

            OnGUI_Footer();
            Repaint();
        }

        private void OnGUI_Header() {
            EditorGUILayout.LabelField(headerTile, headerStyle, titleHeight);
            EditorGUILayout.HelpBox(headerMessage, MessageType.Info);
        }

        private void OnGUI_TopMenu() {
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Space(10.0f);
                EditorGUILayout.LabelField("SAVE SH DATA:", labelWidth);

                GIDataname = EditorGUILayout.TextField(curSceneName + "_LP");

                if (GUILayout.Button("SAVE")) {
                    Debug.Log("Pressed SaveProbeData to XML btn");
                    if (GIDataname == string.Empty) {
                        Debug.LogWarning("There is no baked GI name defined, add name to save");
                    } else {
                        saveProbeDataXml(getSavePath(GIDataname, "xml"));
                        AssetDatabase.Refresh();

                        // Initialize baked probe weight data
                        {
                            Array.Resize(ref bakedProbeWeight, LightmapSettings.lightProbes.count);
                            bakedProbeViewIdx = -1;
                            bakedProbeWeightScrollPos = new Vector2(0.0f, 0.0f);
                            SceneView.RepaintAll();
                        }
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Space(10.0f);
                EditorGUILayout.LabelField("STORED SH DATA:", labelWidth);
                bakedGIData = EditorGUILayout.ObjectField(bakedGIData, typeof(TextAsset), false) as TextAsset;
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("RESTORE ORIGINAL SH", GUILayout.Height(48))) {
                Debug.Log("Restore Original SH");
                calculateProbes(1.0f);
            }

            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Space(10.0f);
                EditorGUILayout.LabelField("SH WEIGHT:", weightWidth);
                bakedGIWeight = GUILayout.HorizontalSlider(bakedGIWeight, 0.0f, 2.0f);
                GUILayout.Box("", GUIStyle.none, GUILayout.Width(16));
            }
            EditorGUILayout.EndHorizontal();
        }

        private void OnGUI_Body_Top() {
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Space(10.0f);
                EditorGUILayout.LabelField("SH WEIGHT PER PROBE:", labelWidth);
                if (GUILayout.Button("RESET")) {
                    for (int i = 0; i < bakedProbeWeight.Length; ++i) {
                        bakedProbeWeight[i] = 1.0f;
                    }
                    bakedProbeViewIdx = -1;
                    bakedProbeWeightScrollPos = new Vector2(0.0f, 0.0f);
                    SceneView.RepaintAll();
                }
                GUILayout.Box("", GUIStyle.none, GUILayout.Width(16));
            }
            EditorGUILayout.EndHorizontal();
        }

        private void OnGUI_Body_ProbeList() {
            bakedProbeWeightScrollPos = EditorGUILayout.BeginScrollView(bakedProbeWeightScrollPos);
            {
                for (int i = 0; i < bakedProbeWeight.Length; ++i) {
                    EditorGUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(10.0f);
                        if (bakedProbeViewIdx != i) {
                            if (GUILayout.Button("PROBE " + i.ToString(), probeBtnWidth, GUILayout.Height(BAKED_PROBE_WEIGHT_ENTRY_HEIGHT))) {
                                bakedProbeViewIdx = i;
                                Vector3 lightProbeToViewPos = LightmapSettings.lightProbes.positions[bakedProbeViewIdx];
                                SceneView.lastActiveSceneView.LookAt(lightProbeToViewPos);
                                SceneView.RepaintAll();
                            }
                        } else {
                            if (GUILayout.Button("PROBE " + i.ToString(), probeButtonActive, probeBtnWidth, GUILayout.Height(BAKED_PROBE_WEIGHT_ENTRY_HEIGHT))) {
                                Vector3 lightProbeToViewPos = LightmapSettings.lightProbes.positions[bakedProbeViewIdx];
                                SceneView.lastActiveSceneView.LookAt(lightProbeToViewPos);
                                SceneView.RepaintAll();
                            }
                        }
                        GUILayout.Space(30.0f);
                        float newSliderValue = GUILayout.HorizontalSlider(bakedProbeWeight[i], 0.0f, 2.0f);
                        if (Math.Abs(bakedProbeWeight[i] - newSliderValue) > float.Epsilon) {
                            if (bakedProbeViewIdx != i) {
                                bakedProbeViewIdx = i;
                                Vector3 lightProbeToViewPos = LightmapSettings.lightProbes.positions[bakedProbeViewIdx];
                                SceneView.lastActiveSceneView.LookAt(lightProbeToViewPos);
                            }

                            bakedProbeWeight[i] = newSliderValue;
                            SceneView.RepaintAll();
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void OnGUI_Body_Bottom() {
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Space(10.0f);
                EditorGUILayout.LabelField("SAVE SH WEIGHT:", labelWidth);
                sHSettingsName = EditorGUILayout.TextField(curSceneName + "_LPW", GUILayout.Width(163));
                if (GUILayout.Button("SAVE")) {
                    SHWeightSettings sHSettingsAsset = AssetDatabase.LoadAssetAtPath(getSavePath(sHSettingsName, "asset"), typeof(SHWeightSettings)) as SHWeightSettings;
                    if (sHSettingsAsset == null) {
                        SHWeightSettings asset = CreateInstance<SHWeightSettings>();
                        asset.bakedGIWeight = bakedGIWeight;
                        asset.bakedProbeWeight = new float[bakedProbeWeight.Length];
                        Array.Copy(bakedProbeWeight, asset.bakedProbeWeight, bakedProbeWeight.Length);
                        AssetDatabase.CreateAsset(asset, getSavePath(sHSettingsName, "asset"));
                    } else {
                        sHSettingsAsset.bakedGIWeight = bakedGIWeight;
                        sHSettingsAsset.bakedProbeWeight = new float[bakedProbeWeight.Length];
                        Array.Copy(bakedProbeWeight, sHSettingsAsset.bakedProbeWeight, bakedProbeWeight.Length);
                    }
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Space(10.0f);
                EditorGUILayout.LabelField("STORED SH WEIGHT:", labelWidth);
                sHWeightSettings = EditorGUILayout.ObjectField(sHWeightSettings, typeof(SHWeightSettings), false, GUILayout.Width(163)) as SHWeightSettings;
                if (GUILayout.Button("LOAD")) {
                    bakedGIWeight = sHWeightSettings.bakedGIWeight;
                    bakedProbeWeight = new float[sHWeightSettings.bakedProbeWeight.Length];
                    Array.Copy(sHWeightSettings.bakedProbeWeight, bakedProbeWeight, sHWeightSettings.bakedProbeWeight.Length);
                    bakedProbeViewIdx = -1;
                    bakedProbeWeightScrollPos = new Vector2(0.0f, 0.0f);
                    SceneView.RepaintAll();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void OnGUI_Footer() {
            //process original sh with multiply GI Weight
            if (GUILayout.Button("LIGHT PROBES WEIGHT PROCESS", GUILayout.Height(48))) {
                calculateProbes(bakedGIWeight, bakedProbeWeight);
            }

            if (GUI.changed) {
                EditorUtility.SetDirty(this);
            }

            //Draw Footer Image

            EditorGUILayout.LabelField(labeCopyright, footerStyle, copyrightWidth);
        }

        void calculateProbes(float bakedGIWeight, float[] bakedGIWeightPerProbe = null) {

            SphericalHarmonicsL2[] bakedProbes = LightmapSettings.lightProbes.bakedProbes;
            int probesCount = LightmapSettings.lightProbes.count;
            for (int i = 0; i < probesCount; i++) {
                bakedProbes[i].Clear();
            }
            if (bakedGIData == null) {
                Debug.LogError("null reference for baked GI data");
                return;
            }
            bakedProbes = assignData2bakedProbe(loadProbeDataXml(AssetDatabase.GetAssetPath(bakedGIData)), bakedGIWeight, bakedGIWeightPerProbe, bakedProbes, probesCount);
            LightmapSettings.lightProbes.bakedProbes = bakedProbes;
        }

        static void GetProbeInformation(List<List<float>> probedata) {
            SphericalHarmonicsL2[] bakedProbes = LightmapSettings.lightProbes.bakedProbes;

            int probeCount = LightmapSettings.lightProbes.count;
            //get all the coeffiencts
            for (int i = 0; i < probeCount; i++) {
                List<float> probeCoefficient = new List<float>();
                for (int j = 0; j < 3; j++) {
                    for (int k = 0; k < 9; k++) {
                        probeCoefficient.Add(bakedProbes[i][j, k]);
                    }
                }
                probedata.Add(probeCoefficient);
            }
        }

        public static void saveProbeDataXml(string filepath) {
            List<List<float>> probedata = new List<List<float>>();
            GetProbeInformation(probedata);
            //string filepath = Application.dataPath+@"/probeData/test_green.xml";
            XmlDocument xmldoc = new XmlDocument();
            XmlElement rootNode = xmldoc.CreateElement("probedata");
            xmldoc.AppendChild(rootNode);
            for (int i = 0; i < probedata.Count; i++) {
                string probename = "probe" + i.ToString();
                XmlElement probeNode = xmldoc.CreateElement(probename); // create the rotation node.
                rootNode.AppendChild(probeNode);
                for (int j = 0; j < probedata[i].Count; j++) {
                    XmlElement coefNode = xmldoc.CreateElement("coefficient"); // create the x node.
                    coefNode.InnerText = (probedata[i][j]).ToString(); // apply to the node text the values of the variable.
                    probeNode.AppendChild(coefNode);
                }
            }
            xmldoc.Save(filepath); // save file.
        }

        public static List<List<float>> loadProbeDataXml(string filepath) {
            //string filepath = Application.dataPath+@"/probeData/test_blue.xml";
            XmlDocument xmldoc = new XmlDocument();
            List<List<float>> tempProbeData = new List<List<float>>();
            xmldoc.Load(filepath);
            XmlNodeList rootNodeList = xmldoc.GetElementsByTagName("probedata");
            foreach (XmlNode rootNode in rootNodeList) {
                XmlNodeList probeNodeList = rootNode.ChildNodes;
                foreach (XmlNode probeNode in probeNodeList) {
                    List<float> probeCoefficient = new List<float>();
                    XmlNodeList coefList = probeNode.ChildNodes;
                    foreach (XmlNode coefNode in coefList) {
                        //print (coefNode.InnerText);
                        float coeffcient = float.Parse(coefNode.InnerText);
                        probeCoefficient.Add(coeffcient);
                    }
                    tempProbeData.Add(probeCoefficient);
                }
            }
            return tempProbeData;
        }

        //read data from probeDataXML and assign them to the baked probe
        public SphericalHarmonicsL2[] assignData2bakedProbe(List<List<float>> tempProbeData, float GIweight, float[] GIWeightPerProbe, SphericalHarmonicsL2[] bakedProbes, int probeCount) {
            for (int i = 0; i < probeCount; i++) {
                for (int j = 0; j < 3; j++) {
                    for (int k = 0; k < 9; k++) {
                        bakedProbes[i][j, k] += tempProbeData[i][9 * j + k] * GIweight;
                    }
                }
            }

            if (GIWeightPerProbe != null) {
                for (int i = 0; i < probeCount; i++) {
                    for (int j = 0; j < 3; j++) {
                        for (int k = 0; k < 9; k++) {
                            bakedProbes[i][j, k] *= GIWeightPerProbe[i];
                        }
                    }
                }
            }

            return bakedProbes;
        }

        void bakedGIList(TextAsset bakedGIData, float bakedGIWeight) {
            TextAsset tempbakedGIData = new TextAsset();
            float tempbakedGIWeight = new float();
            bakedGIData = tempbakedGIData;
            bakedGIWeight = tempbakedGIWeight;
        }


        string getXMLPath(TextAsset GIdata) {
            Debug.Log(AssetDatabase.GetAssetPath(GIdata));
            return (AssetDatabase.GetAssetPath(GIdata));
            //string filepath = Application.dataPath+@"/probeData/test_blue.xml";
        }

        string getSavePath(string GIDataname, string ExtensionName) {
            string scenePath = SceneManager.GetSceneAt(0).path;
            scenePath = scenePath.Substring(0, scenePath.Length - 6);
            string savepath = scenePath + @"/" + GIDataname + "." + ExtensionName;
            return savepath;
        }

        void GenerateStyles() {
            copyrightWidth = GUILayout.Width(footerStyle.CalcSize(labeCopyright).x);

            probeButtonActive = new GUIStyle("button");
            probeButtonActive.normal = probeButtonActive.onActive;
        }
    }
}