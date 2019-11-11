using UnityEngine;
using Valve.VR;
using System.Threading;
using System;

public class TrackHead : MonoBehaviour
{
    private RunExperiment script;
    private GameObject movingObj;
    private CVRSystem vrSystem;

    public SaveData dataManager;
    Thread _poseThread;
    private object thisLock = new object();
    public bool threadStarted;
    public bool workFinished;


    void Start()
    {
        var error = EVRInitError.None;
        //vrSystem = OpenVR.Init(ref error, EVRApplicationType.VRApplication_Background);
        vrSystem = OpenVR.Init(ref error, EVRApplicationType.VRApplication_Scene);

        movingObj = GameObject.Find("MovingObj");
        script = movingObj.GetComponent<RunExperiment>();
        

    }

    private void PrintOpenVRDevices()
    {
        for (uint i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++)
        {
            var deviceClass = vrSystem.GetTrackedDeviceClass(i);
            if (deviceClass != ETrackedDeviceClass.Invalid)
            {
               Debug.Log("OpenVR device at " + i + ": " + deviceClass);
            }
        }
    }
    void Update() 
    {
        /*
        if (!script.isRunning)
        {
            if (threadStarted)
            {
                workFinished = true;
                threadStarted = false;
                _poseThread.Join();
                Debug.Log("******Thread stopped");

            }

        }
        else
        {
            if (!threadStarted)
            {
                _poseThread = new Thread(_getPose);
                //PrintOpenVRDevices();
                workFinished = false;
                threadStarted = true;
                _poseThread.Start();
                Debug.Log("******Thread started");

            }
            else
            {
                Thread.Sleep(4000);
            }

        }
        */

    }
    public void callBack(bool on)
    {
        while (on)
        {
           if (!script.isRunning)
            {
                if (threadStarted)
                {
                    workFinished = true;
                    threadStarted = false;
                    _poseThread.Join();
                    Debug.Log("******Thread stopped");

                }

            }
            else
            {
                if (!threadStarted)
                {
                    _poseThread = new Thread(_getPose);
                    //PrintOpenVRDevices();
                    workFinished = false;
                    threadStarted = true;
                    _poseThread.Start();
                    Debug.Log("******Thread started");

                }
                else
                {
                    //Thread.Sleep(4000);
                }

            }
        }
       


    }

    void _getPose()
    {
        lock (thisLock)
        {
            while (!workFinished)
            {
                
                TrackedDevicePose_t[]
                allPoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
                vrSystem.GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin.TrackingUniverseStanding, 0, allPoses);
                var pose = allPoses[0];
                if (pose.bPoseIsValid)
                {
                    var absTracking = pose.mDeviceToAbsoluteTracking;
                    var mat = new SteamVR_Utils.RigidTransform(absTracking);
                    Debug.Log("Position: " + mat.pos + " Rotation: " + mat.rot.eulerAngles);
                    dataManager.AddHeadPos(0.0f, mat.pos, mat.rot.eulerAngles);
                }

                
            }
            if (workFinished)
            {
                dataManager.WriteHeadPosData();
            }
        }
    }
    void OnApplicationQuit()
    {
        if (_poseThread != null && !_poseThread.IsAlive)
        {
            threadStarted = false;
            workFinished = true;
            _poseThread.Join();
            _poseThread = null;
        }

    }

}