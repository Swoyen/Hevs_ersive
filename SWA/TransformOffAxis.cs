using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HEVS;

// TODO: the other implementation is better and uses transform, but for some reason transform is not implemented for off axis

public class TransformOffAxis : MonoBehaviour
{
    public float positionThreshold;

    public float rotationThreshold;

    // The id of the display to update
    public List<string> displayIDs;
    private LinkedDisplay[] _displays;

    //
    private Vector3 _prevPos;
    private Quaternion _prevRot;

    // Start is called before the first frame update
    void Start()
    {
        _displays = new LinkedDisplay[displayIDs.Count];

        for (int i = 0; i < _displays.Length; i++)
        {
            LinkedDisplay display = new LinkedDisplay();
            display.config = PlatformConfig.current.displays.First(j => j.id == displayIDs[i]);

            // Store initial corners
            if (display.config.offAxisData != null)
            {
                display.ul = display.config.offAxisData.ul;
                display.ll = display.config.offAxisData.ll;
                display.lr = display.config.offAxisData.lr;
            }

            // Directly set starting transform
            display.config.offAxisData.ul = transform.TransformPoint(display.ul);
            display.config.offAxisData.ll = transform.TransformPoint(display.ll);
            display.config.offAxisData.lr = transform.TransformPoint(display.lr);

            // Set the goals to match
            display.goalUl = display.config.offAxisData.ul;
            display.goalLl = display.config.offAxisData.ll;
            display.goalLr = display.config.offAxisData.lr;

            _displays[i] = display;
        }

        // Store initial transform
        _prevPos = transform.localPosition;
        _prevRot = transform.localRotation;
    }

    // Update is called once per frame
    void Update()
    {
        // Update goals if either threshold is breached
        if (Vector3.Distance(_prevPos, transform.localPosition) > positionThreshold ||
            Quaternion.Angle(_prevRot, transform.localRotation) > rotationThreshold)
        {
            UpdateLinkedDisplayGoals();
        }

        LerpLinkedDisplays(0.8f);

        // TODO: this'll crash if the display touches the origin
    }

    private void UpdateLinkedDisplayGoals()
    {
        foreach (LinkedDisplay display in _displays)
            if (display.config.offAxisData != null)
            {
                display.goalUl = transform.TransformPoint(display.ul);
                display.goalLl = transform.TransformPoint(display.ll);
                display.goalLr = transform.TransformPoint(display.lr);
            }

        // Store new previous transform
        _prevPos = transform.localPosition;
        _prevRot = transform.localRotation;
    }

    private void LerpLinkedDisplays(float t)
    {
        foreach (LinkedDisplay display in _displays)
        {
            display.config.offAxisData.ul = Vector3.Lerp(display.config.offAxisData.ul, display.goalUl, t);
            display.config.offAxisData.ll = Vector3.Lerp(display.config.offAxisData.ll, display.goalLl, t);
            display.config.offAxisData.lr = Vector3.Lerp(display.config.offAxisData.lr, display.goalLr, t);
        }
    }

    private class LinkedDisplay
    {
        // The connected display
        public DisplayConfig config;

        // Store the original positions
        public Vector3 ul;
        public Vector3 ll;
        public Vector3 lr;

        // Store the goal positions for lerping
        public Vector3 goalUl;
        public Vector3 goalLl;
        public Vector3 goalLr;
    }
}