# MRTK3 Custom Gesture Recognition for Unity 
## Problem
##### MRTK3 in Unity uses version of XR Hands 1.3.0, which doesn't have prefabs for custom gesture definition. It is still possible to use tap, air-tap in Unity project to interact with MRTK UI prefabs and other objects seamlessly, but that's not the case if you want to have a Thumbs-Up, Fist, etc. This script provides support for Thumbs-Up and Fist ( more gestures will be available in the future ). This implementation is a reverse-engineered version of the model descriped in [XR Hands 1.4.1](https://docs.unity3d.com/Packages/com.unity.xr.hands@1.4/manual/gestures/hand-orientation.html) and is based on Vector3 and Quaternion manipulations of joints data.
> [!WARNING]
> Don't be a smart cookie like me and DO NOT update XR Hands to 1.4.1 or higher if your MRTK3 project has XR Hands 1.3.0. XR Rig won't be able to track your hands in the scene in the scene anymore and it will break your project 
## Supported Gestures
- Thumbs Up
- Fist
- Victory, Thumbs Down, Palm Up, Shaka, Rock ( in process )  
## Input
![script](https://i.gyazo.com/88007b0884c7ecdb5284c0bd46832844.png)
- Menu - some Game Object in scene that is SetActive by Thumbs-Up and Fist 
- Curl Threshold - defines sensitivity of Thumbs-Up
- Fist Threshold - defines sensitivity of Fist
 
 
