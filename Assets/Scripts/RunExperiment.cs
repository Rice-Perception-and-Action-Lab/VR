using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class RunExperiment : MonoBehaviour {

    public SaveData dataManager;        // The GameObject responsible for tracking trial responses
    public Transform target;            // Position of the target (i.e., the Vive camera rig)

    private string input_file = "U:\\vr_experiment\\VR\\Assets\\TrialData\\sample_input.json";          // A JSON file holding the information for every trial to be run
    private Trial[] trials;             // The input file converted to an array of Trial objects 
    private int curTrial;               // Track the number of the current trial being run
    private float trialStart;           // Track the time that the current trial began
    private bool waiting;               // Boolean to track whether we're waiting between trials
    private float waitTime;             // Timer to track how long we've been waiting
    private string objName;             // The name of the prefab object used for the given trial
    private Vector3 startPos;           // The initial position of the moving object for the current trial
    private Transform obj;              // The prefab object that will be instantiated
    private Transform movingObj;        // The object that will approach the user


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

            Debug.Log("len of trials is " + trialData.trials.Length);

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

            // Get the current trial from the data array
            Trial trial = trials[curTrial];

            // Set the inital position of the moving object
            startPos = new Vector3(0, 3, trial.finalDist);

            // Randomly choose the shape of the object to present
            System.Random r = new System.Random();
            int i = r.Next(0, trial.objects.Length);
            objName = trial.objects[i];

            // Set the object prefab that will be displayed
            GameObject newObj = Resources.Load(objName) as GameObject;
            obj = newObj.transform;

            // Instantiate the object so that it's visible
            movingObj = Instantiate(obj, startPos, Quaternion.identity);

            // Set the start time of this trial so that it can be 
            // recorded by the data manager
            trialStart = Time.time;
            Debug.Log("Trial " + curTrial + " starting at time " + trialStart);

            curTrial++;
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
            Debug.Log("Deleting moving object for trial " + curTrial);
            Destroy(movingObj.gameObject);
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
        HideObj();

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
     * Initializes all trial data once the experiment begins. Starts
     * tracking the time that events are occurring.
     */
    void Start()
    {
        Debug.Log("here");
        Debug.Log("input file: " + input_file);
        Debug.Log("time: " + Time.time);

        // Load the data from the desired input file
        this.trials = LoadTrialData(input_file, Time.time);

        Debug.Log("trial len :" + trials.Length);

        curTrial = 0;
        waiting = true;
        waitTime = 0.0f;
    }

    /**
     * Every frame, move the object and check if it should disappear 
     * from view. If the first trial hasn't been initialized yet, then
     * do so.
     */
     void Update()
    {
        // Make sure there is currently a trial running before attempting
        // to move an object
        if (movingObj)
        {
            // step size equals current trial's velocity * frame rate
            float step = trials[curTrial - 1].velocity * Time.deltaTime;

            // Move the object 1 step closer to the target
            movingObj.position = Vector3.MoveTowards(movingObj.position, target.position, step);

            // If the object has been visible for the time visible param, hide it
            if ((Time.time - trialStart) >= trials[curTrial - 1].timeVisible)
            {
                HideObj();
            }
        }
        else
        {
            // Don't check for a timeout if we're waiting to end the trial
            if (waiting)
            {
                // Only initialize a new trial after the waiting period is over
                if (waitTime >= 3.0f)
                {
                    waiting = false;
                    InitializeTrial();
                }
                else
                {
                    // Increment wait time
                    waitTime += Time.deltaTime;
                }
            }
            else
            {
                // Check for a timeout (i.e., no controller response for some specified amount of time)
                if ((curTrial - 1 < trials.Length) && (Time.time - trialStart) >= (trials[curTrial - 1].timeVisible * 2))
                {
                    Debug.Log("Timeout");
                    CompleteTrial(Time.time, false);
                }
            }
        }
    }

}
