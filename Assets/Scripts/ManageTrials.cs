using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ManageTrials : MonoBehaviour {

    [System.Serializable]
    public class Trial
    {
        public int trialNum;            // the number of the current trial
        public float startDist;         // the initial distance in front of the participant at which the object appears
        public float velocity;          // the speed that the object is moving
        public float timeVisible;       // the amount of time that the object is visible before disappearing
        public string objType;          // the name of the prefab that the object should be instantiated as
        public float objScaleX;         // the x-coordinate for the scale of the object
        public float objScaleY;         // the y-coordinate for the scale of the object
        public float objScaleZ;         // the z-coordinate for the scale of the object
        public float rotationSpeed;     // the speed at which the object should rotate
    }


    /**
     * This wrapper class is a workaround for how Unity's JsonUtility class handles
     * top-level JSON objects.
     */
    [System.Serializable]
    class TrialArray
    {
        public Trial[] trials;
    }


    /**
     * Given a path to a JSON file containing the parameters for each trial in the experiment,
     * creates a Trial object that has the correct values for each entry in the input file.
     */
    public Trial[] LoadTrialData(string path, float time)
    {
        try
        {
            StreamReader sr = new StreamReader(path);
            string jsonString = sr.ReadToEnd();
            TrialArray trialData = JsonUtility.FromJson<TrialArray>(jsonString);
            Debug.Log("quick check");
            return trialData.trials;
        }
        catch (System.Exception e)
        {
            Debug.Log("Exception: " + e.Message);
            return null;
        }
    }

}
