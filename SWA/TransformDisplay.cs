using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HEVS;

// Could make this inherit from ClusterTransform

/// <summary>
/// Transforms a display by this objects transform.
/// </summary>
public class TransformDisplay : MonoBehaviour
{
    /// <summary>
    /// Threshold in metres to break before moving the display.
    /// </summary>
    public float positionThreshold;

    /// <summary>
    /// Threshold in degrees to break before rotating the display.
    /// </summary>
    public float rotationThreshold;

    // The id of the display to update
    public List<string> displayIDs;
    private StoredDisplay[] _displays;

    // the last important transform
    private Vector3 _position;
    private Quaternion _rotation;

    // Start is called before the first frame update
    new void Start()
    {

        // Get all the displays using IDs
        _displays = displayIDs.Select(i => new StoredDisplay(NodeConfig.current.displays.First(j => j.id == i))).ToArray();

        foreach (StoredDisplay display in _displays)
        {
            // Directly set starting transform
            display.config.transform.translate = display.originalTransform.translate + transform.position;
            display.config.transform.rotate = display.originalTransform.rotate * transform.rotation;
        }

        // Store initial transform
        _position = transform.position;
        _rotation = transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        // Update goals if either threshold is breached
        if (Vector3.Distance(_position, transform.position) > positionThreshold ||
            Quaternion.Angle(_rotation, transform.rotation) > rotationThreshold)
        {
            _position = transform.localPosition;
            _rotation = transform.localRotation;
        }

        TransformDisplays(0.5f);

        // TODO: this'll crash if the display touches the origin
    }

    /// <summary>
    /// Lerps the display to its goal.
    /// </summary>
    /// <param name="t">Value between 0 and 1.</param>
    private void TransformDisplays(float t)
    {
        foreach (StoredDisplay display in _displays)
        {
            display.config.transform.translate = Vector3.Lerp(display.config.transform.translate,
                display.originalTransform.translate + _position, t);
            display.config.transform.rotate = Quaternion.Lerp(display.config.transform.rotate,
                display.originalTransform.rotate * _rotation, t);
        }
    }

    /// <summary>
    /// Holds a display config and its initial transform.
    /// </summary>
    private class StoredDisplay
    {
        /// <summary>
        /// The connected display.
        /// </summary>
        public DisplayConfig config;
        
        /// <summary>
        /// The original transform of the display.
        /// </summary>
        public TransformData originalTransform;

        public Vector3 originalUL;
        public Vector3 originalLL;
        public Vector3 originalLR;

        /// <summary>
        /// Gets the new transform of the display.
        /// </summary>
        /*private TransformData UpdatedTransform(TransformData transform)
        {
                DisplayTrackerConfig constraints = config

                // Get the position ignoring locks
                var translate = transform.translate;
                transform.translate = originalTransform.translate + new Vector3(
                    constraints.translateX ? translate.x : 0f,
                    constraints.translateY ? translate.y : 0f,
                    constraints.translateZ ? translate.z : 0f);

                // Get the rotation ignoring locks
                var euler = transform.rotate.eulerAngles;
                transform.rotate = originalTransform.rotate * Quaternion.Euler(
                    constraints.rotateX ? euler.x : 0f,
                    constraints.rotateY ? euler.y : 0f,
                    constraints.rotateZ ? euler.z : 0f);

                return transform;
        }*/

        /// <summary>
        /// Constructs a stored display, storing the original transform.
        /// </summary>
        /// <param name="config">The connected display.</param>
        public StoredDisplay (DisplayConfig config) {
            this.config = config;
            originalTransform = config.transform;
        }
    }
}