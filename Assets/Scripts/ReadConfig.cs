using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReadConfig : MonoBehaviour {

    [System.Serializable]
    public class Config
    {
        public int subjNum;
        public int subjSex;
        public string dataFile;
        public int objMoveMode;
        public bool trackHeadPos;
        public float[] initCameraPos;
        public bool showFeedback;
        public float[] canvasPos;
        public int feedbackSize;
        public string feedbackColor;
        public bool setObjX;
        public bool setObjY;
    }

    public Config LoadConfig(string configFile)
    {
        try
        {
            string filepath = configFile.Replace(".json", "");
            TextAsset jsonString = Resources.Load<TextAsset>(filepath);
            Config config = JsonUtility.FromJson<Config>(jsonString.ToString());
            return config;
        }
        catch (System.Exception e)
        {
            print("Exception: " + e.Message);
            return null;
        }
    }

}
