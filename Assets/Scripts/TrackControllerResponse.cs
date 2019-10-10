using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackControllerResponse : MonoBehaviour
{

    private float timer;                        // a timer to pause between receiving input and initializing a new trial
    private bool waitingConfidence;             // a variable to determine whether we are waiting for a confidence judgment before stopping the trial
    private bool practiceOver;                  // a varialbe to determine whether practice trials are over
    private SteamVR_TrackedObject controller;   // a reference to the controller being tracked
    private GameObject movingObj;               // a reference to the MovingObj GameObject so we can call methods from the RunExperiment script attached to it
    private RunExperiment script;               // a reference to the RunExperiment script so we can call its methods
                                                // Hair trigger can sometimes be sensitive; set a threshold so that it has to be pushed down slightly further before it registers as a trigger press
    private float threshold = 0.3f;
    private float pressTime;
    private float releaseTime;
    private string pressButton;
    private string confidenceNA = "N/A";

    private bool checkPress;                    // a variable to check if the controller's press has already been recorded.


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

        waitingConfidence = false;
        practiceOver = false;

        checkPress = false;

    }

    // Update is called once per frame
    void Update()
    {
        
        Vector2 touchVector = (Controller.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis0)); // Returns the axis that is being pushed.

        if (waitingConfidence)
        {
            //If statements required because switch needs to be able to figure out the value of all case statements at compile time
            if (Input.GetKeyDown("0"))
            {
                script.uiManager.ClearMessage();
                script.CompleteTrial(pressTime, true, pressButton, "0");
                waitingConfidence = false;
            }
            if (Input.GetKeyDown("1"))
            {
                script.uiManager.ClearMessage();
                script.CompleteTrial(pressTime, true, pressButton, "1");
                waitingConfidence = false;
            }
            if (Input.GetKeyDown("2"))
            {
                script.uiManager.ClearMessage();
                script.CompleteTrial(pressTime, true, pressButton, "2");
                waitingConfidence = false;
            }
            if (Input.GetKeyDown("3"))
            {
                script.uiManager.ClearMessage();
                script.CompleteTrial(pressTime, true, pressButton, "3");
                waitingConfidence = false;
            }
            if (Input.GetKeyDown("4"))
            {
                script.uiManager.ClearMessage();
                script.CompleteTrial(pressTime, true, pressButton, "4");
                waitingConfidence = false;
            }
            if (Input.GetKeyDown("5"))
            {
                script.uiManager.ClearMessage();
                script.CompleteTrial(pressTime, true, pressButton, "5");
                waitingConfidence = false;
            }
            if (Input.GetKeyDown("6"))
            {
                script.uiManager.ClearMessage();
                script.CompleteTrial(pressTime, true, pressButton, "6");
                waitingConfidence = false;
            }
            if (Input.GetKeyDown("7"))
            {
                script.uiManager.ClearMessage();
                script.CompleteTrial(pressTime, true, pressButton, "7");
                waitingConfidence = false;
            }
            if (Input.GetKeyDown("8"))
            {
                script.uiManager.ClearMessage();
                script.CompleteTrial(pressTime, true, pressButton, "8");
                waitingConfidence = false;
            }
            if (Input.GetKeyDown("9"))
            {
                script.uiManager.ClearMessage();
                script.CompleteTrial(pressTime, true, pressButton, "9");
                waitingConfidence = false;
            }

        }

        // Start the next trial if there isn't a currently running trial and the hair trigger button is pressed
        if (!waitingConfidence && !script.isRunning && Controller.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger).x > threshold)
        {
            script.InitializeTrial();
        }

        // Option for pressing button down.
        if (script.config.pressHold)
        {
            // Record when button first pressed down. (Last inequality is to make sure trigger button is not pressed).
            if (Controller.GetState().ulButtonPressed > 0 && !checkPress && Controller.GetState().ulButtonPressed < .5) ;
            {
                pressTime = Time.time;
                checkPress = true;
                if (script.config.debugging) { Debug.Log("Time is: " + pressTime); }
            }

            // Record when button is released.
            if (Controller.GetState().ulButtonPressed == 0 && checkPress)
            {
                releaseTime = Time.time; // Record when button is released.
                checkPress = false;
                script.isRunning = false;

                if (script.config.debugging) { Debug.Log("Time is: " + releaseTime); }
            }
            // Add keyboard press for experimenter to end the trial. (Make sure not to click into console.), pass release time in.
        }



        // End the trial if there is a trial running and the touchpad button is pressed
            if (script.isRunning && Controller.GetPress(SteamVR_Controller.ButtonMask.Touchpad))
        {

            if (touchVector.x < -0.5f)
            {
                pressTime = Time.time;
                pressButton = "Left";
                script.isRunning = false;

                if (script.config.debugging) { Debug.Log("Button pressed: " + pressButton); }
                if (script.config.debugging) { Debug.Log("Time at press: " + pressTime); }


                if (!script.config.collectConfidence) { script.CompleteTrial(pressTime, true, pressButton, confidenceNA); }
                if (script.config.collectConfidence)
                {
                    waitingConfidence = true;
                    script.HideAllObjs();
                    if (script.config.debugging) { Debug.Log("Waiting for confidence rating..."); }
                    script.uiManager.ShowMessage("How confident?");
                }


             }

            if (touchVector.x > 0.5f)
            {
                pressTime = Time.time;
                pressButton = "Right";
                script.isRunning = false;

                if (script.config.debugging) { Debug.Log("Button pressed: " + pressButton); }
                if (script.config.debugging) { Debug.Log("Time at press: " + pressTime); }

                if (!script.config.collectConfidence) { script.CompleteTrial(pressTime, true, pressButton, confidenceNA); }
                if (script.config.collectConfidence)
                {
                    waitingConfidence = true;
                    script.HideAllObjs();
                    if (script.config.debugging) { Debug.Log("Waiting for confidence rating..."); }
                    script.uiManager.ShowMessage("How confident?");
                }

            }
            
            // if (touchVector.y > 0.7f) UP
            // if (touchVector.y < -0.7) DOWN

        }

    }
}
