using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ManageTrials : MonoBehaviour {

    /**
     * This class defines a Trial, which is defined by the input file. The trialStart and trialEnd fields
     * aren't defined in the input file but are set when they occur during the experiment.
     */
    [System.Serializable]
    public class Trial
    {
        public int trialNum;                // the number of the current trial
        public ManageObjs.Obj[] objects;    // the objects to be displayed in the trial
        public float trialStart;            // the time at which the trial began
        public float trialEnd;              // the time at which the trial ended
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
    public Trial[] LoadTrialData(string filepath, float time)
    {
        try
        {
            string jsonString = File.ReadAllText(filepath);
            TrialArray trialData = JsonUtility.FromJson<TrialArray>(jsonString);
            return trialData.trials;
        }
        catch (System.Exception e)
        {
            Debug.Log("Exception: " + e.Message);
            return null;
        }
    }

}
