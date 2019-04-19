using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManageObjs : MonoBehaviour {

    [System.Serializable]
    public class Obj
    {
        public int objNum;
        public string objType;          // the name of the prefab that the object should be instantiated as
        public float[] objScale;        // the x,y,z-coordinates for the scale of the object
        public float[] startPos;        // the x,y,z-coordinates for the initial position of the object
        public float[] endPos;          // the x,y,z-coordinates for the final position of the object
        public float velocity;          // the speed that the object is moving
        public float timeVisible;       // the amount of time that the object is visible before disappearing
        public float rotationSpeed;     // the speed at which the object should rotate
        public float dist;              // the distance that the object must travel

        public float step;
        public float stepHidden;
        public float finalStep;
        public int stepCounter = 0;
        public bool objVisible = false;
        public bool objActive = false;
    }

}
