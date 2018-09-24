using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class SaveData : MonoBehaviour {

    public TrialData[] data;    // the data collected for all trials

    private int i;              // an index to track the current position in the data array
    private float expStart;     // the start time of the experiment (all trial times are relative to this value)

    // The class that holds data for an individual trial
    [System.Serializable]
    public class TrialData
    {
        public int trialNum;       // the number of the trial
        public float speed;        // the speed of the object
        public float xPos;         // the initial x-coordinate of the object
        public float yPos;         // the initial y-coordinate of the object
        public float zPos;         // the initial z-coordinate of the object
        public string objType;     // the type of object presented to the participant
        public float startTime;    // the time (relative to the beginning of the experiment) that the trial started 
        public float endTime;      // the time (relative to the beginning of the experiment) that the trial ended (due to either user response or timeout)

        public TrialData(int trialNum, float speed, float x, float y, float z, string obj, float start, float end)
        {
            this.trialNum = trialNum;
            this.speed = speed;
            this.xPos = x;
            this.yPos = y;
            this.zPos = z;
            this.objType = obj;
            this.startTime = start;
            this.endTime = end;
        }
    }

    // A helper class to convert a TrialData object into JSON
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

    // This method is called by the dataManager object in the MoveObj script when the experiment starts
    public void initDataArray(int numTrials)
    {
        this.data = new TrialData[numTrials];
        this.i = 0;
    }

    // This method is called by the dataManager object in the MoveObj script when the experiment starts
    public void setStartTime(float start)
    {
        Debug.Log("Experiment starting at time " + start);
        this.expStart = start;
    }

    // A method to create a new trial and add it to the array of all data (made for use by other GameObject scripts)
    public void addNewTrial(int trialNum, float speed, float x, float y, float z, string obj, float start, float end)
    {
        if (i < data.Length)
        {
            Debug.Log("Adding trial " + trialNum + " to data array");
            // Create a new object with the appropriate data
            TrialData trial = new TrialData(trialNum + 1, speed, x, y, z, obj, start, end);

            // Place this trial's data in the next available slot in the data array
            data[i] = trial;

            // Increment the array pointer
            i++;
        }
        else
        {
            Debug.Log("All trials completed; can't add new trial");
        }
    }

    // A method to write all trial data to a JSON file
    public void saveData()
    {
        string jsonData = JsonHelper.ToJson(data, true);

        string dir = Application.dataPath + "/../Results/";
        string dataFile = System.DateTime.Now.ToString("yyyy-MM-dd--HH-mm-ss") + "_data.json";
        string filepath = Path.Combine(dir, dataFile);

        using (StreamWriter writer = new StreamWriter(filepath, false))
        {
            writer.WriteLine(jsonData);
            writer.Flush();
        }
    }
}

