using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManageObjs : MonoBehaviour {

    /**
     * This class defines an Obj, which is essentially a single moving object in a trial. It holds all of the parameters
     * that the input file defined for the object as well as several derived fields that track the object's motion
     * through the scene once the trial has started.
     */
    [System.Serializable]
    public class Obj
    {
        /* Fields parsed from input file */
        public int objNum;                  // the object number defined in the input file
        public string objType;              // the name of the prefab that the object should be instantiated as
        public float[] objScale;            // the x,y,z-coordinates for the scale of the object
        public float[] objRot;              // the x,y,z-coordinates for the rotation of the object
        public float[] startPos;            // the x,y,z-coordinates for the initial position of the object
        public float[] endPos;              // the x,y,z-coordinates for the final position of the object
        public float velocity;              // the speed that the object is moving
        public float timeVisible;           // the amount of time that the object is visible before disappearing
        public float rotationSpeedX;        // the speed at which the object should rotate around X axis
        public float rotationSpeedY;        // the speed at which the object should rotate around Y axis
        public float rotationSpeedZ;        // the speed at which the object should rotate around Z axis
        public bool offsetX;                // whether or not an object needs an x coordinate offset
        public bool offsetY;                // whether or not an object needs an y coordinate offset
        public bool offsetZ;                // whether or not an object needs an z coordinate offset
        public float dist;                  // the distance that the object must travel

        /* Derived fields */
        public float step;                  // the distance that an object will travel in a given step based on step size and the object's velocity
        public float stepHidden;            // the step at which an object should become invisible
        public float finalStep;             // the final step before an object has finished moving and should become inactive
        public int stepCounter = 0;         // the number of steps the object has taken
        public bool objVisible = false;     // whether or not an object is currently visible in the scene
        public bool objActive = false;      // whether or not the object is currently active in the scene
    }

}
