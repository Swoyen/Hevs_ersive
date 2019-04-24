using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using HEVS;

/// <summary>
/// Config connecting a display to a tracker.
/// </summary>
public class DisplayTrackerConfig
{
    #region Config Variables

    // The actual tracker
    private VRPNTrackerConfig _tracker;

    /// <summary>
    /// The Tracker to transform the display to.
    /// </summary>
    public VRPNTrackerConfig tracker {
        get { return _tracker; }
        set
        {
            // TODO: This is dependent on device being constantly updated
            _device = VRPN.GetDevice(value.address);
            _tracker = value;
        }
    }
    
    // Translation locks
    public bool translateX = true;
    public bool translateY = true;
    public bool translateZ = true;

    // Rotation locks
    public bool rotateX = true;
    public bool rotateY = true;
    public bool rotateZ = true;
    #endregion

    #region Accessing Updated Transforms

    // The device associated to the tracker
    private VRPN.IDevice _device;

    /// <summary>
    /// The original transform of the display.
    /// </summary>
    public TransformData originalTransform;

    public Vector3 originalUL;
    public Vector3 originalLL;
    public Vector3 originalLR;

    /// <summary>
    /// The transform using the tracker.
    /// </summary>
    public TransformData transform
    {
        get
        {
            TransformData transform = new TransformData();
            
            // Get the position ignoring locks
            var translate = _device.transform.translate;
            transform.translate = originalTransform.translate + new Vector3(
                translateX ? translate.x : 0f,
                translateY ? translate.y : 0f,
                translateZ ? translate.z : 0f);
            
            // Get the rotation ignoring locks
            var euler = _device.transform.rotate.eulerAngles;
            transform.rotate = originalTransform.rotate * Quaternion.Euler(
                rotateX ? euler.x : 0f,
                rotateY ? euler.y : 0f,
                rotateZ ? euler.z : 0f);

            return transform;
        }
    }
    #endregion

    #region Create the Display Tracker

    public DisplayTrackerConfig() { }

    public void ParseConfig(SimpleJSON.JSONNode jsonNode)
    {
        // There are no translation or rotation locks
        if (jsonNode.Value != null)
        {
            tracker = PlatformConfig.current.trackers.First(i => i.id == jsonNode.Value);
        }
        else
        {
            // Check for tracker a tracker
            if (jsonNode["id"] != null) tracker = PlatformConfig.current.trackers.First(i => i.id == jsonNode["id"].Value);

            // Check for translate constraints
            if (jsonNode["translateX"] != null) translateX = jsonNode["translateX"].AsBool;
            if (jsonNode["translateY"] != null) translateY = jsonNode["translateY"].AsBool;
            if (jsonNode["translateZ"] != null) translateZ = jsonNode["translateZ"].AsBool;

            // Check for rotate constraints
            if (jsonNode["rotateX"] != null) rotateX = jsonNode["rotateX"].AsBool;
            if (jsonNode["rotateY"] != null) rotateY = jsonNode["rotateY"].AsBool;
            if (jsonNode["rotateZ"] != null) rotateX = jsonNode["rotateZ"].AsBool;
        }
    }
    #endregion
}