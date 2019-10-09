using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManageUI : MonoBehaviour {

    public GameObject canvas;                   // the canvas where the feedback message is displayed
    public UnityEngine.UI.Text feedbackMsg;     // the feedback message that is displayed to the participant at the end of a trial
    public Transform viveCamera;                // the vive camera so the UI canvas can be positioned based on the direction the user is facing


    /**
     * Sets the position of the canvas in the world. ( 0, 6, 100) is the default position.
     */
    public void SetFeedbackPosition(float x, float y, float z)
    {
        canvas.transform.position = new Vector3(x, y, z);

    }

    public void SetFeedbackPosition(float x, float y, float z, bool islocked)
    {
        canvas.transform.position = new Vector3(x, y, z);
        canvas.transform.position = viveCamera.position + (viveCamera.forward * z);
        canvas.transform.position = new Vector3(canvas.transform.position.x, y, canvas.transform.position.z);
        canvas.transform.rotation = Quaternion.LookRotation(canvas.transform.position - viveCamera.position);
    }

    /**
     * Sets the font size of the feedback message.
     */
    public void SetFeedbackSize(int size)
    {
        feedbackMsg.fontSize = size;
    }

    /**
     * Sets the text color of the feedback that is presented to the participant.
     */
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
     * Initializes the feedback message to empty at the beginning of a trial.
     */
     public void ResetFeedbackMsg()
     {
        feedbackMsg.text = "";
     }

    /**
    * Display feedback that shows whether the participant responded too early, too late, or on time.
    */
    public void DisplayPMFeedback(float estimate, float ttcActual)
    {

        if(estimate >= 0)
        {
            double diff = Math.Round((estimate - ttcActual), 2, MidpointRounding.AwayFromZero);

            if (diff == 0.0d) //never evaluates due to floating point precision - Adam hit 0.00 too slow
            {
                feedbackMsg.text = "Perfect timing";
            }
            else if (diff < 0.0d)
            {
                diff = -1 * diff;
                feedbackMsg.text = diff.ToString("F2") + " seconds too fast";
            }
            else
            {
                feedbackMsg.text = diff.ToString("F2") + " seconds too slow";
            }
        }
        else
        {
            feedbackMsg.text = "Wait for object to disappear";
        }
      
    }

    public void DisplayLRFeedback(string pressButton, int corrAns)
    {
        if(corrAns == 1) //Left button
        {
            if (pressButton == "Left") { feedbackMsg.text = "Correct"; } else { feedbackMsg.text = "Incorrect"; }
        }
        else if(corrAns == 2) //Right button
        {
            if (pressButton == "Right") { feedbackMsg.text = "Correct"; } else { feedbackMsg.text = "Incorrect"; }
        }
        else //corrAns was 0
        {
            feedbackMsg.text = "";
        }
    }

    /**
     * Shows whatever message is passed in as an argument to the function.
     */
    public void ShowMessage(string msg)
    {
        feedbackMsg.text = msg;
    }

    /**
     * Clears the canvas so no text is visible.
     */
    public void ClearMessage()
    {
        feedbackMsg.text = "";
    }
}
