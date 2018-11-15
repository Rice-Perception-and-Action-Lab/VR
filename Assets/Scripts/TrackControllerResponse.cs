using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackControllerResponse : MonoBehaviour {

    /*private float timer;                        // a timer to pause between receiving input and initializing a new trial
    private bool waiting;                       // true if we're waiting to initialize a new trial; false otherwise
    private SteamVR_TrackedObject controller;   // a reference to the controller being tracked
    private GameObject movingObj;               // a reference to the MovingObj GameObject so we can call methods from the RunExperiment script attached to it
    private RunExperiment script;               // a reference to the RunExperiment script so we can call its methods



    private float respTime;*/

    private SteamVR_TrackedObject controller;   // a reference to the controller being tracked

    private SteamVR_Controller.Device Controller
    {
        // Finds the index of the controller from all tracked objects; allows us to easily track controller input
        get { return SteamVR_Controller.Input((int) controller.index); }
    }

    void Awake()
    {
        // Get a reference to the TrackedObject component attached to the controller
        controller = GetComponent<SteamVR_TrackedObject>();

        /*
        // Find the MovingObj GameObject so we can call methods from the MoveObj script attached to it
        movingObj = GameObject.Find("MovingObj");
        script = movingObj.GetComponent<RunExperiment>();

        waiting = false;*/
    }
	
	// Update is called once per frame
	void Update () {

        /*bool runningTrial = script.CheckTrialRunning();

        if (runningTrial && Controller.GetPress(SteamVR_Controller.ButtonMask.Touchpad))
        {
            Debug.Log("Touchpad button pressed; end current trial");

            // End the current trial
            script.CompleteTrial(Time.time, true);
            respTime = Time.time;
        } else
        {
            if (!runningTrial && Controller.GetHairTriggerDown())
            {
                Debug.Log("Hair trigger pressed; launch next trial");
                script.InitializeTrial();
            }
        }*/
    }
}
