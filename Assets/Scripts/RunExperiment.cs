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
    public Transform cameraManager;         // Used to reposition the Vive's world location at the beginning of the experiment
    public Transform subject;               // Used to reposition the Vive's world location at the beginning of the experiment

    // The Config options
    private ReadConfig.Config config;       // The configuration file specifying certain experiment-wide parameters
    private string inputFile;               // A JSON file holding the information for every trial to be run

    // Experiment-Dependent Variables
    private float rate;                     // The framerate that we're moving the object at
    private ManageTrials.Trial[] trials;    // The input file converted to an array of Trial objects 
    private Transform headPos;              // The location of the camera rig relevant to the scene
    private bool expComplete;

    // Trial-Dependent Variables
    private int curTrial;                   // Track the number of the current trial being run
    private bool isRunning;                 // Tracks whether or not a trial is currently active
    private float trialStart;               // Track the time that the current trial began
    private string objName;                 // The name of the prefab object used for the given trial
    private Vector3[] startPosArr;          // The starting positions of all objects in a trial, in Vector3 form for easier reference than the float[] version stored with the object
    private Vector3[] endPosArr;            // The ending positions of all objects in a trial, in Vector3 form for easier reference than the float[] version stored with the object
    private Transform[] objs;               // The prefab objects that will be instantiated for a trial
    private Transform[] movingObjs;         // The array of objects for a trial once they have been instantiated
    private int numObjs;                    // the number of objects that are part of a trial
    private float stepSize;                 // The fraction that an object moves on every call of the MoveObjsByStep method; based on the target frame rate

    /**
     * Initializes all trial data once the experiment begins. This includes loading the
     * config file, loading in the trial-information, and setting all experiment-wide variables.
     */
    void Start()
    {
        // Set the target framerate for the application
        Application.targetFrameRate = 75;
        rate = 75.0f;

        // Set the step size for the objects' motion based on the rate
        stepSize = (1.0f / rate);

        // Load the config file
        string configFilepath = Application.dataPath + "/../config.json";
        config = GetComponent<ReadConfig>().LoadConfig(configFilepath.Replace("/", "\\"));

        // Set the feedback display configurations based on the config file
        uiManager.SetFeedbackColor(config.feedbackColor);
        uiManager.SetCanvasPosition(config.canvasPos[0], config.canvasPos[1], config.canvasPos[2]);
        uiManager.SetFeedbackSize(config.feedbackSize);

        // Load the data from the desired input file
        trials = GetComponent<ManageTrials>().LoadTrialData(config.dataFile.Replace("/", "\\"), Time.time);

        // Initialize the TrialData array to be the correct size in the experiment's data manager
        dataManager.InitDataArray(trials.Length, Time.time);

        // Add the config info to the data manager
        dataManager.SetConfigInfo(config);

        // Initialize global variables
        curTrial = 0;
        isRunning = false;
        expComplete = false;

        // Set the initial position of the participant 
        cameraManager.position = viveCamera.TransformPoint(new Vector3(config.initCameraPos[0], config.initCameraPos[1], config.initCameraPos[2]));
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
            // Stop displaying the feedback text
            uiManager.ResetFeedbackMsg();

            Debug.Log("Trial " + trials[curTrial].trialNum + " started");

            // Get the current trial from the data array
            ManageTrials.Trial trial = trials[curTrial];

            // Initialize all of the arrays for the objects in the trial
            numObjs = trial.objects.Length;
            objs = new Transform[numObjs];
            startPosArr = new Vector3[numObjs];
            endPosArr = new Vector3[numObjs];
            movingObjs = new Transform[numObjs];

            for (int i = 0; i < numObjs; i++)
            {
                ManageObjs.Obj curObj = trial.objects[i];

                // Set the object prefab that will be displayed
                GameObject newObj = Resources.Load("Objects\\" + curObj.objType) as GameObject;
                objs[i] = newObj.transform;

                // Set the scale of the object
                objs[i].localScale = new Vector3(curObj.objScale[0], curObj.objScale[1], curObj.objScale[2]);

                // Set the initial and final positions of the object
                Vector3 inputStartPos = new Vector3(curObj.startPos[0], viveCamera.position.y, curObj.startPos[2] + (curObj.objScale[2] / 2.0f) + 0.05f);
                startPosArr[i] = viveCamera.TransformPoint(inputStartPos);      // orient the start position based on the rotation/direction of the Vive
                Vector3 inputEndPos = new Vector3(curObj.endPos[0], viveCamera.position.y, curObj.endPos[2] + (curObj.objScale[2] / 2.0f) + 0.05f);
                endPosArr[i] = viveCamera.TransformPoint(inputEndPos);          // orient the end position based on the rotation/direction of the Vive

                // Adjust the height of the object to match the height of the camera
                startPosArr[i] = new Vector3(startPosArr[i].x, viveCamera.position.y, startPosArr[i].z);
                endPosArr[i] = new Vector3(endPosArr[i].x, viveCamera.position.y, endPosArr[i].z);

                // Calculate the distance that the object must travel
                curObj.dist = Vector3.Distance((Vector3)startPosArr[i], (Vector3)endPosArr[i]);

                // Instantiate the object so that it's visible
                movingObjs[i] = Instantiate(objs[i], startPosArr[i], Quaternion.identity);
                curObj.objVisible = true;
                curObj.objActive = true;

                // Set the variables that need to be used in the repeating method to move the objects
                curObj.step = curObj.velocity * stepSize;
                curObj.finalStep = ((curObj.dist / curObj.velocity) * rate);

                // If timeVisible is negative, the object should never disappear
                if (curObj.timeVisible < 0)
                {
                    curObj.stepHidden = -1;     // set stepHidden to be a step value that can never occur so that HideObject will never be called
                }
                else
                {
                    curObj.stepHidden = curObj.timeVisible * rate;
                }
            }

            // Set the start time of this trial so that it can be recorded by the data manager
            trial.trialStart = Time.time;
            curTrial++;

            // Call the repeating methods to move the objects and track head position
            float delay = (1.0f / rate);
            InvokeRepeating("MoveObjsByStep", 0.0f, delay);
            InvokeRepeating("HeadTracking", 0.0f, delay);

            // Set the trial as running
            isRunning = true;
        }
        else
        {
            // Do this check and then increment the trial num so data is only saved once
            if (curTrial == trials.Length)
            {
                expComplete = true;
                Debug.Log("Experiment complete");
                uiManager.DisplayCompletedMsg();
                dataManager.Save();
                curTrial++;
            }
        }
    }

    /**
     * Hide an object so that it is no longer visible but still exists in the world.
     */
     public void HideObj(Transform movingObj)
    {
        Renderer rend = movingObj.gameObject.GetComponent<Renderer>();

        // Check that the object actually exists to avoid null pointer exceptions
        if (movingObj && movingObj.gameObject && rend.enabled)
        {
            rend.enabled = false;
        }
    }

    /**
     * End the given trial by cancelling the repeating methods and saving the data.
     */
    public void CompleteTrial(float trialEnd, bool receivedResponse, string response)
    {
        Debug.Log("Trial " + trials[curTrial - 1].trialNum + " completed" + Environment.NewLine);
        CancelInvoke("MoveObjsByStep");
        CancelInvoke("HeadTracking");

        // Hide any objects that haven't already been hidden
        ManageObjs.Obj[] objs = trials[curTrial - 1].objects;
        for (int i = 0; i < objs.Length; i++)
        {
            if (objs[i].objVisible)
            {
                HideObj(movingObjs[i]);
            }
        }

        trials[curTrial - 1].trialEnd = trialEnd;
        trials[curTrial - 1].response = response;
        dataManager.AddTrial(trials[curTrial - 1]);
        isRunning = false;

        // Only save the head tracking data if that flag was set in the config file
        if (config.trackHeadPos) dataManager.WritePosData();
    }

    /**
     * Move each object in the current trial based on the object's parameters.
     */
     void MoveObjsByStep()
     {
        ManageObjs.Obj[] objs = trials[curTrial - 1].objects;
    
        for (int i = 0; i < objs.Length; i++)
        {
            ManageObjs.Obj curObj = objs[i];

            // Once an object has become inactive we no longer need to move it
            if (curObj.objActive)
            {
                // Hide the object once it has been visible for its defined timeVisible
                if (curObj.stepCounter > curObj.stepHidden && curObj.objVisible)
                {
                    HideObj(movingObjs[i]);
                    curObj.objVisible = false;
                }

                // Move the object forward another step
                float fracTraveled = curObj.stepCounter / curObj.finalStep;
                movingObjs[i].position = Vector3.Lerp(startPosArr[i], endPosArr[i], fracTraveled);
                movingObjs[i].Rotate(curObj.rotationSpeedX, curObj.rotationSpeedY, curObj.rotationSpeedZ);
				curObj.stepCounter++;

                // If the object has traveled the entire distance, it should no longer be moving
                if (fracTraveled >= 1)
                {
                    // Hide the object if it hasn't been hidden already
                    if (curObj.objVisible)
                    {
                        HideObj(movingObjs[i]);
                        curObj.objVisible = false;
                    }

                    // Set the object to inactive and decrement numObjs
                    curObj.objActive = false;
                    numObjs = numObjs - 1;
                }
            }
        }

        // Once numObjs reaches 0, all objects have finished moving and the repeating method can be canceled
        if (numObjs == 0)
        {
            CancelInvoke("MoveObjsByStep");
        }
     }

    /**
     * Communicates with the controller script to determine if a trial is currently active.
     */
    public bool CheckTrialRunning()
    {
        return isRunning;
    }

    /**
     * Gets the position of the Vive headset at a predefined interval and adds that data point to the
     * head position data file for the current trial.
     */
    void HeadTracking()
    {
        dataManager.AddHeadPos(Time.time, headPos.position, headPos.eulerAngles);
    }


    void OnApplicationQuit()
    {
        if (expComplete)
        {
            Debug.Log(Environment.NewLine + "Simulation shutting down after successful completion" + Environment.NewLine);
            Debug.Log("Total simulation time " + Time.time + " seconds");
        }
        else
        {
            Debug.Log(Environment.NewLine + "Simulation shutting down after UNSUCCESSFUL completion...Saving partial data..." + Environment.NewLine);
            Debug.Log("Total simulation time " + Time.time + " seconds");
            dataManager.Save();
        }

    }

}
