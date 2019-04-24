using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HEVS;
using System.IO;
using SimpleJSON;
using UnityOSC;

// TODO: This does not use inheritence

/// <summary>
/// Adds extra configuration functionality, specifically for:
///     - JSON data for other classes
///     - Automatic tracker to displays
///     - HoloLens setup
/// </summary>
public class SWAConfig : MonoBehaviour
{
    /// <summary>
    /// The main extra config being used by all other classes.
    /// </summary>
    public static SWAConfig current;

    /// <summary>
    /// JSON data for later use.
    /// </summary>
    public JSONNode platformJSON;

    /// <summary>
    /// The port for sending holoLens 6Dof
    /// </summary>
    [HideInInspector]
    public int holoPort = 6668;

    // For receiving holoLens positioning
    private OSCReciever _reciever;
    private Dictionary<string, HoloLensTracker> _holoTrackerFromID;

    private void Awake()
    {
        current = this;
        
        // Get the configuration
        platformJSON = GetPlatformJSON();

        if (platformJSON != null)
        {
            // Get the holo port if available or use 6668
            JSONNode json = platformJSON["cluster"];
            if (json != null)
            {
                json = json["holo_port"];
                if (json != null) holoPort = json.AsInt;
            }
        }
    }

    void Start()
    {
        if (platformJSON != null)
        {
            CreateTrackerToDisplays();

            SetUpHoloLens();
        }
    }

    private void Update()
    {
        /*if (_reciever != null)
        {
            // read messages while there are still messages to read
            while (_reciever.hasWaitingMessages())
            {
                OSCMessage msg = _reciever.getNextMessage();
                
                // Update the holoLens tracker if it exists
                if (_holoTrackerFromID.ContainsKey(msg.Address))
                    //_holoTrackerFromID[msg.Address].UpdateTransform(msg.Data);
                else
                    Debug.Log("Invalid Address: " + msg.Address + ", " + msg.Data);
            }
        }*/
    }

    /// <summary>
    /// Registers a holoLens tracker for the master node.
    /// </summary>
    /// <param name="tracker">The holoLens tracker of the master.</param>
    public void RegisterHoloLensTracker(HoloLensTracker tracker)
    {
        // Create the reciever and dictionary if there is none
        if (_reciever == null)
        {
            _reciever = new OSCReciever();
            _reciever.Open(holoPort);
            _holoTrackerFromID = new Dictionary<string, HoloLensTracker>();
        }

        // add tracker to dictionary
        _holoTrackerFromID.Add(tracker.displayID, tracker);
    }

    /// <summary>
    /// Reopens the config file to get the JSON data again.
    /// </summary>
    /// <returns>The JSONNode of the current platform.</returns>
    private JSONNode GetPlatformJSON()
    {
        // Grab the config file and platform
        Configuration configuration = GetComponent<Configuration>();
        string configFile = configuration.configFile;
        string platformID = PlatformConfig.current.id;

        // Read the file again and get initial node
        StreamReader reader = new StreamReader(configFile);
        JSONNode node = JSON.Parse(reader.ReadToEnd());

        JSONArray array = node["platforms"].AsArray;

        // Look for the platform id
        foreach (JSONNode platform in array)
        {
            if (platform["id"].Value.ToLower() == platformID)
                return platform;
        }

        return null;
    }

    /// <summary>
    /// Creates Trackers and Transform displays automatically.
    /// </summary>
    private void CreateTrackerToDisplays()
    {
        // For creating the trackers
        var trackerToDisplays = new Dictionary<string, TransformOffAxis>();

        // Check every display for a tracker
        foreach (JSONNode displayJSON in platformJSON["displays"].AsArray)
        {
            JSONNode trackerJSON = displayJSON["tracker"];

            if (trackerJSON != null)
            {
                // Get the ids
                string trackerID = trackerJSON.Value;
                string displayID = displayJSON["id"];

                // If the tracker already exists
                if (trackerToDisplays.ContainsKey(trackerID))
                    trackerToDisplays[trackerID].displayIDs.Add(displayID);
                else
                {
                    // Create the tracking object
                    GameObject trackedObject = new GameObject(displayID + "-TransformDisplay");

                    // Set up the tracker
                    VRPNTracker tracker = trackedObject.AddComponent<VRPNTracker>();
                    tracker.trackerID = trackerID;

                    // Set up the display
                    var transformDisplay = trackedObject.AddComponent<TransformOffAxis>();
                    transformDisplay.displayIDs = new List<string>() { displayID };

                    // Add it to the dictionary
                    trackerToDisplays.Add(trackerID, transformDisplay);
                }
            }
        }
    }

    /// <summary>
    /// Runs Hololens setup if display type is set to "holoLens".
    /// </summary>
    private void SetUpHoloLens()
    {
        if (IsRunningOnHoloLens())
        {
            // TODO: Right now this just changes the holoLens' starting point to be the HEVS' origin.

            // Get the holoLens' camera's transform
            UnityEngine.Camera camera = UnityEngine.Camera.main;

            // Create a container to adjust the difference between HoloLens' origin and HEVS' origin
            Transform container = new GameObject("HoloLensContainer").transform;

            // Make the container the camera's parent
            container.parent = camera.transform.parent;
            camera.transform.parent = container;

            // Change position and direction
            container.position = new Vector3(-camera.transform.localPosition.x, 0f, -camera.transform.localPosition.z);
            container.eulerAngles = new Vector3(0f, -camera.transform.localEulerAngles.y, 0f);

            // Other HoloLens setup
            QualitySettings.SetQualityLevel(0); // TODO: could be costly
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            camera.nearClipPlane = 0.85f;
        }
    }

    private bool IsRunningOnHoloLens()
    {

        // Get the holoLens display if it exists
        var holoLens = platformJSON["displays"].Children.FirstOrDefault(i =>
        {
            var typeJSON = i["type"];

            // Ignore if type is not holoLens
            if (typeJSON == null || typeJSON.Value.ToLower() != "hololens")
                return false;

            // Return true if holoLens is on this node
            string id = i["id"].Value.ToLower();
            return NodeConfig.current.displays.Exists(j => j.id == id);
        });
        return holoLens != null;
    }
    
    private void OnApplicationQuit()
    {
        if (_reciever != null)
            _reciever.Close();
    }
}
