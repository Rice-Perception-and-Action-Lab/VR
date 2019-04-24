using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ReadConfig : MonoBehaviour {

    /**
     * The Config class parses all information from the provided config file and creates an object so that the 
     * config information can be accessed by the rest of the program.
     */
    [System.Serializable]
    public class Config
    {
        public int subjNum;
        public int subjSex;
        public string dataFile;
        public float[] initCameraPos;
        public int objMoveMode;
        public bool trackHeadPos;
        public bool offsetObj;
        public bool showFeedback;
        public float[] canvasPos;
        public int feedbackSize;
        public string feedbackColor;
    }

    public Config LoadConfig(string configFilepath)
    {
        try
        {
            if (File.Exists(configFilepath))
            {
                string jsonString = File.ReadAllText(configFilepath);
                Config config = JsonUtility.FromJson<Config>(jsonString.ToString());
                return config;
            }
            // hard-coded for development
            else if (File.Exists(Application.dataPath + "/Resources/config.json"))
            {
                configFilepath = Application.dataPath + "/Resources/config.json";
                string jsonString = File.ReadAllText(configFilepath);
                Config config = JsonUtility.FromJson<Config>(jsonString.ToString());
                return config;
            }
            else
            {
                Debug.Log("ERROR: Couldn't open file at: " + configFilepath);
                return null;
            }

        }
        catch (System.Exception e)
        {
            print("Exception: " + e.Message);
            return null;
        }
    }

}
