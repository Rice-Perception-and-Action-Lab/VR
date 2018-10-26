using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

// https://docs.unity3d.com/ScriptReference/Time-frameCount.html

public class RunExperiment : MonoBehaviour {

    public SaveData dataManager;        // The GameObject responsible for tracking trial responses
    public Transform viveCamera;        // Position of the target (i.e., the Vive camera rig)

    // The Config options
    private Config config;              // The configuration file specifying certain experiment-wide parameters
    private string inputFile;           // A JSON file holding the information for every trial to be run
    private bool targetCamera;          // A boolean to determine if the object follows the user's head or not

    private Trial[] trials;             // The input file converted to an array of Trial objects 
    private int curTrial;               // Track the number of the current trial being run
    private Vector3 targetPos;           // The target that the moving object aims for
    private float trialStart;           // Track the time that the current trial began
    private bool waiting;               // Boolean to track whether we're waiting between trials
    private float waitTime;             // Timer to track how long we've been waiting
    private string objName;             // The name of the prefab object used for the given trial
    private Vector3 startPos;           // The initial position of the moving object for the current trial
    private Transform obj;              // The prefab object that will be instantiated
    private Transform movingObj;        // The object that will approach the user

    private float ttcTimer;
    private float timer2;
    private float prevTime;
    private bool startTimer;
    private bool printTime;

    private System.Diagnostics.Stopwatch watch;

    public static class WinApi
    {
        /// <summary>TimeBeginPeriod(). See the Windows API documentation for details.</summary>

        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Interoperability", "CA1401:PInvokesShouldNotBeVisible"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage"), SuppressUnmanagedCodeSecurity]
        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod", SetLastError = true)]

        public static extern uint TimeBeginPeriod(uint uMilliseconds);

        /// <summary>TimeEndPeriod(). See the Windows API documentation for details.</summary>

        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Interoperability", "CA1401:PInvokesShouldNotBeVisible"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage"), SuppressUnmanagedCodeSecurity]
        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod", SetLastError = true)]

        public static extern uint TimeEndPeriod(uint uMilliseconds);
    }

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

            watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            // Set the target for this trial (will be updated each iteration of Update function if targetCamera is true)
            targetPos = new Vector3(viveCamera.position.x, viveCamera.position.y, viveCamera.position.z);
      

            // Get the current trial from the data array
            Trial trial = trials[curTrial];

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

            // Set the start time of this trial so that it can be 
            // recorded by the data manager
            trialStart = Time.time;
            curTrial++;

            StartCoroutine(MoveOverTime(endPos, (trial.finalDist / trial.velocity)));
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
            //Debug.Log("Deleting moving object for trial " + curTrial);
            //Destroy(movingObj.gameObject);
            Renderer rend = movingObj.gameObject.GetComponent<Renderer>();
            rend.material.color = Color.black;

            startTimer = true;
            prevTime = Time.time;
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
        //HideObj();
        //Destroy(movingObj.gameObject);

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
        WinApi.TimeBeginPeriod(1);

        // Load the config file
        this.config = LoadConfig("config.json");

        // Set the configurations based on the config file
        inputFile = config.dataFile;
        targetCamera = config.targetCamera;



        // Load the data from the desired input file
        this.trials = LoadTrialData(inputFile, Time.time);

        curTrial = 0;
        waiting = true;
        waitTime = 0.0f;

        ttcTimer = 0.0f;
        startTimer = false;
        prevTime = 0.0f;
        timer2 = 0.0f;
        printTime = true;
    }

    public IEnumerator MoveOverTime(Vector3 finalPos, float seconds)
    {
        float elapsedTime = 0.0f;
        float trialTTC = 0.0f;
        bool objHidden = false;
        Vector3 startingPos = movingObj.position;
        Debug.Log("SECONDS: " + seconds);
        while (elapsedTime < seconds)
        {
            if (!objHidden && elapsedTime >= trials[curTrial - 1].timeVisible)
            {
                Debug.Log("TIME VISIBLE: " + elapsedTime);
                Debug.Log("POSITION: " + movingObj.position.x + " " + movingObj.position.y + " " + movingObj.position.z);
                HideObj();
                objHidden = true;
            }

            movingObj.position = Vector3.Lerp(startingPos, finalPos, (elapsedTime / seconds));
            elapsedTime += Time.deltaTime;
            if (objHidden)
                trialTTC += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        movingObj.position = finalPos;
        Debug.Log("ELAPSED TIME: " + elapsedTime);
        Debug.Log("TTC: " + trialTTC);
    }

    /**
     * Every frame, move the object and check if it should disappear 
     * from view. If the first trial hasn't been initialized yet, then
     * do so.
     */
    void FixedUpdate()
    {
        // Make sure there is currently a trial running before attempting
        // to move an object
        if (movingObj)
        {
            // Update the target position if we're trying to follow the user's head
            if (targetCamera)
            {
                targetPos = viveCamera.position;
            }

            if (startTimer)
            {
                ttcTimer += Time.deltaTime;
                float temp = Time.time;
                timer2 += Time.time - prevTime;
                prevTime = temp;
            }

            Vector3 newPos = movingObj.position;

            // If the object has been visible for the time visible param, hide it
            
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


    /*void FixedUpdate()
    {
        Debug.Log("Frame Count: " + Time.frameCount);

        // Make sure there is currently a trial running before attempting
        // to move an object
        if (movingObj)
        {
            // Update the target position if we're trying to follow the user's head
            if (targetCamera)
            {
                targetPos = viveCamera.position;
            }

            // step size equals current trial's velocity * frame rate
            float step = trials[curTrial - 1].velocity * Time.deltaTime;

            // Move the object 1 step closer to the target
            Vector3 prevPos = movingObj.position;

            //Vector3 adj = new Vector3(0.0f, 0.0f, (movingObj.localScale.z / 2));
            //Vector3 adj = new Vector3(0.0f, (movingObj.localScale.y / 2), 0.0f);
            Vector3 adj = new Vector3(0.0f, 0.0f, (movingObj.localScale.z / 2.0f));
            //Vector3 adj = new Vector3(0.0f, 0.0f, (movingObj.localScale.z));
            //Debug.Log("adj z: " + adj.z);

            //movingObj.position = Vector3.MoveTowards(movingObj.position, targetPos, step);
           movingObj.position = Vector3.MoveTowards(movingObj.position, targetPos - adj, step);

            if (startTimer)
            {
                ttcTimer += Time.deltaTime;
                float temp = Time.time;
                timer2 += Time.time - prevTime;
                prevTime = temp;
            }

            Vector3 newPos = movingObj.position;

            //Debug.Log("Prev Pos: " + prevPos + "  New Pos: " + newPos);

            if (prevPos == newPos && printTime)
            {
                Debug.Log("Contact");
                Debug.Log("TTC TIMER: " + ttcTimer);
                Debug.Log("TIMER 2: " + timer2);
                watch.Stop();
                Debug.Log("STOPWATCH: " + watch.ElapsedMilliseconds);
                startTimer = false;
                printTime = false;
            }

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
        }*/
    //}

}
