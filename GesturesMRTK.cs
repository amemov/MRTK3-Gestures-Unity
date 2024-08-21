using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MixedReality.Toolkit.Subsystems;
using MixedReality.Toolkit;
using MixedReality.Toolkit.Input;
using UnityEngine.XR;
using UnityEngine.XR.Hands;
 

public class GesturesMRTK : MonoBehaviour
{
    public GameObject menu;
    public float curlThreshold  = 100f; // Adjust as needed
    public float fistThreshold  = 105f; // Adjust as needed 
    

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine("EnableWhenSubsystemAvailable");
         
    }

    // Update is called once per frame
    void Update()
    {
       StartCoroutine("Gesture");  
    }
    IEnumerator Gesture()
    {
        int ok = 10;
        while (ok == 10)
        {
            var aggregator = XRSubsystemHelpers.GetFirstRunningSubsystem<HandsAggregatorSubsystem>();
            
            // Get a single joint (Index tip, on left hand, for example)
            //bool jointIsValid = aggregator.TryGetJoint(TrackedHandJoint.IndexTip, XRNode.LeftHand, out HandJointPose jointPose);
            // Get an entire hand's worth of joints from the left and right hand.
            bool leftJointsAreValid = aggregator.TryGetEntireHand(XRNode.LeftHand, out IReadOnlyList<HandJointPose> leftJoints);
            bool rightJointsAreValid = aggregator.TryGetEntireHand(XRNode.RightHand, out IReadOnlyList<HandJointPose> rightJoints);
            // Check whether the user's left hand is facing away (commonly used to check "aim" intent)
            // This is adjustable with the HandFacingAwayTolerance option in the Aggregator configuration.
            // "handIsValid" represents whether there was valid hand data in the first place!
            bool handIsValid = aggregator.TryGetPalmFacingAway(XRNode.LeftHand, out bool isLeftPalmFacingAway);

            // Query pinch characteristics from the left hand.
            // pinchAmount is [0,1], normalized to the open/closed thresholds specified in the Aggregator configuration.
            // "isReadyToPinch" is adjusted with the HandRaiseCameraFOV and HandFacingAwayTolerance settings in the configuration.
            handIsValid = aggregator.TryGetPinchProgress(XRNode.LeftHand, out bool isReadyToPinch, out bool isPinching, out float pinchAmount);

            /**********************************************************************************************
            *                                     Check for Poses                                        *
            *                  1 - Thumb Up | 2 - Fist | 3 - No Pose (default)                           *
            **********************************************************************************************
            *   Hand recognition logic: we just check if some joints are not present in front of camera. *
            **********************************************************************************************/
            int pose = 3;
            
            // Base case - no hands => no pose
            if(leftJointsAreValid == false && rightJointsAreValid == false)
            {
                // empty
            }
            // 1 - left hand valid AND right invalid
            else if (leftJointsAreValid == true && rightJointsAreValid == false)
            {

                Quaternion indexDistal, thumbDistal, middleDistal, ringDistal, pinkyDistal;
                bool indexDistalIsValid  = aggregator.TryGetJoint(TrackedHandJoint.IndexDistal, XRNode.LeftHand, out HandJointPose indexDistalPose);
                bool thumbDistalIsValid  = aggregator.TryGetJoint(TrackedHandJoint.ThumbDistal, XRNode.LeftHand, out HandJointPose thumbDistalPose);
                bool middleDistalIsValid = aggregator.TryGetJoint(TrackedHandJoint.MiddleDistal, XRNode.LeftHand, out HandJointPose middleDistalPose);
                bool ringDistalIsValid   = aggregator.TryGetJoint(TrackedHandJoint.RingDistal, XRNode.LeftHand, out HandJointPose ringDistalPose);
                bool pinkyDistalIsValid  = aggregator.TryGetJoint(TrackedHandJoint.LittleDistal, XRNode.LeftHand, out HandJointPose pinkyDistalPose); 
                // Fun fact: for some unkown fucking reason microsoft decided to rename Pinky in 'Little' finger
 
                indexDistal = indexDistalPose.Rotation;
                thumbDistal = thumbDistalPose.Rotation;   
                middleDistal = middleDistalPose.Rotation;
                ringDistal = ringDistalPose.Rotation;
                pinkyDistal = pinkyDistalPose.Rotation;

                // Calculate angle between thumb and index/middle/ring/pinky fingers - FOR THUMBS UP
                float thumbIndexAngle = Quaternion.Angle(thumbDistal, indexDistal);
                float thumbMiddleAngle = Quaternion.Angle(thumbDistal, middleDistal);
                float thumbRingAngle = Quaternion.Angle(thumbDistal, ringDistal);
                float thumbPinkyAngle = Quaternion.Angle(thumbDistal, pinkyDistal);


                if (thumbIndexAngle < curlThreshold &&
                thumbMiddleAngle < curlThreshold &&
                thumbRingAngle < curlThreshold &&
                thumbPinkyAngle < curlThreshold)
                {
                    pose = 1;
                    //Debug.Log("Thumbs-up - Left Hand");
                }
                else
                {
                    // Get some proximal Joints
                    bool indexProximalIsValid  = aggregator.TryGetJoint(TrackedHandJoint.IndexProximal, XRNode.LeftHand, out HandJointPose indexProximalPose);
                    bool thumbProximalIsValid  = aggregator.TryGetJoint(TrackedHandJoint.ThumbProximal, XRNode.LeftHand, out HandJointPose thumbProximalPose);
                    bool middleProximalIsValid = aggregator.TryGetJoint(TrackedHandJoint.MiddleProximal, XRNode.LeftHand, out HandJointPose middleProximalPose);
                    bool ringProximalIsValid = aggregator.TryGetJoint(TrackedHandJoint.RingProximal, XRNode.LeftHand, out HandJointPose ringProximalPose); 

                    float thumbbIndexAngle = CalculateJointAngle(thumbProximalPose.Position,   thumbDistalPose.Position,  indexDistalPose.Position);
                    float indexMiddleAngle = CalculateJointAngle(indexProximalPose.Position,   indexDistalPose.Position,  middleDistalPose.Position);
                    float middleRingAngle  = CalculateJointAngle(middleProximalPose.Position, middleDistalPose.Position, ringDistalPose.Position);
                    float ringPinkyAngle   = CalculateJointAngle(ringProximalPose.Position,    ringDistalPose.Position,   pinkyDistalPose.Position);

                    // Check if all angles fall below the threshold
                    if (thumbbIndexAngle < fistThreshold &&
                        indexMiddleAngle < fistThreshold &&
                        middleRingAngle < fistThreshold &&
                        ringPinkyAngle < fistThreshold)
                    {
                        // Fist gesture detected
                        pose = 2;
                        //Debug.Log("Fist - Left Hand");
                    }
                }

            }
            // 2 - right hand valid AND left invalid OR both valid (still check just right hand)
            else if ( (leftJointsAreValid == false && rightJointsAreValid == true) || (leftJointsAreValid == true && rightJointsAreValid == true) )
            {
                Quaternion indexDistal, thumbDistal, middleDistal, ringDistal, pinkyDistal;
                bool indexDistalIsValid  = aggregator.TryGetJoint(TrackedHandJoint.IndexDistal, XRNode.RightHand, out HandJointPose indexDistalPose);
                bool thumbDistalIsValid  = aggregator.TryGetJoint(TrackedHandJoint.ThumbDistal, XRNode.RightHand, out HandJointPose thumbDistalPose);
                bool middleDistalIsValid = aggregator.TryGetJoint(TrackedHandJoint.MiddleDistal, XRNode.RightHand, out HandJointPose middleDistalPose);
                bool ringDistalIsValid   = aggregator.TryGetJoint(TrackedHandJoint.RingDistal, XRNode.RightHand, out HandJointPose ringDistalPose);
                bool pinkyDistalIsValid  = aggregator.TryGetJoint(TrackedHandJoint.LittleDistal, XRNode.RightHand, out HandJointPose pinkyDistalPose); 
                // Fun fact: for some unkown fucking reason microsoft decided to rename Pinky in 'Little' finger
 
                indexDistal = indexDistalPose.Rotation;
                thumbDistal = thumbDistalPose.Rotation;   
                middleDistal = middleDistalPose.Rotation;
                ringDistal = ringDistalPose.Rotation;
                pinkyDistal = pinkyDistalPose.Rotation;

                // Calculate angle between thumb and index/middle/ring/pinky fingers - FOR THUMBS UP
                float thumbIndexAngle = Quaternion.Angle(thumbDistal, indexDistal);
                float thumbMiddleAngle = Quaternion.Angle(thumbDistal, middleDistal);
                float thumbRingAngle = Quaternion.Angle(thumbDistal, ringDistal);
                float thumbPinkyAngle = Quaternion.Angle(thumbDistal, pinkyDistal);


                if (thumbIndexAngle < curlThreshold &&
                thumbMiddleAngle < curlThreshold &&
                thumbRingAngle < curlThreshold &&
                thumbPinkyAngle < curlThreshold)
                {
                    pose = 1;
                    //Debug.Log("Thumbs-up - Right Hand");
                }
                else
                {
                    // Get some proximal Joints
                    bool indexProximalIsValid  = aggregator.TryGetJoint(TrackedHandJoint.IndexProximal, XRNode.RightHand, out HandJointPose indexProximalPose);
                    bool thumbProximalIsValid  = aggregator.TryGetJoint(TrackedHandJoint.ThumbProximal, XRNode.RightHand, out HandJointPose thumbProximalPose);
                    bool middleProximalIsValid = aggregator.TryGetJoint(TrackedHandJoint.MiddleProximal, XRNode.RightHand, out HandJointPose middleProximalPose);
                    bool ringProximalIsValid = aggregator.TryGetJoint(TrackedHandJoint.RingProximal, XRNode.RightHand, out HandJointPose ringProximalPose); 

                    float thumbbIndexAngle = CalculateJointAngle(thumbProximalPose.Position,   thumbDistalPose.Position,  indexDistalPose.Position);
                    float indexMiddleAngle = CalculateJointAngle(indexProximalPose.Position,   indexDistalPose.Position,  middleDistalPose.Position);
                    float middleRingAngle  = CalculateJointAngle(middleProximalPose.Position, middleDistalPose.Position, ringDistalPose.Position);
                    float ringPinkyAngle   = CalculateJointAngle(ringProximalPose.Position,    ringDistalPose.Position,   pinkyDistalPose.Position);

                    // Check if all angles fall below the threshold
                    if (thumbbIndexAngle < fistThreshold &&
                        indexMiddleAngle < fistThreshold &&
                        middleRingAngle < fistThreshold &&
                        ringPinkyAngle < fistThreshold)
                    {
                        // Fist gesture detected
                        pose = 2;
                        //Debug.Log("Fist - Right Hand");
                    }
                     
                }
            }

            if (pose == 1)
            {
                // Open Menu
                if(menu.activeSelf == false) // to prevent unnecesary calls to Unity Engine which make the UI look glitchy
                {
                    menu.SetActive(true);
                    yield return new WaitForSeconds(9);
                }     
                 
            }
            else if (pose == 2)
            {
                // Hide Menu
                if(menu.activeSelf != false) // to prevent unnecesary calls to Unity Engine which make the UI look glitchy
                {
                    menu.SetActive(false); 
                    yield return new WaitForSeconds(9);
                }        
                 
            }
            yield return 0;
        }
        
    }
    private float CalculateJointAngle (Vector3 proximalPose, Vector3 middlePose, Vector3 distalPose)
    {
        // Calculate the vectors between the joints
        Vector3 proximalToMiddle = middlePose - proximalPose;
        Vector3 middleToDistal = distalPose - middlePose;

        // Calculate and return the angle between the vectors    
        return Vector3.Angle(proximalToMiddle, middleToDistal);
    }

    IEnumerator EnableWhenSubsystemAvailable()
    {
        Debug.Log("HandsAggregatorSubsystem Coroutine Started");
        yield return new WaitUntil(() => XRSubsystemHelpers.GetFirstRunningSubsystem<HandsAggregatorSubsystem>() != null);
        //GoAhead();
    } 

}
