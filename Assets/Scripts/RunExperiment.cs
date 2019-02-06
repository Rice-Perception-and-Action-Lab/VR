using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RunExperiment : MonoBehaviour {

    // Set via the Unity editor
    public SaveData dataManager;            // The GameObject responsible for tracking trial responses
    public ManageUI uiManager;             // The GameObject responsible for handling any changes to the UI
    public Transform viveCamera;            // Position of the target (i.e., the Vive camera rig)
    public Transform cameraManager;

    // The Config options
    private ReadConfig.Config config;       // The configuration file specifying certain experiment-wide parameters
    private string inputFile;               // A JSON file holding the information for every trial to be run
    private bool targetCamera;              // A boolean to determine if the object follows the user's head or not

    // Experiment-Dependent Variables
    private ManageTrials.Trial[] trials;    // The input file converted to an array of Trial objects 
    private IEnumerator movementCoroutine;  // The coroutine responsible for moving the object in the world
    private Transform headPos;              // The location of the camera rig relevant to the scene

    // Trial-Dependent Variables
    private int curTrial;                   // Track the number of the current trial being run
    private bool isRunning;                 // Tracks whether or not a trial is currently active
    private Vector3 targetPos;              // The target that the moving object aims for
    private float trialStart;               // Track the time that the current trial began
    private string objName;                 // The name of the prefab object used for the given trial
    private Vector3 startPos;               // The initial position of the moving object for the current trial
    private Transform obj;                  // The prefab object that will be instantiated
    private Transform movingObj;            // The object that will approach the user


    private int stepCounter;
    private string posString;
    private float hideTime;


    /**
     * Initializes all trial data once the experiment begins. This includes loading the
     * config file, loading in the trial-information, and setting all experiment-wide variables.
     */
    void Start()
    {
        // Set the target framerate for the application
        Application.targetFrameRate = 75;

        // Load the config file
        config = GetComponent<ReadConfig>().LoadConfig("config.json");

        // Set the configurations based on the config file
        targetCamera = config.targetCamera;
        uiManager.SetFeedbackColor(config.feedbackColor);
        uiManager.SetCanvasPosition(config.canvasX, config.canvasY, config.canvasZ);
        uiManager.SetFeedbackSize(config.feedbackSize);

        // Load the data from the desired input file
        trials = GetComponent<ManageTrials>().LoadTrialData(config.dataFile, Time.time);
        //this.trials = LoadTrialData(inputFile, Time.time);

        // Initialize the TrialData array to be the correct size in 
        // the experiment's data manager
        dataManager.InitDataArray(trials.Length, Time.time);

        // Add the config info to the data manager
        dataManager.SetConfigInfo(config.subjNum, config.subjSex, config.dataFile, config.showFeedback, config.feedbackColor, config.targetCamera, config.trackHeadPos);

        // Initialize global variables
        curTrial = 0;
        isRunning = false;

        // Set the initial position of the participant
        cameraManager.position = new Vector3(config.initCameraX, config.initCameraY, config.initCameraZ);

        // Set the head position transform to track the participant's movements
        headPos = GameObject.Find("Camera (eye)").transform;
    }


    /**
     * Initialize the current trial.
     */
    public void InitializeTrial()
    {
        // Check that we still have more trials to run
        if (curTrial < this.trials.Length)
        {
            Debug.Log("Initializing trial " + curTrial);

            // Stop displaying the feedback text
            uiManager.ResetFeedbackMsg();

            // Set the target for this trial (will be updated each iteration of Update function if targetCamera is true)
            targetPos = new Vector3(viveCamera.position.x, viveCamera.position.y, viveCamera.position.z);
            Debug.Log("TARGET POSITION: " + viveCamera.position.x + " " + viveCamera.position.y + " " + viveCamera.position.z);

            // Get the current trial from the data array
            ManageTrials.Trial trial = trials[curTrial];

            // Set the target position for the moving object
            if (targetCamera)
            {
                targetPos = viveCamera.position;
            }

            // Set the inital position of the moving object
            startPos = new Vector3(targetPos.x, targetPos.y, trial.startDist);
            Vector3 endPos = new Vector3(targetPos.x, targetPos.y, targetPos.z);

            // Set the object prefab that will be displayed
            objName = trial.objType;
            GameObject newObj = Resources.Load("Objects\\" + trial.objType) as GameObject;
            obj = newObj.transform;

            // Set the scale of the object
            obj.localScale = new Vector3(trial.objScaleX, trial.objScaleY, trial.objScaleZ);

            // Instantiate the object so that it's visible
            movingObj = Instantiate(obj, startPos, Quaternion.identity);

            isRunning = true;

            // Set the start time of this trial so that it can be recorded by the data manager
            trialStart = Time.time;
            curTrial++;

            stepCounter = 0;
            posString = "";
            hideTime = 0.0f;

            float delay = (1.0f / 75.0f);
            InvokeRepeating("MoveObjByStep", 0f, delay);
            InvokeRepeating("HeadTracking", 0f, delay);
        }
        else
        {
            // Do this check and then increment the trial num so data is only saved once
            if (curTrial == trials.Length)
            {
                Debug.Log("Experiment complete");
                uiManager.DisplayCompletedMsg();
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
            Debug.Log("POSITION: " + movingObj.position.x + " " + movingObj.position.y + " " + movingObj.position.z);
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
        CancelInvoke("MoveObjByStep");
        CancelInvoke("HeadTracking");
        // Delete the existing object in the trial if a button was pressed
        // before the object was hidden from view
        HideObj();                          // remove for testing
        //StopCoroutine(movementCoroutine);
        //Destroy(movingObj.gameObject);      // remove for testing

        float respTime = (trialEnd - trialStart);
        float actualTTC = (trials[curTrial - 1].startDist / trials[curTrial - 1].velocity);

        // Display response time feedback to the participant
        if (config.showFeedback) uiManager.DisplayFeedback(respTime, actualTTC);

        isRunning = false;

        // Add this trial's data to the data manager
        dataManager.AddTrial(curTrial, trials[curTrial - 1].startDist, startPos, trials[curTrial - 1].velocity, trials[curTrial - 1].rotationSpeed,
            trials[curTrial - 1].timeVisible, objName, trialStart, trialEnd, receivedResponse, respTime);

        if (config.trackHeadPos) dataManager.WritePosData();
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


    void MoveObjByStep()
    {
        float stepHidden = (trials[curTrial - 1].timeVisible * 75);
        float finalStep = ((trials[curTrial - 1].startDist / trials[curTrial - 1].velocity) * 75);
        float stepSize = (1.0f / 75.0f);        

        if (stepCounter == stepHidden)
        {
            HideObj();
            posString = " " + movingObj.position.x + " " + movingObj.position.y + " " + movingObj.position.z;
            hideTime = (Time.time - trialStart);
            Debug.Log("Time Visible: " + hideTime + " | " + posString);
            stepCounter++;
        }
        else
        {
            float step = trials[curTrial - 1].velocity * stepSize;
            movingObj.position -= new Vector3(0.0f, 0.0f, step);
            movingObj.Rotate(-step * trials[curTrial - 1].velocity, 0.0f, 0.0f);
            stepCounter++;

            if (stepCounter > finalStep)
            {
                CancelInvoke("MoveObjByStep");
                float endTime = (Time.time - trialStart);
                Debug.Log("TTC: " + (endTime - hideTime) + "  |  POSITION: " + posString);
                stepCounter = 0;
                posString = "";
                hideTime = 0.0f;
            }
        }
    }


    int marker = 0;

    void OutputTime()
    {
        marker++;
        Debug.Log(Time.time);
        if (marker == 75)
        {
            CancelInvoke();
            float delay = (1.0f / 75.0f);
            InvokeRepeating("OutputTime", 0f, delay);
        }
    }

    void HeadTracking()
    {
        dataManager.AddHeadPos(Time.time, headPos.position, headPos.eulerAngles);
    }
}
