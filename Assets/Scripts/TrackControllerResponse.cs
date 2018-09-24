using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackControllerResponse : MonoBehaviour {

    private float timer;                        // a timer to pause between receiving input and initializing a new trial
    private bool waiting;                       // true if we're waiting to initialize a new trial; false otherwise
    private SteamVR_TrackedObject controller;   // a reference to the controller being tracked
    private GameObject movingObj;               // a reference to the MovingObj GameObject so we can call methods from the MoveObj script attached to it
    private MoveObj script;                     // a reference to the MoveObj script so we can call its methods

    private SteamVR_Controller.Device Controller
    {
        // Finds the index of the controller from all tracked objects; allows us to easily track controller input
        get { return SteamVR_Controller.Input((int) controller.index); }
    }

    void Awake()
    {
        // Get a reference to the TrackedObject component attached to the controller
        controller = GetComponent<SteamVR_TrackedObject>();

        // Find the MovingObj GameObject so we can call methods from the MoveObj script attached to it
        movingObj = GameObject.Find("MovingObj");
        script = movingObj.GetComponent<MoveObj>();
    }
	
	// Update is called once per frame
	void Update () {

        if (!waiting)
        {
            if (Controller.GetPress(SteamVR_Controller.ButtonMask.Touchpad))
            {
                Debug.Log("Controller input received; launch next trial");

                // End the current trial
                script.CompleteTrial(Time.time);

                // Wait for 3 seconds before initializing the next trial
                waiting = true;
                timer = 0.0f;
            }
        }
        else
        {
            // Keep waiting until 3 seconds have passed
            if (timer < 3.0f)
            {
                
                timer += Time.deltaTime;
                //Debug.Log("Waiting... " + timer);
            }
            else
            {
                waiting = false;
                Debug.Log("Preparing to initialize next trial");
                script.InitializeTrial();
            }
        }

    }
}
