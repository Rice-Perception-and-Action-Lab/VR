using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class SaveData : MonoBehaviour
{

    public TrialData[] data;        // the TTC data collected for each trial ran
    private List<HeadPos> posData;  // the head position data collected for each trial

    private int i;                  // index to track current position in data array
    private float expStart;         // the start time of the experiment (all trial times are relative to this value)
    private string datetime;        // the date/time that this experiment started

    /* Config-Specified Fields */
    public int subjNum;
    public int subjSex;
    public string dataFile;
    public bool targetCamera;
    public bool trackHeadPos;
    public float initCameraX;
    public float initCameraY;
    public float initCameraZ;
    public bool showFeedback;
    public float canvasX;
    public float canvasY;
    public float canvasZ;
    public int feedbackSize;
    public string feedbackColor;
    public bool setObjX;
    public bool setObjY;


    /**
     * This class holds all data for an individual trial. It includes all 
     * information given in the JSON object for the trial as well as all
     * relevant information gathered over the course of the trial.
     */
    [System.Serializable]
    public class TrialData
    {
        public int trialNum;
        public string objType;
        public float objScaleX;
        public float objScaleY;
        public float objScaleZ;
        public float startXCoord;
        public float startYCoord;
        public float startZCoord;
        public float startDist;
        public float velocity;
        public float timeVisible;
        public float rotationSpeed;
        public float trialStart;
        public float trialEnd;
        public float respTime;
        public float ttcEstimate;
        public float ttcActual;

        /**
         * A constructor for the TrialData object
         */
        public TrialData(ManageTrials.Trial trial, float trialStart, float trialEnd, float ttcActual, float ttcEstimate)
        {
            this.trialNum = trial.trialNum;
            this.objType = trial.objType;
            this.objScaleX = trial.objScaleX;
            this.objScaleY = trial.objScaleY;
            this.objScaleZ = trial.objScaleZ;
            this.startXCoord = trial.startXCoord;
            this.startYCoord = trial.startYCoord;
            this.startZCoord = trial.startZCoord;
            this.startDist = trial.startDist;
            this.velocity = trial.velocity;
            this.rotationSpeed = trial.rotationSpeed;
            this.trialStart = trialStart;
            this.trialEnd = trialEnd;
            this.respTime = trialEnd - trialStart;
            this.ttcEstimate = ttcEstimate;
            this.ttcActual = ttcActual;
        }
    }

    [System.Serializable]
    public class HeadPos
    {
        public float timestamp;     // The time this position was recorded
        public float x;             // The x-coordinate of the camera rig at the given point in time
        public float y;             // The x-coordinate of the camera rig at the given point in time
        public float z;             // The x-coordinate of the camera rig at the given point in time
        public float eulerX;
        public float eulerY;
        public float eulerZ;

        /**
         * A constructor for the HeadPos object
         */
        public HeadPos(float timestamp, Vector3 pos, Vector3 euler)
        {
            this.timestamp = timestamp;
            this.x = pos.x;
            this.y = pos.y;
            this.z = pos.z;
            this.eulerX = euler.x;
            this.eulerY = euler.y;
            this.eulerZ = euler.z;
        }
    }

    /**
     * This helper class converts TrialData objects into JSON.
     */
    public static class JsonHelper
    {
        public static T[] FromJson<T>(string json)
        {
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
            return wrapper.Trials;
        }

        public static string ToJson<T>(SaveData dataObj, T[] array)
        {
            Wrapper<T> wrapper = new Wrapper<T>();
            wrapper.Trials = array;
            wrapper.subjNum = dataObj.subjNum;
            wrapper.subjSex = dataObj.subjSex;
            wrapper.dataFile = dataObj.dataFile;
            wrapper.showFeedback = dataObj.showFeedback;
            wrapper.feedbackColor = dataObj.feedbackColor;
            wrapper.targetCamera = dataObj.targetCamera;
            wrapper.trackHeadPos = dataObj.trackHeadPos;
            return JsonUtility.ToJson(wrapper);
        }

        public static string ToJson<T>(SaveData dataObj, T[] array, bool prettyPrint)
        {
            Wrapper<T> wrapper = new Wrapper<T>();
            wrapper.Trials = array;
            wrapper.subjNum = dataObj.subjNum;
            wrapper.subjSex = dataObj.subjSex;
            wrapper.dataFile = dataObj.dataFile;
            wrapper.showFeedback = dataObj.showFeedback;
            wrapper.feedbackColor = dataObj.feedbackColor;
            wrapper.targetCamera = dataObj.targetCamera;
            wrapper.trackHeadPos = dataObj.trackHeadPos;
            return JsonUtility.ToJson(wrapper, prettyPrint);
        }

        [System.Serializable]
        private class Wrapper<T>
        {
            public int subjNum;
            public int subjSex;
            public string dataFile;
            public bool showFeedback;
            public string feedbackColor;
            public bool targetCamera;
            public bool trackHeadPos;
            public T[] Trials;
        }
    }

   
    /**
     * This method is called by the dataManager object in the RunExperiment script. It sets the 
     * appropriate experiment-level variables specified in the config file. It is necessary to set
     * all the config fields in the SaveData class so that Unity's JsonUtility will correctly
     * convert the data to JSON.
     */
    public void SetConfigInfo(ReadConfig.Config config)
    {
        this.subjNum = config.subjNum;
        this.subjSex = config.subjSex;
        this.dataFile = config.dataFile;
        this.targetCamera = config.targetCamera;
        this.trackHeadPos = config.trackHeadPos;
        this.initCameraX = config.initCameraX;
        this.initCameraY = config.initCameraY;
        this.initCameraZ = config.initCameraZ;
        this.showFeedback = config.showFeedback;
        this.canvasX = config.canvasX;
        this.canvasY = config.canvasY;
        this.canvasZ = config.canvasZ;
        this.feedbackSize = config.feedbackSize;
        this.feedbackColor = config.feedbackColor;
        this.setObjX = config.setObjX;
        this.setObjY = config.setObjY;
}


/**
 * This method is called by the dataManager object in the RunExperiment script
 * when the experiment is initialized and the data for the experiment has
 * been read in. This tells us how many trials the experiment contains 
 * (i.e., how long our array should be).
 */
public void InitDataArray(int numTrials, float startTime)
    {
        this.data = new TrialData[numTrials];
        this.i = 0;     // place the first trial in the first position in the array

        Debug.Log("Experiment starting at time " + startTime);
        this.expStart = startTime;

        this.datetime = System.DateTime.Now.ToString("yyyy-MM-dd--HH-mm-ss");

        posData = new List<HeadPos>();
    }

    /**
     * Called by the dataManager object in the RunExperiment script whenever a given trial is 
     * completed so that the trial's data can be added to the data array.
     */
     public void AddTrial(ManageTrials.Trial trial, Vector3 subjPos, float startTime, float endTime)
     {
        // Ensure that you don't index past the end of the array
        if (i < data.Length)
        {
            Debug.Log("Adding trial " + trial.trialNum + " to data array.");

            // Calculate the actual and estimated TTCs
            float dist = trial.startZCoord - subjPos.z;
            float ttcActual = (dist - (trial.velocity * trial.timeVisible)) / trial.velocity;
            float ttcEstimate = (endTime - startTime) - trial.timeVisible;    // response time - time visible

            // Add the new trial data to the next available position in the data array
            data[i] = new TrialData(trial, startTime, endTime, ttcActual, ttcEstimate);
            i++;
        }
        else
        {
            Debug.Log("All trials completed; can't add new trial");
        }
    }


    public void AddHeadPos(float timestamp, Vector3 curPos, Vector3 curEuler)
    {
        posData.Add(new HeadPos(timestamp, curPos, curEuler));
    }

    public void WritePosData()
    {
        HeadPos[] posArr = posData.ToArray();
        string jsonData = JsonHelper.ToJson(this, posArr, true);
        string dir = Application.dataPath + "/../Results/HeadPos/";

        // Create the directory if it hasn't already been created
        if (!Directory.Exists(dir))
        {
            Debug.Log("creating dir " + dir);
            Directory.CreateDirectory(dir);
        }

        // Add the time of the experiment and create that directory, if needed
        dir += datetime + "/";
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string trialDataFile = "Trial" + (i) + ".json";
        string filepath = Path.Combine(dir, trialDataFile);

        using (StreamWriter writer = new StreamWriter(filepath, false))
        {
            writer.WriteLine(jsonData);
            writer.Flush();
        }

        posData = new List<HeadPos>();
    }

    /**
     * This method writes all trial data to a JSON file.
     */
    public void Save()
    {
        string jsonData = JsonHelper.ToJson(this, data, true);
        string dir = Application.dataPath + "/../Results/ParticipantResponse/";

        // Create the directory if it hasn't already been created
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }


        string dataFile = System.DateTime.Now.ToString("yyyy-MM-dd--HH-mm-ss") + "_data.json";
        string filepath = Path.Combine(dir, dataFile);
        Debug.Log("Saving data to " + filepath);

        using (StreamWriter writer = new StreamWriter(filepath, false))
        {
            writer.WriteLine(jsonData);
            writer.Flush();
        }
    }

}