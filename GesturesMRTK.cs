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
    public float curlThreshold = 100f;
    public float fistThreshold = 105f;

    private HandsAggregatorSubsystem aggregator;
    private bool isMenuCooldown;

    void Start()
    {
        StartCoroutine(WaitForSubsystem());
    }

    void Update()
    {
        if (aggregator == null || isMenuCooldown) return;
        DetectAndHandleGestures();
    }

    private IEnumerator WaitForSubsystem()
    {
        yield return new WaitUntil(() => XRSubsystemHelpers.GetFirstRunningSubsystem<HandsAggregatorSubsystem>() != null);
        aggregator = XRSubsystemHelpers.GetFirstRunningSubsystem<HandsAggregatorSubsystem>();
    }

    private void DetectAndHandleGestures()
    {
        bool leftHandValid = aggregator.TryGetEntireHand(XRNode.LeftHand, out IReadOnlyList<HandJointPose> leftJoints);
        bool rightHandValid = aggregator.TryGetEntireHand(XRNode.RightHand, out IReadOnlyList<HandJointPose> rightJoints);

        int pose = 3; // 1 = Thumbs Up, 2 = Fist, 3 = None

        if (!leftHandValid && !rightHandValid)
        {
            // No hands detected
            return;
        }
        else if (leftHandValid && !rightHandValid)
        {
            pose = DetectGesture(XRNode.LeftHand);
        }
        else
        {
            pose = DetectGesture(XRNode.RightHand);
        }

        HandleMenuByPose(pose);
    }

    private int DetectGesture(XRNode hand)
    {
        // Try getting distal joints
        bool indexOK = aggregator.TryGetJoint(TrackedHandJoint.IndexDistal, hand, out HandJointPose indexDistalPose);
        bool thumbOK = aggregator.TryGetJoint(TrackedHandJoint.ThumbDistal, hand, out HandJointPose thumbDistalPose);
        bool middleOK = aggregator.TryGetJoint(TrackedHandJoint.MiddleDistal, hand, out HandJointPose middleDistalPose);
        bool ringOK = aggregator.TryGetJoint(TrackedHandJoint.RingDistal, hand, out HandJointPose ringDistalPose);
        bool pinkyOK = aggregator.TryGetJoint(TrackedHandJoint.LittleDistal, hand, out HandJointPose pinkyDistalPose);

        if (!(indexOK && thumbOK && middleOK && ringOK && pinkyOK))
            return 3; // No pose detected

        // Thumb up detection by comparing angles
        float thumbIndexAngle = Quaternion.Angle(thumbDistalPose.Rotation, indexDistalPose.Rotation);
        float thumbMiddleAngle = Quaternion.Angle(thumbDistalPose.Rotation, middleDistalPose.Rotation);
        float thumbRingAngle = Quaternion.Angle(thumbDistalPose.Rotation, ringDistalPose.Rotation);
        float thumbPinkyAngle = Quaternion.Angle(thumbDistalPose.Rotation, pinkyDistalPose.Rotation);

        if (thumbIndexAngle < curlThreshold &&
            thumbMiddleAngle < curlThreshold &&
            thumbRingAngle < curlThreshold &&
            thumbPinkyAngle < curlThreshold)
        {
            return 1; // Thumbs up
        }

        // Fist detection by joint angles
        bool indexProxOK = aggregator.TryGetJoint(TrackedHandJoint.IndexProximal, hand, out HandJointPose indexProximalPose);
        bool thumbProxOK = aggregator.TryGetJoint(TrackedHandJoint.ThumbProximal, hand, out HandJointPose thumbProximalPose);
        bool middleProxOK = aggregator.TryGetJoint(TrackedHandJoint.MiddleProximal, hand, out HandJointPose middleProximalPose);
        bool ringProxOK = aggregator.TryGetJoint(TrackedHandJoint.RingProximal, hand, out HandJointPose ringProximalPose);

        if (!(indexProxOK && thumbProxOK && middleProxOK && ringProxOK))
            return 3; // No pose detected

        float thumbIndexFist = CalculateJointAngle(thumbProximalPose.Position, thumbDistalPose.Position, indexDistalPose.Position);
        float indexMiddleFist = CalculateJointAngle(indexProximalPose.Position, indexDistalPose.Position, middleDistalPose.Position);
        float middleRingFist = CalculateJointAngle(middleProximalPose.Position, middleDistalPose.Position, ringDistalPose.Position);
        float ringPinkyFist = CalculateJointAngle(ringProximalPose.Position, ringDistalPose.Position, pinkyDistalPose.Position);

        if (thumbIndexFist < fistThreshold &&
            indexMiddleFist < fistThreshold &&
            middleRingFist < fistThreshold &&
            ringPinkyFist < fistThreshold)
        {
            return 2; // Fist
        }

        return 3; // No pose
    }

    private void HandleMenuByPose(int pose)
    {
        // 1 = Thumbs Up (show menu), 2 = Fist (hide menu)
        if (pose == 1 && !menu.activeSelf)
        {
            menu.SetActive(true);
            StartCoroutine(MenuCooldown());
        }
        else if (pose == 2 && menu.activeSelf)
        {
            menu.SetActive(false);
            StartCoroutine(MenuCooldown());
        }
    }

    private IEnumerator MenuCooldown(float seconds = 9f)
    {
        isMenuCooldown = true;
        yield return new WaitForSeconds(seconds);
        isMenuCooldown = false;
    }

    private float CalculateJointAngle(Vector3 proximal, Vector3 middle, Vector3 distal)
    {
        Vector3 v1 = middle - proximal;
        Vector3 v2 = distal - middle;
        return Vector3.Angle(v1, v2);
    }
}
