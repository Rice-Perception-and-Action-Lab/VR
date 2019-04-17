using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class RunExperiment : MonoBehaviour {

    // Set via the Unity editor
    public SaveData dataManager;            // The GameObject responsible for tracking trial responses
    public ManageUI uiManager;              // The GameObject responsible for handling any changes to the UI
    public Transform viveCamera;            // Position of the target (i.e., the Vive camera rig)
    public Transform cameraManager;
    public Transform subject;
    private Camera mainCamera;

    // The Config options
    private ReadConfig.Config config;       // The configuration file specifying certain experiment-wide parameters
    private string inputFile;               // A JSON file holding the information for every trial to be run

    // Experiment-Dependent Variables
    private float rate;                    // The framerate that we're moving the object at
    private ManageTrials.Trial[] trials;    // The input file converted to an array of Trial objects 
    private Transform headPos;              // The location of the camera rig relevant to the scene

    // Trial-Dependent Variables
    private int curTrial;                   // Track the number of the current trial being run
    private bool isRunning;                 // Tracks whether or not a trial is currently active
    private float trialStart;               // Track the time that the current trial began
    private string objName;                 // The name of the prefab object used for the given trial
    private Vector3 startPos;               // The initial position of the moving object for the current trial
    private Vector3 endPos;                 // The final position of the moving object for the current trial
    private float dist;                     // The distance between the start and end positions
    private Transform obj;                  // The prefab object that will be instantiated
    private Transform movingObj;            // The object that will approach the user
    private int stepCounter;
    private string posString;
    private float hideTime;
    private float stepHidden;
    private float finalStep;
    private float stepSize;
    float step;


    /**
     * Initializes all trial data once the experiment begins. This includes loading the
     * config file, loading in the trial-information, and setting all experiment-wide variables.
     */
    void Start()
    {
        // Set the target framerate for the application
        Application.targetFrameRate = 75;
        rate = 75.0f;

        mainCamera = Camera.main;

        // Load the config file
        string configFilepath = Application.dataPath + "/../config.json";
        config = GetComponent<ReadConfig>().LoadConfig(configFilepath.Replace("/", "\\"));

        // Set the feedback display configurations based on the config file
        uiManager.SetFeedbackColor(config.feedbackColor);
        uiManager.SetCanvasPosition(config.canvasPos[0], config.canvasPos[1], config.canvasPos[2]);
        uiManager.SetFeedbackSize(config.feedbackSize);

        // Load the data from the desired input file
        //uiManager.ShowMessage("Trial data file path is " + Application.dataPath + "/" + config.dataFile);
        trials = GetComponent<ManageTrials>().LoadTrialData(config.dataFile.Replace("/", "\\"), Time.time);

        // Initialize the TrialData array to be the correct size in the experiment's data manager
        dataManager.InitDataArray(trials.Length, Time.time);

        // Add the config info to the data manager
        dataManager.SetConfigInfo(config);

        // Initialize global variables
        curTrial = 0;
        isRunning = false;

        // Set the initial position of the participant 
        //cameraManager.position = new Vector3(config.initCameraPos[0], config.initCameraPos[1], config.initCameraPos[2]);
        cameraManager.position = viveCamera.TransformPoint(new Vector3(config.initCameraPos[0], config.initCameraPos[1], config.initCameraPos[2]));
        //subject.position = new Vector3(config.initCameraPos[0], config.initCameraPos[1], config.initCameraPos[2]);
        subject.position = viveCamera.TransformPoint(new Vector3(config.initCameraPos[0], config.initCameraPos[1], config.initCameraPos[2]));
        
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
            //Debug.Log("Initializing trial " + (curTrial + 1));

            //cameraManager.position = viveCamera.TransformPoint(new Vector3(config.initCameraPos[0], config.initCameraPos[1], config.initCameraPos[2]));
            //subject.position = new Vector3(config.initCameraPos[0], config.initCameraPos[1], config.initCameraPos[2]);
            //subject.position = viveCamera.TransformPoint(new Vector3(config.initCameraPos[0], config.initCameraPos[1], config.initCameraPos[2]));

            // Stop displaying the feedback text
            uiManager.ResetFeedbackMsg();

            // Get the current trial from the data array
            ManageTrials.Trial trial = trials[curTrial];

            // Set the object prefab that will be displayed
            objName = trial.objType;
            GameObject newObj = Resources.Load("Objects\\" + trial.objType) as GameObject;
            obj = newObj.transform;

            // Set the scale of the object
            obj.localScale = new Vector3(trial.objScale[0], trial.objScale[1], trial.objScale[2]);

            // Set the inital and final positions of the moving object
            // starts in the center
            //startPos = viveCamera.TransformDirection(new Vector3(trial.startPos[0], trial.startPos[1], trial.startPos[2] + (trial.objScale[2] / 2.0f) + 0.05f));
            Vector3 inputStartPos = new Vector3(trial.startPos[0], viveCamera.position.y, trial.startPos[2] + (trial.objScale[2] / 2.0f) + 0.05f);
            //inputStartPos.y = viveCamera.position.y;
            startPos = viveCamera.TransformPoint(inputStartPos);


            // starts to the side
            //startPos = viveCamera.TransformDirection(new Vector3(trial.startPos[0] - (trial.objScale[0] / 2.0f), trial.startPos[1], trial.startPos[2] + (trial.objScale[2] / 2.0f) + 0.05f));

            //endPos = viveCamera.TransformDirection(new Vector3(trial.endPos[0] - (trial.objScale[0] / 2.0f), trial.endPos[1], trial.endPos[2] + (trial.objScale[2] / 2.0f) + 0.05f));

            //Debug.Log("TESTING: " + trial.endPos[0] + " " + trial.objScale[0] + " " + (trial.objScale[0] / 2.0f) + " " + (trial.endPos[0] - (trial.objScale[0] / 2.0f)));
            //Vector3 inputEndPos2 = new Vector3(0 - (trial.objScale[0] / 2.0f), trial.endPos[1], trial.endPos[2] + (trial.objScale[2] / 2.0f) + 0.05f);
            //Vector3 inputEndPos2 = new Vector3(trial.endPos[0] - (trial.objScale[0] / 2.0f), trial.endPos[1], trial.endPos[2] + (trial.objScale[2] / 2.0f) + 0.05f);
            //Vector3 endPos2 = viveCamera.TransformPoint(inputEndPos2);

            Vector3 inputEndPos = new Vector3(trial.endPos[0], trial.endPos[1], trial.endPos[2] + (trial.objScale[2] / 2.0f) + 0.05f);
            //inputEndPos.y = viveCamera.position.y;
            endPos = viveCamera.TransformPoint(inputEndPos);


            //Debug.Log("AUTO ADJUST END: " + endPos2);
            //Debug.Log("MANUAL END POS: " + endPos);


            /*if (config.objMoveMode == 0)
            {
                endPos = viveCamera.position;
                endPos.x += (trial.objScale[0] / 2.0f);
                startPos.y = viveCamera.position.y;
            }*/


            

            startPos.y = viveCamera.position.y;
            endPos.y = viveCamera.position.y;

            Vector3 linePos = viveCamera.TransformPoint(new Vector3(viveCamera.position.x, viveCamera.position.y, viveCamera.position.z + 200));
            linePos.y = viveCamera.position.y;

            //Debug.DrawLine(viveCamera.position, linePos, Color.red, 500f, false);

            //Debug.Log("CAMERA POSITION: " + viveCamera.position.x + " " + viveCamera.position.y + " " + viveCamera.position.z);
            //Debug.Log("START POS: " + startPos.x + " " + startPos.y + " " + startPos.z);
            //Debug.Log("END POS: " + endPos.x + " " + endPos.y + " " + endPos.z);

            // Adjust the start/end positions of the object to account for the object's scale
            //startPos[2] -= (trial.objScale[2] / 2.0f) + 0.05f;  // .05 is approx. the size of the HMD
            //endPos[2] -= (trial.objScale[2] / 2.0f) + 0.05f;

            // Calculate the distance that the object must travel
            dist = Vector3.Distance(startPos, endPos);

            // Instantiate the object so that it's visible
            movingObj = Instantiate(obj, startPos, Quaternion.identity);

            // Set the start time of this trial so that it can be recorded by the data manager
            trialStart = Time.time;
            curTrial++;

            posString = movingObj.position.x + " " + movingObj.position.y + " " + movingObj.position.z;
            //Debug.Log("INITIAL POSITION: " + posString);

            // Reset the variables used in the repeating methods
            stepCounter = 0;
            posString = "";
            hideTime = 0.0f;
            finalStep = ((dist / trials[curTrial - 1].velocity) * rate);
            //Debug.Log("FINAL STEP: " + finalStep);
            stepSize = (1.0f / rate);
            step = trials[curTrial - 1].velocity * stepSize;

            // timeVisible is a negative number if the object should never disappear
            if (trials[curTrial - 1].timeVisible < 0)
            {
                stepHidden = -1;    // set stepHidden to be a step value that never occurs so the object will never be hidden
            }
            else
            {
                stepHidden = (trials[curTrial - 1].timeVisible * rate);
            }


            // Start calling the methods that will move the object and record head position
            float delay = (1.0f / rate);
            InvokeRepeating("MoveObjByStep", 0f, delay);
            InvokeRepeating("HeadTracking", 0f, delay);
            isRunning = true;
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
        Renderer rend = movingObj.gameObject.GetComponent<Renderer>();
        // Check that a moving object has been initialized and that it's actually
        // a game object to avoid errors
        if (movingObj && movingObj.gameObject && rend.enabled)
        {
            //Debug.Log("Deleting moving object for trial " + curTrial);
            //Debug.Log("POSITION: " + movingObj.position.x + " " + movingObj.position.y + " " + movingObj.position.z);
            
            rend.enabled = false;
        }
        else
        {
            //Debug.Log("ERROR: Could not delete moving object; object did not exist");
        }
    }


    /**
     * End the given trial.
     */
    public void CompleteTrial(float trialEnd, bool receivedResponse)
    {
        CancelInvoke("MoveObjByStep");
        CancelInvoke("HeadTracking");
        HideObj();

        // Display response time feedback to the participant
        float respTime = (trialEnd - trialStart);
        float actualTTC = (dist / trials[curTrial - 1].velocity);
        if (config.showFeedback) uiManager.DisplayFeedback(respTime, actualTTC);

        isRunning = false;

        // Add this trial's data to the data manager
        dataManager.AddTrial(trials[curTrial - 1], endPos, trialStart, trialEnd, dist);

        if (config.trackHeadPos) dataManager.WritePosData();
    }

    /**
     * Communicates with the controller script to determine if a trial is currently active.
     */
    public bool CheckTrialRunning()
    {
        return isRunning;
    }


    void MoveObjByStep()
    {
        //Debug.Log("Object Position: " + movingObj.position.x + " " + movingObj.position.y + " " + movingObj.position.z);
        //Debug.Log("Step Counter: " + stepCounter + " Step Hidden: " + stepHidden);
        //if (stepCounter >= stepHidden)
        if (stepCounter > stepHidden)
        {
            HideObj();
            posString = " " + movingObj.position.x + " " + movingObj.position.y + " " + movingObj.position.z;
            hideTime = (Time.time - trialStart);
            //Debug.Log("Time Visible: " + hideTime + " | " + posString);
            //Debug.Log("Trial " + (curTrial) + " | Time Visible: " + hideTime + " | Distance Traveled: " + Vector3.Distance(movingObj.position, startPos));
            stepCounter++;
        }
        //else

        if (true)
        {
            float fracTraveled = stepCounter / finalStep;
            movingObj.position = Vector3.Lerp(startPos, endPos, fracTraveled);
            movingObj.Rotate(-step * trials[curTrial - 1].velocity, 0.0f, 0.0f);
            stepCounter++;

            //if (stepCounter > finalStep)
            if (fracTraveled >= 1)
            {
                Debug.Log("Trial " + (curTrial) + " | Time Visible: " + (Time.time - trialStart) + " | Distance Traveled: " + Vector3.Distance(movingObj.position, startPos));
                HideObj();
                posString = movingObj.position.x + " " + movingObj.position.y + " " + movingObj.position.z;
                CancelInvoke("MoveObjByStep");
                float endTime = (Time.time - trialStart);
                //Debug.Log("TTC: " + (endTime - hideTime) + "  |  POSITION: " + posString);
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
