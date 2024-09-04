using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MixedReality.Toolkit.Subsystems;
using MixedReality.Toolkit;
using MixedReality.Toolkit.Input;
using UnityEngine.XR;
using UnityEngine.XR.Hands;

public class GesturesMRTK_BETA : MonoBehaviour
{
    public GameObject menu;
    public float curlThreshold = 100f; // Adjust as needed
    public float fistThreshold = 105f; // Adjust as needed 
    public float victoryThreshold = 120f; // Threshold for Victory gesture
    public float palmUpThreshold = 20f; // Threshold for Palm Up gesture
    public float shakaThreshold = 80f; // Threshold for Shaka gesture
    public float rockThreshold = 60f; // Threshold for Rock gesture

    void Start()
    {
        StartCoroutine("EnableWhenSubsystemAvailable");
    }

    void Update()
    {
        StartCoroutine("Gesture");
    }

    IEnumerator Gesture()
    {
        /***************************************************************************************************
            *                                     Check for Poses                                          *
            *                  1 - Thumb Up | 2 - Fist  | 3 - Victory | 4 - Thumbs Down                    *
            *                  5 - Palm Up  | 6 - Shaka | 7 - Rock    | 8 - No Pose (default)              *
            ************************************************************************************************
            *   Hand recognition logic: 1. Check if any of hands is spotted.                               *
            *                           2. Check the spotted hand (left is prioritized)                    *
            *                              - If gesture recognized, breaks and returns int in range [1-8]  *
            ************************************************************************************************/
        var aggregator = XRSubsystemHelpers.GetFirstRunningSubsystem<HandsAggregatorSubsystem>();
        bool leftJointsAreValid = aggregator.TryGetEntireHand(XRNode.LeftHand, out IReadOnlyList<HandJointPose> leftJoints);
        bool rightJointsAreValid = aggregator.TryGetEntireHand(XRNode.RightHand, out IReadOnlyList<HandJointPose> rightJoints);

        int pose = 8; // Default pose - No Pose
        
        // Check for gestures on both hands. Left is prioritized 
        if (leftJointsAreValid)
        {
            pose = CheckForGesture(aggregator, XRNode.LeftHand);
        }
        else if (rightJointsAreValid)  
        {
            pose = CheckForGesture(aggregator, XRNode.RightHand);
        }

        // Detected gestures for some hand
        if (pose == 1)
        {
            // Open Menu (Thumbs Up)
            if(menu.activeSelf == false)
            {
                Debug.Log("Thumbs Up - Detected");
                menu.SetActive(true);
                yield return new WaitForSeconds(9);
            }     
        }
        else if (pose == 2)
        {
            // Hide Menu (Fist)
            if(menu.activeSelf != false)
            {
                Debug.Log("Fist - Detected");
                menu.SetActive(false); 
                yield return new WaitForSeconds(9);
            }
        }
        else if (pose == 3)
        {
            // Victory  
            Debug.Log("Victory - Detected");
             
        }
        else if (pose == 4)
        {
            // Thumbs Down Gesture  
            Debug.Log("Thumbs Down - Detected");
             
        }
        else if (pose == 5)
        {
            // Palm Up    
            Debug.Log("Palm Up - Detected");
             
        }
        else if (pose == 6)
        {
            // Shaka  
            Debug.Log("Shaka - Detected");
             
        }
        else if (pose == 7)
        {
            // Rock 
            Debug.Log("Rock - Detected");
             
        }

        yield return 0;
        
    }

    private int CheckForGesture(HandsAggregatorSubsystem aggregator, XRNode hand)
    {
        Quaternion indexDistal, thumbDistal, middleDistal, ringDistal, pinkyDistal;
        bool indexDistalIsValid = aggregator.TryGetJoint(TrackedHandJoint.IndexDistal, hand, out HandJointPose indexDistalPose);
        bool thumbDistalIsValid = aggregator.TryGetJoint(TrackedHandJoint.ThumbDistal, hand, out HandJointPose thumbDistalPose);
        bool middleDistalIsValid = aggregator.TryGetJoint(TrackedHandJoint.MiddleDistal, hand, out HandJointPose middleDistalPose);
        bool ringDistalIsValid = aggregator.TryGetJoint(TrackedHandJoint.RingDistal, hand, out HandJointPose ringDistalPose);
        bool pinkyDistalIsValid = aggregator.TryGetJoint(TrackedHandJoint.LittleDistal, hand, out HandJointPose pinkyDistalPose);

        // Position of hand fingers in the scene for given hand
        indexDistal = indexDistalPose.Rotation;
        thumbDistal = thumbDistalPose.Rotation;
        middleDistal = middleDistalPose.Rotation;
        ringDistal = ringDistalPose.Rotation;
        pinkyDistal = pinkyDistalPose.Rotation;

        float thumbIndexAngle = Quaternion.Angle(thumbDistal, indexDistal);
        float thumbMiddleAngle = Quaternion.Angle(thumbDistal, middleDistal);
        float thumbRingAngle = Quaternion.Angle(thumbDistal, ringDistal);
        float thumbPinkyAngle = Quaternion.Angle(thumbDistal, pinkyDistal);

        // Check for specific gestures
        // TODO: see if can imrpove further reduction of semi-redundant angle computations
        /*1 - Thumbs Up*/
        if (thumbIndexAngle < curlThreshold &&
            thumbMiddleAngle < curlThreshold &&
            thumbRingAngle < curlThreshold &&
            thumbPinkyAngle < curlThreshold)
        {
            return 1;  
        }
        /*2 - Fist*/
        else if (CheckForFistGesture(aggregator, hand))
        {
            return 2;  
        }
        /*3 - Victory*/
        else if (CheckForVictoryGesture(thumbDistal, indexDistal, middleDistal, ringDistal, pinkyDistal))
        {
            return 3;  
        }
        /*4 - Thumbs Down*/
        else if (CheckForThumbsDownGesture(thumbDistal, indexDistal, middleDistal, ringDistal, pinkyDistal))
        {
            return 4;   
        }
        /*5 - Palm Up*/
        else if (CheckForPalmUpGesture(aggregator, hand))
        {
            return 5;  
        }
        /*6 - Shaka*/
        else if (CheckForShakaGesture(thumbDistal, indexDistal, middleDistal, ringDistal, pinkyDistal))
        {
            return 6;  
        }
        /*7 - Rock*/
        else if (CheckForRockGesture(thumbDistal, indexDistal, pinkyDistal))
        {
            return 7;  
        }
        /*8 - No Pose(default)*/
        return 8;     
    }

    private bool CheckForFistGesture(HandsAggregatorSubsystem aggregator, XRNode hand)
    {
        bool indexProximalIsValid = aggregator.TryGetJoint(TrackedHandJoint.IndexProximal, hand, out HandJointPose indexProximalPose);
        bool thumbProximalIsValid = aggregator.TryGetJoint(TrackedHandJoint.ThumbProximal, hand, out HandJointPose thumbProximalPose);
        bool middleProximalIsValid = aggregator.TryGetJoint(TrackedHandJoint.MiddleProximal, hand, out HandJointPose middleProximalPose);
        bool ringProximalIsValid = aggregator.TryGetJoint(TrackedHandJoint.RingProximal, hand, out HandJointPose ringProximalPose);

        float thumbbIndexAngle = CalculateJointAngle(thumbProximalPose.Position, thumbProximalPose.Position, indexProximalPose.Position);
        float indexMiddleAngle = CalculateJointAngle(indexProximalPose.Position, indexProximalPose.Position, middleProximalPose.Position);
        float middleRingAngle = CalculateJointAngle(middleProximalPose.Position, middleProximalPose.Position, ringProximalPose.Position);
        float ringPinkyAngle = CalculateJointAngle(ringProximalPose.Position, ringProximalPose.Position, ringProximalPose.Position);

        return (thumbbIndexAngle < fistThreshold &&
                indexMiddleAngle < fistThreshold &&
                middleRingAngle < fistThreshold &&
                ringPinkyAngle < fistThreshold);
    }

    private bool CheckForVictoryGesture(Quaternion thumbDistal, Quaternion indexDistal, Quaternion middleDistal, Quaternion ringDistal, Quaternion pinkyDistal)
    {
        float thumbIndexAngle = Quaternion.Angle(thumbDistal, indexDistal);
        float indexMiddleAngle = Quaternion.Angle(indexDistal, middleDistal);
        float middleRingAngle = Quaternion.Angle(middleDistal, ringDistal);

        float thumbRingAngle = Quaternion.Angle(thumbDistal, ringDistal);
        return (thumbRingAngle < victoryThreshold && 
                indexMiddleAngle < curlThreshold &&
                middleRingAngle < curlThreshold &&
                pinkyDistal.Equals(Quaternion.identity));
    }

    private bool CheckForThumbsDownGesture(Quaternion thumbDistal, Quaternion indexDistal, Quaternion middleDistal, Quaternion ringDistal, Quaternion pinkyDistal)
    {
        float thumbIndexAngle = Quaternion.Angle(thumbDistal, indexDistal);
        float thumbMiddleAngle = Quaternion.Angle(thumbDistal, middleDistal);
        float thumbRingAngle = Quaternion.Angle(thumbDistal, ringDistal);
        float thumbPinkyAngle = Quaternion.Angle(thumbDistal, pinkyDistal);

        return (thumbIndexAngle > curlThreshold &&
                thumbMiddleAngle > curlThreshold &&
                thumbRingAngle > curlThreshold &&
                thumbPinkyAngle > curlThreshold);
    }

    private bool CheckForPalmUpGesture(HandsAggregatorSubsystem aggregator, XRNode hand)
    {
        return aggregator.TryGetPalmFacingAway(hand, out bool isPalmFacingUp) && isPalmFacingUp;
    }

    private bool CheckForShakaGesture(Quaternion thumbDistal, Quaternion indexDistal, Quaternion middleDistal, Quaternion ringDistal, Quaternion pinkyDistal)
    {
        float thumbIndexAngle = Quaternion.Angle(thumbDistal, indexDistal);
        float indexMiddleAngle = Quaternion.Angle(indexDistal, middleDistal);
        float middleRingAngle = Quaternion.Angle(middleDistal, ringDistal);
        float thumbPinkyAngle = Quaternion.Angle(thumbDistal, pinkyDistal);

        // Thumb and Pinky should be extended, other fingers should be curled
        return (thumbPinkyAngle > shakaThreshold && 
                thumbIndexAngle > curlThreshold && 
                indexMiddleAngle < curlThreshold && 
                middleRingAngle < curlThreshold);
    }

    private bool CheckForRockGesture(Quaternion thumbDistal, Quaternion indexDistal, Quaternion pinkyDistal)
    {
        float thumbIndexAngle = Quaternion.Angle(thumbDistal, indexDistal);
        float thumbPinkyAngle = Quaternion.Angle(thumbDistal, pinkyDistal);

        return (thumbIndexAngle > rockThreshold && thumbPinkyAngle > rockThreshold);
    }

    private float CalculateJointAngle(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 ab = a - b;
        Vector3 cb = c - b;

        return Vector3.Angle(ab, cb);
    }

    IEnumerator EnableWhenSubsystemAvailable()
    {
        Debug.Log("HandsAggregatorSubsystem Coroutine Started");
        yield return new WaitUntil(() => XRSubsystemHelpers.GetFirstRunningSubsystem<HandsAggregatorSubsystem>() != null);
        //GoAhead();
    }
}
