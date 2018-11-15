﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class RunExperiment : MonoBehaviour {

    // Set via the Unity editor
    public SaveData dataManager;            // The GameObject responsible for tracking trial responses
    public Transform viveCamera;            // Position of the target (i.e., the Vive camera rig)

    // The Config options
    private Config config;                  // The configuration file specifying certain experiment-wide parameters
    private string inputFile;               // A JSON file holding the information for every trial to be run
    private bool targetCamera;              // A boolean to determine if the object follows the user's head or not

    // Experiment-Dependent Variables
    private Trial[] trials;                 // The input file converted to an array of Trial objects 
    private IEnumerator movementCoroutine;  // The coroutine responsible for moving the object in the world

    // Trial-Dependent Variables
    private int curTrial;                   // Track the number of the current trial being run
    private bool isRunning;                 // Tracks whether or not a trial is currently active
    private Vector3 targetPos;              // The target that the moving object aims for
    private float trialStart;               // Track the time that the current trial began
    private bool waiting;                   // Boolean to track whether we're waiting between trials
    private float waitTime;                 // Timer to track how long we've been waiting
    private string objName;                 // The name of the prefab object used for the given trial
    private Vector3 startPos;               // The initial position of the moving object for the current trial
    private Transform obj;                  // The prefab object that will be instantiated
    private Transform movingObj;            // The object that will approach the user

    [System.Serializable]
    public class Config
    {
        public string dataFile;
        public bool targetCamera;
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

    [System.Serializable]
    class TrialArray
    {
        // this wrapper class is a workaround for how Unity's JsonUtility
        // handles top-level JSON objects
        public Trial[] trials;
    }

    [System.Serializable]
    public class Trial
    {
        public int curTrial;            // the trial number
        public float finalDist;         // the total distance this object should travel (in meters)
        public float velocity;          // the speed the object is moving (in meters / second)
        public float timeVisible;       // the amount of time this object should be visible before disappearing
        public string[] objects;        // the names of the potential object prefabs that can be instantiated
        public float rotationSpeed;     // the speed at which the object should rotate each frame
    }

    /**
     * Given a path to a JSON file containing the parameters for each
     * trial in the experiment, creates a Trial object with the correct
     * parameters for each entry in the input file.
     */
     public Trial[] LoadTrialData(string path, float time)
    {
        try
        {
            StreamReader sr = new StreamReader(path);
            string jsonString = sr.ReadToEnd();
            TrialArray trialData = JsonUtility.FromJson<TrialArray>(jsonString);

            // Initialize the TrialData array to be the correct size in 
            // the experiment's data manager
            dataManager.InitDataArray(trialData.trials.Length, time);
            return trialData.trials;
        }
        catch (System.Exception e)
        {
            Debug.Log("exception?");
            print("Exception: " + e.Message);
            return null;
        }
    }

    /**
     * Initialize the given trial.
     */
    public void InitializeTrial()
    {
        // Check that we still have more trials to run
        if (curTrial < this.trials.Length)
        {
            Debug.Log("Initializing trial " + curTrial);

            // Set the target for this trial (will be updated each iteration of Update function if targetCamera is true)
            targetPos = new Vector3(viveCamera.position.x, viveCamera.position.y, viveCamera.position.z);
      
            // Get the current trial from the data array
            Trial trial = trials[curTrial];

            // Set the target position for the moving object
            if (targetCamera)
            {
                targetPos = viveCamera.position;
            }

            // Set the inital position of the moving object
            startPos = new Vector3(targetPos.x, targetPos.y, trial.finalDist);
            Vector3 endPos = new Vector3(targetPos.x, targetPos.y, targetPos.z);

            // Randomly choose the shape of the object to present
            System.Random r = new System.Random();
            int i = r.Next(0, trial.objects.Length);
            objName = trial.objects[i];

            // Set the object prefab that will be displayed
            GameObject newObj = Resources.Load("Objects\\" + objName) as GameObject;
            obj = newObj.transform;

            // Instantiate the object so that it's visible
            movingObj = Instantiate(obj, startPos, Quaternion.identity);

            isRunning = true;

            // Set the start time of this trial so that it can be 
            // recorded by the data manager
            trialStart = Time.time;
            curTrial++;

            movementCoroutine = MoveOverTime(endPos, (trial.finalDist / trial.velocity));
            StartCoroutine(movementCoroutine);
        }
        else
        {
            // Do this check and then increment the trial num so data is only saved once
            if (curTrial == trials.Length)
            {
                Debug.Log("Experiment complete");
                dataManager.Save();
                curTrial++;
            }
        }
    }

    public void HideObj()
    {
        // Check that a moving object has been initialized and that it's actually
        // a game object to avoid errors
        if (movingObj && movingObj.gameObject)
        {
            Debug.Log("Deleting moving object for trial " + curTrial);          // remove for testing
            //Destroy(movingObj.gameObject);                                      // remove for testing
            Renderer rend = movingObj.gameObject.GetComponent<Renderer>();
            rend.enabled = false;

            //rend.material.color = Color.black;                                // add for testing
        }
        else
        {
            Debug.Log("ERROR: Could not delete moving object; object did not exist");
        }
    }

    /**
     * End the given trial.
     */
    public void CompleteTrial(float trialEnd, bool receivedResponse)
    {
        // Delete the existing object in the trial if a button was pressed
        // before the object was hidden from view
        //HideObj();                          // remove for testing
        StopCoroutine(movementCoroutine);
        Destroy(movingObj.gameObject);      // remove for testing

        isRunning = false;

        // Add this trial's data to the data manager
        dataManager.AddTrial(curTrial, trials[curTrial - 1].finalDist, startPos, trials[curTrial - 1].velocity,
            trials[curTrial - 1].timeVisible, objName, trialStart, trialEnd, receivedResponse);

        // Increment trial number so we don't run this trial again
        //curTrial++;
        //InitializeTrial();
        waiting = true;
        waitTime = 0.0f;
    }

    /**
     * Checks whether a trial is currently running as a helper method for
     * communicating with the TrackControllerResponse script. Returns true if
     * a trial is active or false if no trial is active and we are waiting for
     * user input to move on.
     */
    public bool CheckTrialRunning()
    {
        return isRunning;
    }

    /**
     * Initializes all trial data once the experiment begins. Starts
     * tracking the time that events are occurring.
     */
    void Start()
    {
        // Load the config file
        this.config = LoadConfig("config.json");

        // Set the configurations based on the config file
        inputFile = config.dataFile;
        targetCamera = config.targetCamera;

        // Load the data from the desired input file
        this.trials = LoadTrialData(inputFile, Time.time);

        // Initialize global variables
        curTrial = 0;
        waiting = true;
        waitTime = 0.0f;
        isRunning = false;
    }

    /**
     * Move the object over a fixed period of time (determined by the distance to travel and velocity
     * of the object). Hide the object after a certain period of time has passed.
     */
    public IEnumerator MoveOverTime(Vector3 finalPos, float seconds)
    {
        float elapsedTime = 0.0f;
        float trialTTC = 0.0f;
        bool objHidden = false;
        Vector3 adjustment = new Vector3(0.0f, 0.0f, (movingObj.localScale.z / 4.0f));
        Vector3 startingPos = movingObj.position;
        while (elapsedTime < seconds)
        {
            if (movingObj.position.z > adjustment.z)
            {
                if (!objHidden && elapsedTime >= trials[curTrial - 1].timeVisible)
                {
                    Debug.Log("TIME VISIBLE: " + elapsedTime);
                    Debug.Log("POSITION: " + movingObj.position.x + " " + movingObj.position.y + " " + movingObj.position.z);
                    HideObj();
                    objHidden = true;
                }

                movingObj.position = Vector3.Lerp(startingPos, finalPos, (elapsedTime / seconds));
            }

            // Update the rotation of the object every time
            movingObj.Rotate(-trials[curTrial - 1].rotationSpeed * Time.deltaTime, 0, 0, Space.Self);

            elapsedTime += Time.deltaTime;
            if (objHidden)
                trialTTC += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        movingObj.position = finalPos;
        Debug.Log("ELAPSED TIME: " + elapsedTime);
        Debug.Log("TTC: " + trialTTC);
    }
}