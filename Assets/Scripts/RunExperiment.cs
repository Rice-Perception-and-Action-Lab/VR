using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class RunExperiment : MonoBehaviour {

    // Set via the Unity editor
    public SaveData dataManager;            // The GameObject responsible for tracking trial responses
    public UnityEngine.UI.Text feedbackMsg; // The text displayed on the scene's canvas when the participant responds with their TTC guess
    public Transform viveCamera;            // Position of the target (i.e., the Vive camera rig)

    // The Config options
    private Config config;                  // The configuration file specifying certain experiment-wide parameters
    private string inputFile;               // A JSON file holding the information for every trial to be run
    private bool targetCamera;              // A boolean to determine if the object follows the user's head or not

    // Experiment-Dependent Variables
    private Trial[] trials;                 // The input file converted to an array of Trial objects 
    private IEnumerator movementCoroutine;  // The coroutine responsible for moving the object in the world
    private Transform headPos;              // The location of the camera rig relevant to the scene

    // Trial-Dependent Variables
    private int trialNum;                   // Track the number of the current trial being run
    private float trialStart;               // The time at which the trial was initialized
    private Transform[] movingObjs;         // The objects that need to be manipulated during the current trial
    private Vector3[] targetPos;            // The target positions for each object (targetPos[i] == target position for movingObjs[i])
    private int stepCounter;                // The number of "steps" (i.e., frames) that the object has moved
    private float stepSize;                 // The distance the object should move in a single frame

    /*
    private Vector3 targetPos;              // The target that the moving object aims for
    private Transform obj;                  // The prefab object that will be instantiated
    private Transform movingObj;            // The object that will approach the user
    */

    [System.Serializable]
    public class Config
    {
        public string dataFile;
        public bool targetCamera;
        public float fps;
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
        public int trialNum;            // the trial number
        public MovingObj[] objects;     // the moving objects to be presented during the trial
    }

    [System.Serializable]
    public class MovingObj
    {
        public float startXCoord;       // the initial x-coordinate of the object
        public float startYCoord;       // the initial y-coordinate of the object
        public float startZCoord;       // the initial z-coordinate of the object
        public float velocity;          // the speed (m/s) at which the object will travel
        public float timeVisible;       // the time (in seconds) for which the object will remain visible
        public string objectPrefab;     // the name of the prefab object to be instantiated
        public float rotationSpeed;     // the rate (in m/s) at which the object will rotate
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
            Debug.Log("Exception: " + e.Message);
            return null;
        }
    }

    /**
     * Initialize the given trial.
     */
    public void InitializeTrial()
    {
        // Check that we still have more trials to run
        if (trialNum < this.trials.Length)
        {
            Debug.Log("Initializing trial " + trialNum);

            feedbackMsg.text = "";

            // Set the target for this trial (will be updated each iteration of Update function if targetCamera is true)
            //targetPos = new Vector3(viveCamera.position.x, viveCamera.position.y, viveCamera.position.z);
      
            // Get the current trial from the data array
            Trial trial = trials[trialNum];

            // Initialize the array of transforms for the objects
            movingObjs = new Transform[trial.objects.Length];

            // Initialize the array of target positions for the objects
            targetPos = new Vector3[trial.objects.Length];

            // Set the initial target positions for the objects
            for (int i = 0; i < targetPos.Length; i++)
            {
                targetPos[i] = viveCamera.position;
            }

            // Instantiate each of the objects
            for (int i = 0; i < trial.objects.Length; i++)
            {
                // Represent the start position as a Vector3 
                Vector3 startPos = new Vector3(trial.objects[i].startXCoord, trial.objects[i].startYCoord, trial.objects[i].startZCoord);

                // Create the object
                GameObject obj = Resources.Load("Objects\\" + trial.objects[i].objectPrefab) as GameObject;     // find the correct prefab
                movingObjs[i] = Instantiate(obj.transform, startPos, Quaternion.identity);                      // make the new object visible
            }



            // Set the start time of this trial so that it can be recorded by the data manager
            trialStart = Time.time;

            // Increment the trial number so the same trial isn't instantiated again
            trialNum++;

            // Initialize all framerate-based variables for the trial
            float stepSize = (1.0f / config.fps);
            stepCounter = 0;

            // Schedule the object to be moved at the correct framerate
            InvokeRepeating("MoveObjs", 0f, stepSize);
        }
        else
        {
            // Do this check and then increment the trial num so data is only saved once
            if (trialNum == trials.Length)
            {
                Debug.Log("Experiment complete");
                feedbackMsg.text = "";
                dataManager.Save();
                trialNum++;
            }
        }
    }

    /*public void HideObj()
    {
        // Check that a moving object has been initialized and that it's actually
        // a game object to avoid errors
        if (movingObj && movingObj.gameObject)
        {
            Debug.Log("Deleting moving object for trial " + trialNum);          // remove for testing
            Debug.Log("POSITION: " + movingObj.position.x + " " + movingObj.position.y + " " + movingObj.position.z);
            Renderer rend = movingObj.gameObject.GetComponent<Renderer>();
            //rend.enabled = false;

            rend.material.color = Color.black;                                // add for testing
        }
        else
        {
            Debug.Log("ERROR: Could not delete moving object; object did not exist");
        }
    }*/

    /**
     * Display feedback that shows whether the participant responded too early, too late, or on time.
     */
    /*public void DisplayFeedback(float respTime, float actualTTC)
    {
        float diff = (respTime - actualTTC);
        if (diff == 0)
        {
            feedbackMsg.text = "On time";
        }
        else if (diff < 0)
        {
            diff = -1 * diff;
            feedbackMsg.text = diff.ToString() + " seconds too fast";
        }
        else
        {
            feedbackMsg.text = diff.ToString() + " seconds too slow";
        }
    }*


    /**
     * End the given trial.
     */
    /*public void CompleteTrial(float trialEnd, bool receivedResponse)
    {
        // Delete the existing object in the trial if a button was pressed
        // before the object was hidden from view
        HideObj();                          // remove for testing
        //StopCoroutine(movementCoroutine);
        //Destroy(movingObj.gameObject);      // remove for testing

        float respTime = (trialEnd - trialStart);
        float actualTTC = (trials[trialNum - 1].finalDist / trials[trialNum - 1].velocity);
        DisplayFeedback(respTime, actualTTC);

        // Add this trial's data to the data manager
        dataManager.AddTrial(trialNum, trials[trialNum - 1].finalDist, startPos, trials[trialNum - 1].velocity,
            trials[trialNum - 1].timeVisible, objName, trialStart, trialEnd, receivedResponse);

        // Increment trial number so we don't run this trial again
        //trialNum++;
        //InitializeTrial();

        //HeadPos[] posArr = headTracking.ToArray();
        //string jsonPos = JsonHelper.ToJson(posArr, true);
        //Debug.Log(jsonPos);
        dataManager.WritePosData();
    }*/

    /**
     * Initializes all trial data once the experiment begins. Starts
     * tracking the time that events are occurring.
     */
    void Start()
    {
        // Set the target framerate for the application
        Application.targetFrameRate = 60;

        // Load the config file
        this.config = LoadConfig("config.json");

        // Set the configurations based on the config file
        inputFile = config.dataFile;
        targetCamera = config.targetCamera;

        // Load the data from the desired input file
        this.trials = LoadTrialData(inputFile, Time.time);

        // Initialize global variables
        trialNum = 0;

        // Set the head position transform to track the participant's movements
        headPos = GameObject.Find("Camera (eye)").transform;

        InitializeTrial();
    }


    void MoveObjs()
    {
        int maxFinalStep = 1;

        // Iterate through each object to update its position
        for (int i = 0; i < movingObjs.Length; i++)
        {
            MovingObj obj = trials[trialNum - 1].objects[i];
            // Perform object-specific step calculations
            int stepHidden = obj.timeVisible * config.fps;
            float finalStep = (obj.startZCoord / obj.velocity) * 60;

            if (finalStep > maxFinalStep)
            {
                maxFinalStep = finalStep;
            }
            
            if (stepCounter == stepHidden)
            {
                Debug.Log("hide");
            }
            else
            {
                float step = obj.velocity * stepSize;
                movingObjs[i].position -= new Vector3(0.0f, 0.0f, step);
            }
        }
        stepCounter++;

        if (stepCounter > maxFinalStep)
        {
            CancelInvoke();

            // TODO: Add the rest of the canceling logic
        }
    }


    /*int stepCounter = 0;
    string posString = "";
    float hideTime = 0.0f;
    void MoveObjByStep()
    {
        float stepHidden = (trials[trialNum - 1].timeVisible * 60);
        float finalStep = ((trials[trialNum - 1].finalDist / trials[trialNum - 1].velocity) * 60);
        float stepSize = (1.0f / 60.0f);        

        if (stepCounter == stepHidden)
        {
            HideObj();
            posString = movingObj.position.x + " " + movingObj.position.y + " " + movingObj.position.z;
            hideTime = (Time.time - trialStart);
            Debug.Log("Time Visible: " + hideTime + " | " + posString);
            stepCounter++;
        }
        else
        {
            float step = trials[trialNum - 1].velocity * stepSize;
            movingObj.position -= new Vector3(0.0f, 0.0f, step);
            stepCounter++;

            if (stepCounter > finalStep)
            {
                CancelInvoke();
                float endTime = (Time.time - trialStart);
                Debug.Log("TTC: " + (endTime - hideTime) + "  |  POSITION: " + posString);
                stepCounter = 0;
                posString = "";
                hideTime = 0.0f;
            }
        }


    }*/

}
