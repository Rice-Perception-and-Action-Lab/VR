using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManageUI : MonoBehaviour {

    public GameObject canvas;                   // the canvas where the feedback message is displayed
    public UnityEngine.UI.Text feedbackMsg;     // the feedback message that is displayed to the participant at the end of a trial

    /**
     * Sets the position of the canvas in the world. ( 0, 6, 100) is the default position.
     */
    public void SetCanvasPosition(float x, float y, float z)
    {
        canvas.transform.position = new Vector3(x, y, z);
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
    public void DisplayFeedback(float respTime, float actualTTC)
    {
        double diff = Math.Round((respTime - actualTTC), 2, MidpointRounding.AwayFromZero);

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


    /**
     * Displays a message telling the participant that the experiment is complete.
     */
     public void DisplayCompletedMsg()
     {
        feedbackMsg.text = "Experiment complete";
     }


    public void ShowMessage(string msg)
    {
        feedbackMsg.text = msg;
    }


    public void ClearMessage()
    {
        feedbackMsg.text = "";
    }
}
