using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Valve.VR;
using System.Threading;

public class RunExperiment : MonoBehaviour {

    // Set via the Unity editor
    public SaveData dataManager;            // The GameObject responsible for tracking trial responses
    public ManageUI uiManager;              // The GameObject responsible for handling any changes to the UI
    public GameObject movingObj;

    public Transform viveCamera;            // Position of the target (i.e., the Vive camera rig)
    public Transform cameraManager;         // Used to reposition the Vive's world location at the beginning of the experiment
    public Transform subject;               // Used to reposition the Vive's world location at the beginning of the experiment

    // The Config options
    public ReadConfig.Config config;       // The configuration file specifying certain experiment-wide parameters
    private string inputFile;               // A JSON file holding the information for every trial to be run

    // Experiment-Dependent Variables
    private float rate;                     // The framerate that we're moving the object at
    public ManageTrials.Trial[] trials;    // The input file converted to an array of Trial objects 
    private Transform headPos;              // The location of the camera rig relevant to the scene

    private bool expComplete;
    [SerializeField] private GameObject road;   // The road object for the scene (reference for design decision: https://akbiggs.silvrback.com/please-stop-using-gameobject-find) 
    [SerializeField] private GameObject ground; // The ground object for the scene

    // Trial-Dependent Variables

    private int curTrial;                   // Track the number of the current trial being run
    public bool isRunning;                 // Tracks whether or not a trial is currently active
    private float trialStart;               // Track the time that the current trial began
    private string objName;                 // The name of the prefab object used for the given trial
    private Vector3[] startPosArr;          // The starting positions of all objects in a trial, in Vector3 form for easier reference than the float[] version stored with the object
    private Vector3[] endPosArr;            // The ending positions of all objects in a trial, in Vector3 form for easier reference than the float[] version stored with the object
    private Transform[] objs;               // The prefab objects that will be instantiated for a trial
    private Transform[] movingObjs;         // The array of objects for a trial once they have been instantiated
    private int numObjs;                    // the number of objects that are part of a trial

    private float stepSize;                     // The fraction that an object moves on every call of the MoveObjsByStep method; based on the target frame rate
    private float hideTime;
    private string posString;
    private float ttcActual;                //the theoretical TTC calculated from startPos, time visible, and velocity
    private float ttcActualSim;             //the TTC calculated by the simulator when the endPos is 0,0,0
    private float estimate;                 //the calculation of the participant's TTC estimate NOTE: 1 object PM Scenes only
    private float timeVisible;              //the time the object is visible NOTE: 1 object PM Scenes only
    public bool pmVisible;                  //True if the object is still visible on the screen. Used in TrackControllerResponse

    private Double[][] positions;            // positions array for custom motion
            // Testing out circular motion
            // {new Vector3(0, 0, 1), new Vector3(((float)(Math.Sqrt(2) / 2)), 0, ((float) (Math.Sqrt(2) / 2))),
            // new Vector3(1, 0, 0), new Vector3(((float)(Math.Sqrt(2) / 2)), 0, ((float)(Math.Sqrt(2) / -2))), new Vector3(0, 0, -1), new Vector3(((float)(Math.Sqrt(2) / -2)), 0, ((float)(Math.Sqrt(2) / -2))),
            // new Vector3(-1, 0, 0), new Vector3(((float)(Math.Sqrt(2) / -2)), 0, ((float)(Math.Sqrt(2) / 2))), new Vector3(0, 0, 1)};         
    private float curFrame;
    private int cusMotArrayIndex;

    /**
     * Initializes all trial data once the experiment begins. This includes loading the
     * config file, loading in the trial-information, and setting all experiment-wide variables.
     */
    void Start()
    {
        // Set the target framerate for the application
        Application.targetFrameRate = 90;
        rate = 90.0f;

        // Set the step size for the objects' motion based on the rate
        stepSize = (1.0f / rate);

        // Load the config file
        string configFilepath = Application.dataPath + "/config.json";
        config = GetComponent<ReadConfig>().LoadConfig(configFilepath.Replace("/", "\\"));

        if (config.debugging) { Debug.Log(configFilepath); }

        // Set the feedback display configurations based on the config file
        uiManager.SetFeedbackColor(config.feedbackColor);
        uiManager.SetFeedbackSize(config.feedbackSize);
        if (config.cameraLock)
        {
            uiManager.SetFeedbackPosition(config.feedbackPos[0], config.feedbackPos[1], config.feedbackPos[2], true);

        }
        else
        {
            uiManager.SetFeedbackPosition(config.feedbackPos[0], config.feedbackPos[1], config.feedbackPos[2]);
        }

        // Load the data from the desired input file
        trials = GetComponent<ManageTrials>().LoadTrialData(config.trialFile.Replace("/", "\\"), Time.time);

        // Initialize the TrialData array to be the correct size in the experiment's data manager
        dataManager.InitDataArray(trials.Length, Time.time);

        // Add the config info to the data manager
        dataManager.SetConfigInfo(config);

        // Initialize global variables
        curTrial = 0;
        isRunning = false;
        expComplete = false;

        // Set the initial position of the participant 
        //cameraManager.position = viveCamera.TransformPoint(new Vector3(config.initCameraPos[0], config.initCameraPos[1], config.initCameraPos[2]));
        //subject.position = viveCamera.TransformPoint(new Vector3(config.initCameraPos[0], config.initCameraPos[1], config.initCameraPos[2]));
        // Set the head position transform to track the participant's movements
        headPos = GameObject.Find("Camera (eye)").transform;
        movingObj = GameObject.Find("MovingObj");


        // Set up environment.
        if (config.ground) // Toggle ground visibility.
        {
           ground.SetActive(true); // Make the ground visible.
        }
        else
        {
           ground.SetActive(false); // Toggle off ground visibility.
        }

        if (config.road) // Toggle and set up road.
        {
            road.SetActive(true); // Make the ground visible.
            road.transform.position = new Vector3(config.roadPos[0], config.roadPos[1], config.roadPos[2]); // Set position.
        }
        else
        {
            road.SetActive(false); // Toggle off road visibility.
        }

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

            Debug.Log("Trial " + trials[curTrial].trialNum + ": " + trials[curTrial].trialName + " started");

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

                // Set current frame to 0.
                curFrame = 0;

                if (trial.customMot) // Check for custom motion configurations.
                {
                    // Set custom motion array index to 0.
                    cusMotArrayIndex = 0;
                    // Set custom motion array (called positions).
                    ReadCustomPositions("Assets/Trials/Custom_Motion_Positions/" + trial.customFile);
                }


                // Set the object prefab that will be displayed
                GameObject newObj = Resources.Load("Objects\\" + curObj.objType) as GameObject;
                objs[i] = newObj.transform;

                // Set the scale of the object
                objs[i].localScale = new Vector3(curObj.objScale[0], curObj.objScale[1], curObj.objScale[2]);

                // Set the object rotation.
                objs[i].localEulerAngles = new Vector3(curObj.objRot[0], curObj.objRot[1], curObj.objRot[2]);
                if (config.debugging) { Debug.Log("rotation: " + objs[i].localEulerAngles.x + " " + objs[i].localEulerAngles.y + " " + objs[i].localEulerAngles.z); }

                if (config.cameraLock)
                {
                    // Set the initial and final positions of the object
                    Vector3 inputStartPos = new Vector3(curObj.startPos[0], viveCamera.position.y + curObj.startPos[1], curObj.startPos[2]);
                    Vector3 inputEndPos = new Vector3(curObj.endPos[0], viveCamera.position.y + curObj.endPos[1], curObj.endPos[2]);

                    if (curObj.offsetZ)
                    {
                        if (inputStartPos.z != inputEndPos.z) // Can't calculate offset if doesn't move.
                        {
                            // Get size of model
                            Renderer render = objs[i].GetComponent<Renderer>();
                            Vector3 objSize = render.bounds.size;

                            // Determine where the front of the object is (direction is either 1 or -1). Since offset is either added or subtracted
                            // from the center depending on the direction of your velocity, this calculates the correct sign of the offset.
                            float direction = (inputStartPos.z - inputEndPos.z) / Mathf.Abs(inputStartPos.z - inputEndPos.z);

                            // Calculate offset. Assumes the object is symmetric and the object's position in Unity is the center of the object.
                            float offset = objSize.z / 2;

                            // Set the initial and final positions of the object with the z offset.
                            Vector3 newStartVector = new Vector3(inputStartPos.x, inputStartPos.y, inputStartPos.z + (direction * offset));
                            Vector3 newEndVector = new Vector3(inputEndPos.x, inputEndPos.y, inputEndPos.z + (direction * offset));

                            // Set new vectors in their respective arrays.
                            inputStartPos = newStartVector;
                            inputEndPos = newEndVector;
                        }
                    }

                    startPosArr[i] = viveCamera.TransformPoint(inputStartPos);      // orient the start position based on the rotation/direction of the Vive
                    endPosArr[i] = viveCamera.TransformPoint(inputEndPos);          // orient the end position based on the rotation/direction of the Vive

                    // Adjust the height of the object to match the height of the camera
                    startPosArr[i] = new Vector3(startPosArr[i].x, viveCamera.position.y + curObj.startPos[1], startPosArr[i].z);
                    endPosArr[i] = new Vector3(endPosArr[i].x, viveCamera.position.y + curObj.endPos[1], endPosArr[i].z);

                }
                else
                {
                    /**
                    * Begin calculating offsets. --------------------------------------------------------------------------------------------------------------
                    */

                    // Get size of model
                    Renderer render = objs[i].GetComponent<Renderer>();
                    Vector3 objSize = render.bounds.size;
                    if (config.debugging) { Debug.Log("Render bounds Size: " + render.bounds.size.x + " " + render.bounds.size.y + " " + render.bounds.size.z); }

                    // Initialize startPosArr and endPosArr with a copy of the object's current start and end positions, respectively.
                    startPosArr[i] = new Vector3(curObj.startPos[0], curObj.startPos[1], curObj.startPos[2]);
                    endPosArr[i] = new Vector3(curObj.endPos[0], curObj.endPos[1], curObj.endPos[2]);

                    if (curObj.offsetX)
                    {
                        if (startPosArr[i].x != endPosArr[i].x) // Can't calculate offset if doesn't move.
                        {
                            if (config.debugging) { Debug.Log("inside the offset"); }
                            if (config.debugging) { Debug.Log(startPosArr[i].x); }
                            if (config.debugging) { Debug.Log(endPosArr[i].x); }
                            // Determine where the front of the object is (direction is either 1 or -1). Since offset is either added or subtracted
                            // from the center depending on the direction of your velocity, this calculates the correct sign of the offset.
                            float direction = (startPosArr[i].x - endPosArr[i].x) / Mathf.Abs(startPosArr[i].x - endPosArr[i].x);

                            // Calculate offset. Assumes the object is symmetric and the object's position in Unity is the center of the object.
                            float offset = objSize.x / 2;

                            // Set the initial and final positions of the object with the x offset.
                            Vector3 newStartVector = new Vector3(startPosArr[i].x + (direction * offset), startPosArr[i].y, startPosArr[i].z);
                            Vector3 newEndVector = new Vector3(endPosArr[i].x + (direction * offset), endPosArr[i].y, endPosArr[i].z);

                            // Set new vectors in their respective arrays.
                            startPosArr[i] = newStartVector;
                            endPosArr[i] = newEndVector;

                        }

                        if (config.debugging) { Debug.Log("X offset Start Pos: " + startPosArr[i].x + " " + startPosArr[i].y + " " + startPosArr[i].z); }
                        if (config.debugging) { Debug.Log("X offset End Pos: " + endPosArr[i].x + " " + endPosArr[i].y + " " + endPosArr[i].z); }

                    }

                    if (curObj.offsetY)
                    {

                        if (startPosArr[i].y != endPosArr[i].y) // Can't calculate offset if doesn't move.
                        {

                            // Determine where the front of the object is (direction is either 1 or -1). Since offset is either added or subtracted
                            // from the center depending on the direction of your velocity, this calculates the correct sign of the offset.
                            float direction = (startPosArr[i].y - endPosArr[i].y) / Mathf.Abs(startPosArr[i].y - endPosArr[i].y);

                            // Calculate offset. Assumes the object is symmetric and the object's position in Unity is the center of the object.
                            float offset = objSize.y / 2;

                            // Set the initial and final positions of the object with the y offset.
                            Vector3 newStartVector = new Vector3(startPosArr[i].x, startPosArr[i].y + (direction * offset), startPosArr[i].z);
                            Vector3 newEndVector = new Vector3(endPosArr[i].x, endPosArr[i].y + (direction * offset), endPosArr[i].z);

                            // Set new vectors in their respective arrays.
                            startPosArr[i] = newStartVector;
                            endPosArr[i] = newEndVector;
                        }

                        if (config.debugging) { Debug.Log("Y Offset Start Pos: " + startPosArr[i].x + " " + startPosArr[i].y + " " + startPosArr[i].z); }
                        if (config.debugging) { Debug.Log(" Y offset End Pos: " + endPosArr[i].x + " " + endPosArr[i].y + " " + endPosArr[i].z); }

                    }

                    if (curObj.offsetZ)
                    {
                        if (startPosArr[i].z != endPosArr[i].z) // Can't calculate offset if doesn't move.
                        {
                            // Determine where the front of the object is (direction is either 1 or -1). Since offset is either added or subtracted
                            // from the center depending on the direction of your velocity, this calculates the correct sign of the offset.
                            float direction = (startPosArr[i].z - endPosArr[i].z) / Mathf.Abs(startPosArr[i].z - endPosArr[i].z);

                            // Calculate offset. Assumes the object is symmetric and the object's position in Unity is the center of the object.
                            float offset = objSize.z / 2;

                            // Set the initial and final positions of the object with the z offset.
                            Vector3 newStartVector = new Vector3(startPosArr[i].x, startPosArr[i].y, startPosArr[i].z + (direction * offset));
                            Vector3 newEndVector = new Vector3(endPosArr[i].x, endPosArr[i].y, endPosArr[i].z + (direction * offset));

                            // Set new vectors in their respective arrays.
                            startPosArr[i] = newStartVector;
                            endPosArr[i] = newEndVector;
                        }
                    }

                }
               
			   /**
                 * End calculating offsets. --------------------------------------------------------------------------------------------------------------
                 */

                // Calculate the distance that the object must travel
                curObj.dist = Vector3.Distance((Vector3)startPosArr[i], (Vector3)endPosArr[i]);

                if (config.debugging) { Debug.Log("Start Pos: " + startPosArr[i].x + " " + startPosArr[i].y + " " + startPosArr[i].z); }
                if (config.debugging) { Debug.Log("End Pos: " + endPosArr[i].x + " " + endPosArr[i].y + " " + endPosArr[i].z); }

                // Instantiate the object so that it's visible
                movingObjs[i] = Instantiate(objs[i], startPosArr[i], objs[i].localRotation); // Important to make sure these are correct variables.

                curObj.objVisible = true;
                curObj.objActive = true;
                if (config.feedbackType == 1) { pmVisible = true; }

                // Set the variables that need to be used in the repeating method to move the objects
                curObj.step = curObj.velocity * stepSize;
                curObj.finalStep = ((curObj.dist / curObj.velocity) * rate);

                //calculate theoretical ttcActual
                ttcActual = ((curObj.startPos[2] - (curObj.timeVisible * curObj.velocity)) / curObj.velocity);
                if (config.debugging) { Debug.Log("TTC: " + ttcActual); }

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
                if (config.debugging) { Debug.Log("Experiment complete"); }
                uiManager.ShowMessage("Experiment complete");
                dataManager.Save(false);
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

    public void CompleteTrial(float trialEnd, bool receivedResponse, string response, string confidence)
    {

        Debug.Log("Trial " + trials[curTrial - 1].trialNum + " completed" + Environment.NewLine);
        CancelInvoke("MoveObjsByStep");
        CancelInvoke("HeadTracking");

        HideAllObjs();

        trials[curTrial - 1].trialEnd = trialEnd;
        trials[curTrial - 1].response = response;
        trials[curTrial - 1].confidence = confidence;
        dataManager.AddTrial(trials[curTrial - 1]);
        isRunning = false;


        estimate = (trials[curTrial - 1].trialEnd - trials[curTrial - 1].trialStart) - timeVisible;

        if (config.debugging)
        {
            Debug.Log("Trial Start: " + trials[curTrial - 1].trialStart + Environment.NewLine);
            Debug.Log("Time Visible: " + timeVisible + Environment.NewLine);
            Debug.Log("Trial End: " + trials[curTrial - 1].trialEnd + Environment.NewLine);
            Debug.Log("TTC Estimate: " + estimate + Environment.NewLine);
        }

        if (config.showFeedback)
        {
            if (config.feedbackType == 1) { uiManager.DisplayPMFeedback(estimate, ttcActual); }
            if (config.feedbackType == 2) { uiManager.DisplayLRFeedback(response, trials[curTrial - 1].corrAns); }
        }
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
                    hideTime = (Time.time - trials[curTrial - 1].trialStart);
                    if (config.debugging) { Debug.Log("Time Hidden: " + hideTime); }
                    HideObj(movingObjs[i]);
                    curObj.objVisible = false;
                    if (config.feedbackType == 1) { pmVisible = false; }
                }

                // Move the object forward another step
                if (trials[curTrial - 1].customMot) { CustomMotionMov(curObj, i); } // Custom motion movement
                else { LinearMotionMov(curObj, i); } // Linear motion movement
            }
        }

        // Once numObjs reaches 0, all objects have finished moving and the repeating method can be canceled
        if (numObjs == 0)
        {
            CancelInvoke("MoveObjsByStep");
        }
    }
    /**
     * Moves the object in a linear path.
     */
    public void LinearMotionMov(ManageObjs.Obj curObj, int i)
    {
        float fracTraveled = curObj.stepCounter / curObj.finalStep;
        movingObjs[i].position = Vector3.Lerp(startPosArr[i], endPosArr[i], fracTraveled);
        movingObjs[i].Rotate(curObj.rotationSpeedX, curObj.rotationSpeedY, curObj.rotationSpeedZ);
        curObj.stepCounter++;

        // If the object has traveled the entire distance, it should no longer be moving
        if (fracTraveled >= 1) { StopObjMov(curObj, i);}
    }
    /**
     * Moves the object in a custom motion path.
     */
    public void CustomMotionMov(ManageObjs.Obj curObj, int i)
    {
        float duration = trials[curTrial - 1].customDur;
        // Be careful, cusMotArrayIndex refers to the positions array index. i refers to the trial object.
        float totalFrames = duration * rate;
        float framesPerPoint = totalFrames / (positions.Length - 1);
        float fracTraveled = curObj.stepCounter / framesPerPoint;

        if (config.debugging) { Debug.Log("total frames is: " + framesPerPoint); }
        if (config.debugging) { Debug.Log("current frame is: " + curObj.stepCounter); }

        if (fracTraveled >= 1) // Move onto the next position in the array.
        {
            cusMotArrayIndex++;
            if (config.debugging) { Debug.Log("Index position is: " + positions[cusMotArrayIndex]); }
            if (config.debugging) { Debug.Log("Index is: " + cusMotArrayIndex); }
            curObj.stepCounter = 0; // Reset the counter to 0 for the next segment.
            fracTraveled = 0;
        }

        // Once we hit the second to last element of the array, it should no longer be moving
        if (cusMotArrayIndex + 2 >= positions.Length) { StopObjMov(curObj, i); }

        else // Move the object forward another step
        {
            if (config.debugging) { Debug.Log("Index is: " + cusMotArrayIndex); }
            if (config.debugging) { Debug.Log("fraction traveled inside: " + fracTraveled); }

            Vector3 initPos = new Vector3((float) positions[cusMotArrayIndex][0], (float) positions[cusMotArrayIndex][1], (float) positions[cusMotArrayIndex][2]);
            Vector3 nextPos = new Vector3((float)positions[cusMotArrayIndex + 1][0], (float)positions[cusMotArrayIndex + 1][1], (float)positions[cusMotArrayIndex + 1][2]);

            movingObjs[i].position = Vector3.Lerp(initPos, nextPos, fracTraveled);
            if (config.debugging) { Debug.Log("Lerped position is: " + movingObjs[i].position); }

            movingObjs[i].Rotate((float) positions[cusMotArrayIndex][3], (float) positions[cusMotArrayIndex][4], (float) positions[cusMotArrayIndex][5]);
            curObj.stepCounter++;
        }
    }

    /**
     * Stops the object's movement.
     */
    public void StopObjMov(ManageObjs.Obj curObj, int i)
    {
        float endTime = (Time.time - trials[curTrial - 1].trialStart);
        ttcActualSim = (endTime - hideTime);
        if (config.debugging) { Debug.Log("TTC (simulator): " + ttcActualSim + " Valid when end is 0,0,0"); }


        // Hide the object if it hasn't been hidden already
        if (curObj.objVisible)
        {
            HideObj(movingObjs[i]);
            curObj.objVisible = false;
            if (config.feedbackType == 1) { pmVisible = false; }
        }

        // Set the object to inactive and decrement numObjs
        curObj.objActive = false;
        numObjs = numObjs - 1;
    }

    public void HideAllObjs()
    {
        // Hide any objects that haven't already been hidden
        ManageObjs.Obj[] objs = trials[curTrial - 1].objects;
        for (int i = 0; i < objs.Length; i++)
        {
            timeVisible = objs[i].timeVisible;//!!! Only works correctly for 1 object scenes
            if (objs[i].objVisible)
            {
                HideObj(movingObjs[i]);
            }
        }
    }

    public void ReadCustomPositions(string filename)
    {
        int lineNum = 0;
        string line;
        int n;
        String[] posStrings;

        System.IO.StreamReader file = new System.IO.StreamReader(filename);
        while ((line = file.ReadLine()) != null)
        {
            posStrings = line.Split(' '); // Split by spaces.
            n = posStrings.Length;
            Double[] position = new Double[n];

            // Population positions array.
            for (int i = 0; i < n; i++)
            {
                // Convert string decimal to double and put into an array.
                position[i] = Convert.ToDouble(posStrings[i]); // Assumes you use periods to delineate decimal numbers.
            }

            positions[lineNum] = position;
            lineNum++;
        }

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
            dataManager.Save(true);
        }

    }

    void Update()
    {


    }

}
