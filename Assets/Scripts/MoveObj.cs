using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/**
 * TODO:
 *  2) Present next trial even if there is no press
 *  3) Track time at which button has been pressed
 *  4) Switch to blank scene after all trials have run
 *  5) Blank out object at some point before contact
 *  6) Add rotation to trial input
 */


public class MoveObj : MonoBehaviour {

    public SaveData dataManager;  // The GameObject responsible for managing 
    public Transform target;        // Position of the target (i.e., the Vive camera rig)

    private float speed;            // Speed in units per second
    private Vector3 startPos;       // The starting position of the object
    private string objName;         // The name of the prefab object used for the given trial
    private Transform obj;          // The prefab object that will be instantiated
    private Transform movingObj;    // The object that will approach the user
    private float trialStartTime;   // The time that a particular trial began running
    private string filepath = "U:\\unity-testing\\Flying Cube Experiment\\Flying Cube v.1\\Assets\\Scripts\\sample2.json";  // The path to the file containing the trial data
    private Trial[] trials;         // A Trial object containing all info for the current trial (eventually make this an array of trials)
    private int trialNum;           // A pointer to the next trial we want to run

    [System.Serializable]
    class TrialArray
    {
        public Trial[] trials;  // need this wrapper class as a workaround for how JsonUtility handles top-level JSON objects
    }

    [System.Serializable]
    public class Trial
    {
        public int trialNum;        // the trial number
        public int speed;           // the speed for the trial
        public float xPos;          // the initial x-coordinate for the moving object's transform
        public float yPos;          // the initial y-coordinate for the moving object's transform
        public float zPos;          // the initial z-coordinate for the moving object's transform
        public string[] objects;    // the names of the potential object prefabs that can be instantiated
    }

    /* 
     *  Given a filepath to a JSON file containing info for each trial, 
     *  create a Trial object with the correct parameters for all of the trials
     *  in the input file
     */
    public Trial[] LoadTrialData(string path)
    {
        try
        {
            StreamReader sr = new StreamReader(path);
            string jsonString = sr.ReadToEnd();
            TrialArray trialData = JsonUtility.FromJson<TrialArray>(jsonString);    // parse an array of trials from given JSON

            // Initialize the TrialData array in the data manager
            dataManager.initDataArray(trialData.trials.Length);

            return trialData.trials;
        }
        catch (System.Exception e)
        {
            print("Exception: " + e.Message);
            return null;
        }
    }

    // Deletes the existing moving object before a new trial can be initialized
    public void DeleteMovingObj()
    {
        if (movingObj && movingObj.gameObject)
        {
            Debug.Log("Deleting moving object");
            Destroy(movingObj.gameObject);
        } else
        {
            Debug.Log("Tried to delete moving object, but no object to delete");
        }
    }

    // Sets the fields of the MoveObj class to the appropriate values for the given trial
    public void InitializeTrial()
    {
        // Check that we still have trials to run
        if (trialNum < trials.Length)
        {
            Debug.Log("Initializing trial " + trialNum);

            // Get the current trial
            Trial trial = trials[trialNum];

            // Set the speed 
            speed = trial.speed;

            // Set the start position
            startPos = new Vector3(trial.xPos, trial.yPos, trial.zPos);

            // Randomly select a value from the array of prefabs
            System.Random r = new System.Random();
            int ind = r.Next(0, trial.objects.Length);
            objName = trial.objects[ind];


            // Set the object prefab that will approach the user
            GameObject newObj = Resources.Load(objName) as GameObject;
            obj = newObj.transform;

            // Instantiate the object so that it's visible
            movingObj = Instantiate(obj, startPos, Quaternion.identity);

            // Set the start time of this trial
            trialStartTime = Time.time;
            Debug.Log("Trial " + trialNum + " starting at time " + trialStartTime);
        }
        else
        {
            Debug.Log("No more trials to execute");
            dataManager.saveData();
        }

    }

    public void CompleteTrial(float trialEndTime)
    {
        // Delete the old moving object
        DeleteMovingObj();

        // Add this trial to the data manager
        dataManager.addNewTrial(trialNum, speed, startPos.x, startPos.y, startPos.z, objName, trialStartTime, trialEndTime);

        // Increment trialNum so we don't initialize this trial again
        trialNum++;
    }

    // Use this for initialization
    void Start () {
        // Load the provided trial data
        trials = LoadTrialData(filepath);

        // Set the start time of the experiment
        dataManager.setStartTime(Time.time);

        // Initialize the first trial
        trialNum = 0;
        //InitializeTrial();
    }

    // Update is called once per frame
    void Update () {
        // Make sure the object exists before trying to update its position
        if (movingObj)
        {
            // Step size = speed * frame time
            float step = speed * Time.deltaTime;

            // Move the object 1 step closer to the target
            movingObj.position = Vector3.MoveTowards(movingObj.position, target.position, step);
        }
        else
        {
            // On the first update, initialize the first trial
            if (trialNum == 0)
            {
                InitializeTrial();
            }
        }
    }
}
