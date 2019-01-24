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
        public int subjNum;
        public int subjSex;
        public string dataFile;
        public bool showFeedback;
        public string feedbackColor;
        public bool targetCamera;
        public bool trackHeadPos;
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
        public float startDist;         // the starting distance of the object (in meters)
        public float velocity;          // the speed the object is moving (in meters / second)
        public float timeVisible;       // the amount of time this object should be visible before disappearing
        public string objType;          // the names of the potential object prefabs that can be instantiated
        public float rotationSpeed;     // the speed at which the object should rotate each frame
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
        if (curTrial < this.trials.Length)
        {
            Debug.Log("Initializing trial " + curTrial);

            feedbackMsg.text = "";

            // Set the target for this trial (will be updated each iteration of Update function if targetCamera is true)
            targetPos = new Vector3(viveCamera.position.x, viveCamera.position.y, viveCamera.position.z);


            Debug.Log("TARGET POSITION: " + viveCamera.position.x + " " + viveCamera.position.y + " " + viveCamera.position.z);

            // Get the current trial from the data array
            Trial trial = trials[curTrial];

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

            // Instantiate the object so that it's visible
            movingObj = Instantiate(obj, startPos, Quaternion.identity);

            isRunning = true;

            // Set the start time of this trial so that it can be recorded by the data manager
            trialStart = Time.time;
            curTrial++;

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
                feedbackMsg.text = "Experiment complete";
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
     * Display feedback that shows whether the participant responded too early, too late, or on time.
     */
    public void DisplayFeedback(float respTime, float actualTTC)
    {
        float diff = (respTime - actualTTC);
        if (diff == 0) //never evaluates due to floating point precision - Adam hit 0.00 too slow
        {
            feedbackMsg.text = "Perfect timing";
        }
        else if (diff < 0)
        {
            diff = -1 * diff;
            feedbackMsg.text = diff.ToString("F2") + " seconds too fast";
        }
        else
        {
           feedbackMsg.text = diff.ToString("F2") + " seconds too slow";
        }
    }


    /**
     * End the given trial.
     */
    public void CompleteTrial(float trialEnd, bool receivedResponse)
    {
        CancelInvoke("HeadTracking");
        // Delete the existing object in the trial if a button was pressed
        // before the object was hidden from view
        HideObj();                          // remove for testing
        //StopCoroutine(movementCoroutine);
        //Destroy(movingObj.gameObject);      // remove for testing

        float respTime = (trialEnd - trialStart);
        float actualTTC = (trials[curTrial - 1].startDist / trials[curTrial - 1].velocity);

        // Display response time feedback to the participant
        if (config.showFeedback) DisplayFeedback(respTime, actualTTC);

        isRunning = false;

        // Add this trial's data to the data manager
        dataManager.AddTrial(curTrial, trials[curTrial - 1].startDist, startPos, trials[curTrial - 1].velocity, trials[curTrial - 1].rotationSpeed,
            trials[curTrial - 1].timeVisible, objName, trialStart, trialEnd, receivedResponse, respTime);

        // Wait to start the next trial
        waiting = true;
        waitTime = 0.0f;

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

    public void SetFeedbackColor(string color)
    {
        // All named color values supported by Unity
        Dictionary<string, Color> colorDict = new Dictionary<string, Color>
        {
            {"black", Color.black},
            {"blue", Color.blue},
            {"clear", Color.clear},
            {"cyan", Color.cyan},
            {"gray", Color.gray},
            {"green", Color.green},
            {"grey", Color.grey},
            {"magenta", Color.magenta},
            {"red", Color.red},
            {"white", Color.white},
            {"yellow", Color.yellow}
        };

        if (!colorDict.ContainsKey(color))
        {
            feedbackMsg.color = Color.black;    // default to black if color doesn't exist
        }
        else
        {
            feedbackMsg.color = colorDict[color];
        }
    }

    /**
     * Initializes all trial data once the experiment begins. Starts
     * tracking the time that events are occurring.
     */
    void Start()
    {
        // Set the target framerate for the application
        Application.targetFrameRate = 75;

        // Load the config file
        this.config = LoadConfig("config.json");

        // Set the configurations based on the config file
        inputFile = config.dataFile;
        targetCamera = config.targetCamera;
        SetFeedbackColor(config.feedbackColor);

        // Add the config info to the data manager
        dataManager.SetConfigInfo(config.subjNum, config.subjSex, config.dataFile, config.showFeedback, config.feedbackColor, config.targetCamera, config.trackHeadPos);

        // Load the data from the desired input file
        this.trials = LoadTrialData(inputFile, Time.time);

        // Initialize global variables
        curTrial = 0;
        waiting = true;
        waitTime = 0.0f;
        isRunning = false;

        // Set the head position transform to track the participant's movements
        headPos = GameObject.Find("Camera (eye)").transform;

        //UnityEngine.VR.InputTracking
    }

    int stepCounter = 0;
    string posString = "";
    float hideTime = 0.0f;
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
