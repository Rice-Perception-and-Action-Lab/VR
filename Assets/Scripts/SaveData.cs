using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class SaveData : MonoBehaviour
{

    public TrialData[] data;    // the data collected for each trial ran

    private int i;              // index to track current position in data array
    private float expStart;     // the start time of the experiment (all trial times are relative to this value)

    /**
     * This class holds all data for an individual trial. It includes all 
     * information given in the JSON object for the trial as well as all
     * relevant information gathered over the course of the trial.
     */
    [System.Serializable]
    public class TrialData
    {
        public int trialNum;            // the number of the corresponding trial
        public float finalDist;         // the total distance this object should travel (in meters)
        public float startXCoord;       // the x-coord of the location where the object was first placed into view 
        public float startYCoord;       // the y-coord of the location where the object was first placed into view 
        public float startZCoord;       // the z-coord of the location where the object was first placed into view 
        public float velocity;          // the speed the object is moving (in meters / second)
        public float timeVisible;       // the amount of time this object should be visible before disappearing
        public string objType;          // the type of object presented to the participant (e.g., "Cube", "Sphere", etc.)
        public float trialStart;        // the time at which the trial began
        public float trialEnd;      // the time at which the participant responded via Vive controller button press
        public bool receivedResponse;   // tracks whether a participant responsed (true) or the trial timed out (false)
        public float ttc;               // the time to contact based on (finalDist / velocity)

        /**
         * A constructor for the TrialData object
         */
        public TrialData(int trialNum, float dist, Vector3 startPos, float velocity, float timeVisible,
            string objType, float trialStart, float trialEnd, bool receivedResponse)
        {
            this.trialNum = trialNum;
            this.finalDist = dist;
            this.startXCoord = startPos.x;
            this.startYCoord = startPos.y;
            this.startZCoord = startPos.z;
            this.velocity = velocity;
            this.timeVisible = timeVisible;
            this.objType = objType;
            this.trialStart = trialStart;
            this.trialEnd = trialEnd;
            this.receivedResponse = receivedResponse;
            this.ttc = (dist / velocity);
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
            return wrapper.Items;
        }

        public static string ToJson<T>(T[] array)
        {
            Wrapper<T> wrapper = new Wrapper<T>();
            wrapper.Items = array;
            return JsonUtility.ToJson(wrapper);
        }

        public static string ToJson<T>(T[] array, bool prettyPrint)
        {
            Wrapper<T> wrapper = new Wrapper<T>();
            wrapper.Items = array;
            return JsonUtility.ToJson(wrapper, prettyPrint);
        }

        [System.Serializable]
        private class Wrapper<T>
        {
            public T[] Items;
        }

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
    }

    /**
     * Called by the dataManager object in the RunExperiment script whenever
     * a given trial is completed so that the trial's data can be added
     * to the data array.
     */
    public void AddTrial(int trialNum, float dist, Vector3 startPos, float velocity, float timeVisible,
           string objType, float trialStart, float trialEnd, bool receivedResponse)
    {
        // Ensure that you aren't trying to index into a non-existant position in the array
        if (i < data.Length)
        {
            Debug.Log("Adding trial " + trialNum + " to data array.");

            // Create a new object with the appropriate data
            TrialData trial = new TrialData(trialNum + 1, dist, startPos, velocity, timeVisible,
                objType, trialStart, trialEnd, receivedResponse);

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

    /**
     * This method writes all trial data to a JSON file.
     */
    public void Save()
    {
        string jsonData = JsonHelper.ToJson(data, true);
        string dir = Application.dataPath + "/../Results/";
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