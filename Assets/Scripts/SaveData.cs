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
    private int subjNum;
    private int subjSex;
    private string dataFile;
    private bool showFeedback;
    private string feedbackColor;
    private bool targetCamera;
    private bool trackHeadPos;
    
    /**
     * This class holds all data for an individual trial. It includes all 
     * information given in the JSON object for the trial as well as all
     * relevant information gathered over the course of the trial.
     */
    [System.Serializable]
    public class TrialData
    {
        public int trialNum;            // the number of the corresponding trial
        public float startDist;         // the starting distance of the object (in meters)
        public float startXCoord;       // the x-coord of the location where the object was first placed into view 
        public float startYCoord;       // the y-coord of the location where the object was first placed into view 
        public float startZCoord;       // the z-coord of the location where the object was first placed into view 
        public float velocity;          // the speed the object is moving (in meters / second)
        public float rotationRate;      // the rate at which the object rotates forwards (use a negative number for backwards rotation)
        public float timeVisible;       // the amount of time this object should be visible before disappearing
        public string objType;          // the type of object presented to the participant (e.g., "Cube", "Sphere", etc.)
        public float trialStart;        // the time at which the trial began
        public float trialEnd;          // the time at which the participant responded via Vive controller button press
        public bool receivedResponse;   // tracks whether a participant responsed (true) or the trial timed out (false)
        public float respTime;          // the time during the simulation when the participant responded (trialEnd - trialStart)
        public float ttcEstimate;       // the participants actual TTC estimate (respTime - timeVisible);
        public float ttcActual;         // the time to contact based on when the object disappeared

        /**
         * A constructor for the TrialData object
         */
        public TrialData(int trialNum, float dist, Vector3 startPos, float velocity, float timeVisible, float rotationRate,
            string objType, float trialStart, float trialEnd, bool receivedResponse, float respTime)
        {
            this.trialNum = trialNum;
            this.startDist = dist;
            this.startXCoord = startPos.x;
            this.startYCoord = startPos.y;
            this.startZCoord = startPos.z;
            this.velocity = velocity;
            this.rotationRate = rotationRate;
            this.timeVisible = timeVisible;
            this.objType = objType;
            this.trialStart = trialStart;
            this.trialEnd = trialEnd;
            this.receivedResponse = receivedResponse;
            this.respTime = respTime;
            this.ttcEstimate = respTime - timeVisible;
            this.ttcActual = (dist - (velocity * timeVisible)) / velocity;
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
     * This method is called by the dataManager object in the RunExperiment script in order to set
     * the appropriate experiment-level variables specified by the config file.
     */
    public void SetConfigInfo(int subjNum, int subjSex, string dataFile, bool showFeedback, string feedbackColor, bool targetCamera, bool trackHeadPos)
    {
        this.subjNum = subjNum;
        this.subjSex = subjSex;
        this.dataFile = dataFile;
        this.showFeedback = showFeedback;
        this.feedbackColor = feedbackColor;
        this.targetCamera = targetCamera;
        this.trackHeadPos = trackHeadPos;
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
     * Called by the dataManager object in the RunExperiment script whenever
     * a given trial is completed so that the trial's data can be added
     * to the data array.
     */
    public void AddTrial(int trialNum, float dist, Vector3 startPos, float velocity, float rotationRate, float timeVisible,
           string objType, float trialStart, float trialEnd, bool receivedResponse, float respTime)
    {
        // Ensure that you aren't trying to index into a non-existant position in the array
        if (i < data.Length)
        {
            Debug.Log("Adding trial " + trialNum + " to data array.");

            // Create a new object with the appropriate data
            TrialData trial = new TrialData(trialNum, dist, startPos, velocity, timeVisible, rotationRate,
                objType, trialStart, trialEnd, receivedResponse, respTime);

            // Place this trial's data in the next available slot in the data array
            data[i] = trial;

            // Increment the array pointer for the next trial
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