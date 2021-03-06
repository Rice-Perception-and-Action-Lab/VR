﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Data;
using System.Text;

[System.Serializable]
public class SaveData : MonoBehaviour
{
    public TrialData[] data;                    // the data about participant responses and the metadata about each trial's parameters
    private List<Position> headPosData;         // the head position data collected for each trial
    private List<Position> controllerPosData;   // the controller position data collected for each trial
    private int i;                              // the index to track the current trial/position in the data array
    private float expStart;                     // the start time of the experiment (the times for individual trials are relative to this)
    private string datetime;                    // the date/time that the experiment began

    /* Config-Specified Fields */
    public int subjNum;
    public int subjSex;
    public int session;
    public int group;
    public static int TRIALNUM;
    public int corrAns;
    public string trialFile;
    public bool trackHeadPos;
    public float[] initCameraPos;
    public bool showFeedback;
    public int feedbackType;
    public bool collectConfidence;
    public float[] feedbackPos;
    public int feedbackSize;
    public string feedbackColor;
    public string path;



    void Awake()
    {
        path = Application.dataPath;
    }

    /**
	 * The ObjData class holds the information for a single object within a trial.
	 */
    [System.Serializable]
    public class ObjData
    {
        public int objNum;
        public string objType;          // the name of the prefab that the object should be instantiated as
        public float[] objScale;        // the x,y,z-coordinates for the scale of the object
        public float[] startPos;        // the x,y,z-coordinates for the initial position of the object
        public float[] endPos;          // the x,y,z-coordinates for the final position of the object
        public float distTraveled;      // the distance that the object traveled
        public float velocity;          // the speed that the object is moving
        public float timeVisible;       // the amount of time that the object is visible before disappearing
        public float rotationSpeedX;         // the speed at which the object should rotate around X axis
        public float rotationSpeedY;         // the speed at which the object should rotate around Y axis
        public float rotationSpeedZ;         // the speed at which the object should rotate around Z axis

        /**
		 * The constructor for the ObjData object.
		 */
        public ObjData(ManageObjs.Obj obj)
        {
            this.objNum = obj.objNum;
            this.objType = obj.objType;
            this.objScale = obj.objScale;
            this.startPos = obj.startPos;
            this.endPos = obj.endPos;
            this.distTraveled = obj.dist;
            this.velocity = obj.velocity;
            this.timeVisible = obj.timeVisible;
            this.rotationSpeedX = obj.rotationSpeedX;
            this.rotationSpeedX = obj.rotationSpeedY;
            this.rotationSpeedX = obj.rotationSpeedZ;
        }
    }

    /**
	 * The TrialData class holds all data for an individual trial. This includes all information about the
	 * parameters specified for the trial as well as information about the participant's responses
	 * during the trial.
	 */
    [System.Serializable]
    public class TrialData
    {
        public int trialNum;            // the number of the trial
        public string trialName;        // the name of the trial
        public int corrAns;
        public ObjData[] objData;       // the data about the objects presented in the trial
        public float trialStart;        // the time at which the trial began 
        public float trialEnd;          // the time at which the trial ended (based on when the participant responds via the controller)
        public float respTime;          // the amount of time it took for the participant to respond
        public string response;         // the reponse made by the participant
        public string confidence;       // the confidence rating made by the participant
        public float ttcEstimate;       // the participant's calculated TTC estimate



        /**
		 * The constructor for the TrialData object. It needs to create an array to represent 
		 */
        public TrialData(ManageTrials.Trial trial)
        {
            this.trialNum = trial.trialNum;
            TRIALNUM = SaveData.putTrialNum(trial.trialNum);
            this.trialName = trial.trialName;
            this.corrAns = trial.corrAns;
            this.trialStart = trial.trialStart;
            this.trialEnd = trial.trialEnd;
            this.respTime = trialEnd - trialStart;
            this.response = trial.response;
            this.confidence = trial.confidence;



            // Iterate through all objects to create an ObjData object for each object in the trial
            this.objData = new ObjData[trial.objects.Length];
            for (int j = 0; j < trial.objects.Length; j++)
            {
                this.objData[j] = new ObjData(trial.objects[j]);
                this.ttcEstimate = this.respTime - this.objData[j].timeVisible;
            }
        }

    }

    /**
     * The HeadPos class represents the position of the Vive headset at a specific point in time.
     */
    [System.Serializable]
    public class Position
    {
        public float timestamp;     // the time at which this position was recorded
        public float x;             // the x-coordinate of the camera rig at this point in time
        public float y;             // the y-coordinate of the camera rig at this point in time
        public float z;             // the z-coordinate of the camera rig at this point in time
        public float eulerX;        // the x-coordinate of the rotation of the camera rig at this point in time
        public float eulerY;        // the y-coordinate of the rotation of the camera rig at this point in time
        public float eulerZ;        // the z-coordinate of the rotation of the camera rig at this point in time

        /**
         * A constructor for the HeadPos object.
         */
        public Position(float timestamp, Vector3 pos, Vector3 euler)
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

        public static string ToJson<T>(SaveData dataObj, T[] array, bool prettyPrint)
        {
            Wrapper<T> wrapper = new Wrapper<T>();
            wrapper.Trials = array;
            wrapper.subjNum = dataObj.subjNum;
            wrapper.subjSex = dataObj.subjSex;
            wrapper.session = dataObj.session;
            wrapper.group = dataObj.group;
            wrapper.trialFile = dataObj.trialFile;
            wrapper.trackHeadPos = dataObj.trackHeadPos;
            wrapper.showFeedback = dataObj.showFeedback;
            wrapper.feedbackType = dataObj.feedbackType;
            wrapper.collectConfidence = dataObj.collectConfidence;
            wrapper.feedbackColor = dataObj.feedbackColor;
            return JsonUtility.ToJson(wrapper, prettyPrint);
        }

        [System.Serializable]
        private class Wrapper<T>
        {
            public int subjNum;
            public int subjSex;
            public int session;
            public int group;
            public string trialFile;
            public bool showFeedback;
            public int feedbackType;
            public bool collectConfidence;
            public string feedbackColor;
            public bool trackHeadPos;
            public T[] Trials;
        }
    }

    /**
     * This method is called by the dataManager object in the RunExperiment script. It sets the appropriate
     * experiment-level variables specified in the config file. Setting these config fields is necessary
     * so that Unity's JsonUtility class can correctly convert the data to JSON.
     */
    public void SetConfigInfo(ReadConfig.Config config)
    {
        this.subjNum = config.subjNum;
        this.subjSex = config.subjSex;
        this.session = config.session;
        this.group = config.group;
        this.trialFile = config.trialFile;
        this.trackHeadPos = config.trackHeadPos;
        this.showFeedback = config.showFeedback;
        this.feedbackType = config.feedbackType;
        this.collectConfidence = config.collectConfidence;
        this.feedbackPos = config.feedbackPos;
        this.feedbackSize = config.feedbackSize;
        this.feedbackColor = config.feedbackColor;
    }

    /**
     * This method is called by the dataManager object in the RunExperiment script when the experiment is
     * initialized, after the trial datafile has been read in. This allows us to know how many trials
     * are in the experiment (i.e., how long our data array needs to be).
     */
    public void InitDataArray(int numTrials, float startTime)
    {
        this.data = new TrialData[numTrials];
        this.i = 0;
        this.expStart = startTime;
        this.datetime = System.DateTime.Now.ToString("yyyy-MM-dd--HH-mm-ss");
        this.headPosData = new List<Position>();
        this.controllerPosData = new List<Position>();

    }

    /**
     * Called by the dataManager object in the RunExperiment script whenever a given trial is completed
     * so that the trial's data can be stored.
     */
    public void AddTrial(ManageTrials.Trial trial)
    {
        // Ensure that we don't try to index past the end of the array
        if (i < data.Length)
        {
            data[i] = new TrialData(trial);
            i++;
        }
    }

    /**
     * Called by the dataManager object in the RunExperiment script at a specified interval so that
     * a time series of head position data can be stored.
     */
    public void AddHeadPos(float timestamp, Vector3 curPos, Vector3 curEuler)
    {
        headPosData.Add(new Position(timestamp, curPos, curEuler));
    }

    public void AddControllerPos(float timestamp, Vector3 curPos, Vector3 curEuler)
    {
        controllerPosData.Add(new Position(timestamp, curPos, curEuler));
    }

    /**
     * Write the head position data to a JSON file.
     */
    public void WriteHeadPosData()
    {
            Position[] headPosArr = headPosData.ToArray();   // Unity's JsonHelper utility can only parse arrays, not lists
            string jsonData = JsonHelper.ToJson(this, headPosArr, true);
            string dir = path + "/Data/Subj" + subjNum + "/Head Data/";

            // Create the directory to store the position data if it doesn't already exist
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string trialPosDataFile = "Subj" + subjNum.ToString() + "_Head_Data_Trial" + TRIALNUM + ".json";
            string filepath = Path.Combine(dir, trialPosDataFile);
            Debug.Log("Saving head data to " + filepath);

            // Write the JSON string to the specified file path
            using (StreamWriter writer = new StreamWriter(filepath, false))
            {
                writer.WriteLine(jsonData);
                writer.Flush();
            }

            // Make a new list to collect the next trial's head position data
            headPosData = new List<Position>();
    }

    /**
 * Write the controller position data to a JSON file.
 */
    public void WriteControllerPosData()
    {
        Position[] controllerPosArr = controllerPosData.ToArray();   // Unity's JsonHelper utility can only parse arrays, not lists
        string jsonData = JsonHelper.ToJson(this, controllerPosArr, true);
        string dir = path + "/Data/Subj" + subjNum + "/Controller Data/";

        // Create the directory to store the position data if it doesn't already exist
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string trialPosDataFile = "Subj" + subjNum.ToString() + "_Controller_Data_Trial" + TRIALNUM + ".json";
        string filepath = Path.Combine(dir, trialPosDataFile);
        Debug.Log("Saving controller data to " + filepath);

        // Write the JSON string to the specified file path
        using (StreamWriter writer = new StreamWriter(filepath, false))
        {
            writer.WriteLine(jsonData);
            writer.Flush();
        }

        // Make a new list to collect the next trial's controller position data
        controllerPosData = new List<Position>();
    }

    /**
     * Write all trial data to a JSON file.
     */

    public void Save(bool partial)
    {
        string jsonData = JsonHelper.ToJson(this, data, true);
        string dir = Application.dataPath + "/Data/Subj" + subjNum;
        string dataFile = "";
        string dataFileCsv = "";

        // Create the directory to store the trial data if it hasn't already been created
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }


        if (partial) { dataFile = "Subj" + subjNum.ToString() + "_Data_Partial.json"; } else { dataFile = "Subj" + subjNum.ToString() + "_Data.json"; }
        string filepath = Path.Combine(dir, dataFile);
        Debug.Log("Saving data to " + filepath);

        using (StreamWriter writer = new StreamWriter(filepath, false))
        {
            writer.WriteLine(jsonData);
            writer.Flush();
        }


        //start code to write to csv file
        if(partial) { dataFileCsv = "Subj" + subjNum.ToString() + "_Data_Partial.csv"; } else { dataFileCsv = "Subj" + subjNum.ToString() + "_Data.csv"; }
        string filepathCsv = Path.Combine(dir, dataFileCsv);
        Debug.Log("Saving CSV data to " + filepathCsv);

        using (StreamWriter writer = new StreamWriter(filepathCsv, false))
        {

            var header = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12}", "Subj", "Sex", "Session", "Group", "Trial", "TrialName", "CorrAns", "TrialStart", "TrialEnd", "RespTime", "Resp", "TTC Estimate", "Confidence");
            writer.WriteLine(header);
            writer.Flush();

            foreach (TrialData trial in data)
            {
                var subjnum = subjNum.ToString();
                var subjsex = subjSex;
                var sess = session;
                var grp = group;
                var trialnum = trial.trialNum.ToString();
                var name = trial.trialName;
                var start = trial.trialStart.ToString();
                var end = trial.trialEnd.ToString();
                var resptime = trial.respTime.ToString();
                var resp = trial.response;
                var corrAns = trial.corrAns;
                var conf = trial.confidence;
                var est = trial.ttcEstimate.ToString();

                var data = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12}", subjnum, subjsex, sess, grp, trialnum, name, corrAns, start, end, resptime, resp, est, conf);
                writer.WriteLine(data);
                writer.Flush();
            }

        }

    }

    public static int putTrialNum(int trialNum) //probably not the best approach but it worked...
    {
        return trialNum;
    }

}
