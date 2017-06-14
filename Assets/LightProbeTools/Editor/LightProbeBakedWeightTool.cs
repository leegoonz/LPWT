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
        public TextAsset bakedGIData;
        public float bakedGIWeight;
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

        GUIStyle sliderTroughStyle = new GUIStyle ();
        GUIStyle sliderKnobStyle = new GUIStyle ();
        GUIStyle sectionStyle = new GUIStyle ();

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
            EditorGUILayout.LabelField ("Save SH data:", titleAStyle, GUILayout.Width (100));

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
                    saveProbeDataXml (getSavePath (GIDataname));
                    AssetDatabase.Refresh ();
                }
            }


            EditorGUILayout.EndHorizontal ();
            if (GUILayout.Button ("RESTORE ORIGINAL SH", buttonAStyle, GUILayout.Width (379), GUILayout.Height (48)))
            {
                Debug.Log ("Restore Original SH");
                calculateProbes (1.0f);
            }

            EditorGUILayout.BeginHorizontal ();
            GUILayout.Space (10.0f);
            EditorGUILayout.LabelField ("STORED SH:", titleAStyle, GUILayout.Width (100));
            bakedGIData = EditorGUILayout.ObjectField (bakedGIData, typeof(TextAsset), false) as TextAsset;
            EditorGUILayout.EndHorizontal ();

            EditorGUILayout.BeginHorizontal ();
            GUILayout.Space (10.0f);
            EditorGUILayout.LabelField ("SH WEIGHT:", titleAStyle, GUILayout.Width (100));
            bakedGIWeight = GUILayout.HorizontalSlider (bakedGIWeight, 0.0f, 2.0f, sliderTroughStyle, sliderKnobStyle, GUILayout.Width (180));

            EditorGUILayout.EndHorizontal ();


            //process original sh with multiply GI Weight
            if (GUILayout.Button ("LIGHT PROBES WEIGHT PROCESS", buttonAStyle, GUILayout.Width (379), GUILayout.Height (48)))
            {
                calculateProbes (bakedGIWeight);
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

        void calculateProbes (float bakedGIWeight)
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
            bakedProbes = assignData2bakedProbe (loadProbeDataXml (getXMLPath (bakedGIData)), bakedGIWeight, bakedProbes, probesCount);
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
        public SphericalHarmonicsL2[] assignData2bakedProbe (List<List<float>> tempProbeData, float GIweight, SphericalHarmonicsL2[] bakedProbes, int probeCount)
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

        string getSavePath (string GIDataname)
        {
            string scenePath = EditorApplication.currentScene;
            scenePath = scenePath.Substring (0, scenePath.Length - 6);
            string savepath = scenePath + @"/" + "/" + GIDataname + ".xml";
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
            titleAStyle.fontSize = 12;
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


