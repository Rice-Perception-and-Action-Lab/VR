using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class TrackControllerResponse : MonoBehaviour
{

    private float timer;                        // a timer to pause between receiving input and initializing a new trial
    private bool waiting;                       // true if we're waiting to initialize a new trial; false otherwise
    private SteamVR_TrackedObject controller;   // a reference to the controller being tracked
    private GameObject movingObj;               // a reference to the MovingObj GameObject so we can call methods from the RunExperiment script attached to it
    private RunExperiment script;               // a reference to the RunExperiment script so we can call its methods

    private SteamVR_Controller.Device Controller
    {
        // Finds the index of the controller from all tracked objects; allows us to easily track controller input
        get { return SteamVR_Controller.Input((int)controller.index); }
    }

    void Awake()
    {
        // Get a reference to the TrackedObject component attached to the controller
        controller = GetComponent<SteamVR_TrackedObject>();

        // Find the MovingObj GameObject so we can call methods from the MoveObj script attached to it
        movingObj = GameObject.Find("MovingObj");
        script = movingObj.GetComponent<RunExperiment>();

        waiting = false;

    }

    // Update is called once per frame
    void Update()
    {
        // Hair trigger can sometimes be sensitive; set a threshold so that it has to be pushed down slightly further before it registers as a trigger press
        float threshold = 0.3f;
        bool runningTrial = script.CheckTrialRunning();

        // End the trial if there is a trial running and the touchpad button is pressed
        if (runningTrial && Controller.GetPress(SteamVR_Controller.ButtonMask.Touchpad))
        {
            script.CompleteTrial(Time.time, true);
        }
        else
        {
            // Start the next trial if there isn't a currently running trial and the hair trigger button is pressed
            if (!runningTrial && Controller.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger).x > threshold)
            {
                script.InitializeTrial();
            }
        }
    }
}
