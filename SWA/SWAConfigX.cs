using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HEVS;
using System.IO;
using SimpleJSON;

// TODO: This does not use inheritence

/// <summary>
/// Adds extra configuration functionality, specifically for:
///     - JSON data for other classes
///     - Automatic tracker to displays
///     - HoloLens setup
/// </summary>
public class SWAConfigX : MonoBehaviour
{

    #region Variables
    /// <summary>
    /// The main extra config being used by all other classes.
    /// </summary>
    public static SWAConfig current;

    /// <summary>
    /// Links display id's to their Display Tracker Config
    /// </summary>
    public Dictionary<string, DisplayTrackerConfig> displayTrackerConfigs;

    /// <summary>
    /// JSON data for later use (starting from highest parent).
    /// </summary>
    public List<JSONNode> platformJSONs;

    /// <summary>
    /// The port for sending holoLens 6Dof
    /// </summary>
    [HideInInspector]
    public int holoPort = 6668;
    #endregion

    #region Unity Methods

    private void Awake()
    {
        //current = this;
        
        platformJSONs = GetPlatformJSONs();

        SetUpHoloPort();
    }

    void Start()
    {
        ReadDisplayTrackerConfigs();

        SetUpHoloLens();
    }

    private void Update()
    {
        // TODO: need a way to listen for tracker update.
        foreach (var displayConfig in PlatformConfig.current.displays)
        {
            if (displayTrackerConfigs.ContainsKey(displayConfig.id))
            {
                displayConfig.transform = displayTrackerConfigs[displayConfig.id].transform;

                // TODO: remove me
                if (displayConfig.type == DisplayType.OffAxis)
                {
                    displayConfig.offAxisData.ul = displayConfig.transform.rotate
                        * (displayTrackerConfigs[displayConfig.id].originalUL + displayConfig.transform.translate);
                    displayConfig.offAxisData.ll = displayConfig.transform.rotate
                        * (displayTrackerConfigs[displayConfig.id].originalLL + displayConfig.transform.translate);
                    displayConfig.offAxisData.lr = displayConfig.transform.rotate
                        * (displayTrackerConfigs[displayConfig.id].originalLR + displayConfig.transform.translate);
                }
            }
        }
    }
    #endregion

    #region Read JSONs

    /// <summary>
    /// Reopens the config file to get the JSON data again.
    /// </summary>
    /// <returns>The JSONNode of the current platform.</returns>
    private List<JSONNode> GetPlatformJSONs()
    {
        // Grab the config file and platform
        Configuration configuration = GetComponent<Configuration>();
        string configFile = configuration.configFile;
        string platformID = PlatformConfig.current.id;

        // Read the file again and get initial node
        StreamReader reader = new StreamReader(configFile);
        JSONNode node = JSON.Parse(reader.ReadToEnd());

        var allPlatforms = node["platforms"].Children;

        // The list to return
        List<JSONNode> platformJSONs = new List<JSONNode>();

        do
        {
            // Get the platform using the ID
            JSONNode platformJSON = allPlatforms.First(i => i["id"].Value.ToLower() == platformID);

            platformJSONs.Insert(0, platformJSON);

            // Now use the parent id
            if (platformJSON["inherited"] != null)
                platformID = platformJSON["inherited"].Value.ToLower();
            else
                platformID = null;

        } while (platformID != null);

        return platformJSONs;
    }

    /// <summary>
    /// Creates Trackers and Transform displays automatically.
    /// </summary>
    private void ReadDisplayTrackerConfigs()
    {
        // For creating the trackers
        displayTrackerConfigs = new Dictionary<string, DisplayTrackerConfig>();

        // Go through the hierachy
        foreach (var platformJSON in platformJSONs)
        {
            // Check every display of the platform
            foreach (JSONNode displayJSON in platformJSON["displays"].AsArray)
            {
                JSONNode trackerJSON = displayJSON["tracker"];

                if (trackerJSON != null)
                {
                    string displayID = displayJSON["id"].Value.ToLower();
                    DisplayTrackerConfig displayTracker = null;

                    // Tracker needs to be updated
                    if (displayTrackerConfigs.ContainsKey(displayID))
                    {
                        displayTracker = displayTrackerConfigs[displayID];
                    }
                    else
                    {
                        // Need a new tracker
                        displayTracker = new DisplayTrackerConfig();
                        displayTrackerConfigs.Add(displayID, displayTracker);
                    }

                    displayTracker.ParseConfig(trackerJSON);
                }
            }
        }

        foreach (var displayConfig in PlatformConfig.current.displays)
        {
            displayTrackerConfigs[displayConfig.id].originalTransform = displayConfig.transform;
            
            // TODO: remove me
            if (displayConfig.type == DisplayType.OffAxis)
            {
                displayTrackerConfigs[displayConfig.id].originalUL = displayConfig.offAxisData.ul;
                displayTrackerConfigs[displayConfig.id].originalLL = displayConfig.offAxisData.ll;
                displayTrackerConfigs[displayConfig.id].originalLR = displayConfig.offAxisData.lr;
            }
        }
    }
    #endregion

    #region HoloLens Setup

    // Gets the HoloPort from the json
    private void SetUpHoloPort()
    {
        // Get the holo port if available or use 6668
        foreach (var platformJSON in platformJSONs)
        {
            JSONNode json = platformJSON["cluster"];
            if (json != null)
            {
                json = json["holo_port"];
                if (json != null) holoPort = json.AsInt;
            }
        }
    }

    // Set up the HoloLens' camera
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
            container.position = new Vector3(-camera.transform.localPosition.x, 0f, -camera.transform.localPosition.y);
            container.eulerAngles = new Vector3(0f, -camera.transform.localEulerAngles.y, 0f);

            // Other HoloLens setup
            QualitySettings.SetQualityLevel(0); // TODO: could be costly
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            camera.nearClipPlane = 0.85f;
        }
    }

    // Check if this node is a holoLens
    private bool IsRunningOnHoloLens()
    {
        return false;

        foreach (var platformJSON in platformJSONs)
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
            
            if (holoLens != null) return true;
        }

        return false;
    }
    #endregion
}
