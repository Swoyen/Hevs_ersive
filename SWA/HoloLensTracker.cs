using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HEVS;
using OSCsharp.Data;
using OSCsharp.Net;
using OSCsharp.Utils;
using System;

/// <summary>
/// For sharing the transform of a HoloLens between nodes.
/// </summary>
public class HoloLensTracker : ClusterTransform
{
    #region Variables

    /// <summary>
    /// Store all HoloLensTrackers by their display ID 
    /// </summary>
    private static Dictionary<string, HoloLensTracker> _holoLens;
    
    /// <summary>
    /// The ID for the HoloLens' display config
    /// </summary>
    public string displayID;

    /// <summary>
    /// The actual display config
    /// </summary>
    public DisplayConfig display;

    public TransformData transformData;
    #endregion

    #region Unity Methods

    new void Start()
    {
        base.Start();

        // Find the display config
        display = PlatformConfig.current.displays.Find(i => i.id == displayID);
        
        // If this is the HoloLens display in question
        bool isHoloLens = NodeConfig.current.displays.Contains(display);

        // Is the holoLens
        if (isHoloLens/* && !Cluster.isMaster*/)
        {
            Debug.Log("Transmitter");
            if (_transmitter == null) CreateTransmitter();
        }
        // No purpose receiving if this is the sender
        else if (!isHoloLens/* && Cluster.isMaster*/)
        {
            Debug.Log("Receiver");
            if (_receiver == null) CreateReceiver();

            _holoLens[displayID] = this;
        }
    }

    public TextMesh t;

    void FixedUpdate()
    {
        // Transmit transform if there is a transmitter
        if (_transmitter != null)
            SendTransformData();

        if (_receiver != null)
        {
            transform.position = transformData.translate;
            transform.rotation = transformData.rotate;
        }

        t.text = transform.position + ", " + transform.eulerAngles;
    }

    private void OnEnable()
    {
        EnableOSC();
    }

    private void OnApplicationQuit()
    {
        DisableOSC();
    }

    private void OnDisable()
    {
        DisableOSC();
    }
    #endregion

    #region Transmitter to Master

    // The transmitter for sending OSC
    private static UDPTransmitter _transmitter;

    // Create the transmitter object
    private static void CreateTransmitter()
    {
        // Create and start the transmitter
        _transmitter = new UDPTransmitter("10.160.98.129"/*ClusterConfig.current.master.address*/, SWAConfig.current.holoPort);
        _transmitter.Connect();
    }

    // Send the cameras transform data
    private void SendTransformData()
    {
        // Gather the transform data
        OscMessage msg = new OscMessage("/" + displayID + "/transform");

        msg.Append(UnityEngine.Camera.main.transform.position.x);
        msg.Append(UnityEngine.Camera.main.transform.position.y);
        msg.Append(UnityEngine.Camera.main.transform.position.z);
        msg.Append(UnityEngine.Camera.main.transform.rotation.x);
        msg.Append(UnityEngine.Camera.main.transform.rotation.y);
        msg.Append(UnityEngine.Camera.main.transform.rotation.z);
        msg.Append(UnityEngine.Camera.main.transform.rotation.w);

        // Send the transform data to master
        _transmitter.Send(msg);
    }
    #endregion

    #region Receiver for Master

    // The receiver for receiving OSC
    private static UDPReceiver _receiver;

    // Create the receiver object
    private static void CreateReceiver()
    {
        // Create the reciever
        _receiver = new UDPReceiver(SWAConfig.current.holoPort, false);
        _receiver.MessageReceived += messageReceived;
        _receiver.ErrorOccured += oscErrorOccured;

        // Create the array of positions
        _holoLens = new Dictionary<string, HoloLensTracker>();

        // Start the reciever
        _receiver.Start();
    }

    // Handles OSC messages
    private static void messageReceived(object sender, OscMessageReceivedEventArgs oscMessageReceivedEventArgs)
    {
        OscMessage message = oscMessageReceivedEventArgs.Message;
        
        // Read the address part by part
        string[] address = message.Address.TrimStart('/').Split('/');

        // For the type of the message
        switch (address[1])
        {
            case "transform":
                if (_holoLens.ContainsKey(address[0]))
                {
                    HoloLensTracker holoLens = _holoLens[address[0]];
                    
                    holoLens.transformData.translate = new Vector3((float)message.Data[0], (float)message.Data[1], (float)message.Data[2]);
                    holoLens.transformData.rotate = new Quaternion((float)message.Data[3], (float)message.Data[4], (float)message.Data[5], (float)message.Data[6]);
                }
                break;
            case "input":
                // No way to currently handle input
                break;
        }
    }

    // Handles OSC errors
    private static void oscErrorOccured(object sender, ExceptionEventArgs exceptionEventArgs)
    {
        Debug.Log("HoloLens OSC Error: " + exceptionEventArgs.ToString());
    }
    #endregion

    #region Other OSC Methods

    private static void EnableOSC()
    {
        // Start reciever if it exists
        if (_receiver != null && !_receiver.IsRunning)
            _receiver.Start();

        // Connect transmitter if it exists
        if (_transmitter != null) _transmitter.Connect();
    }

    private static void DisableOSC()
    {
        // Stop reciever if it exists
        if (_receiver != null && _receiver.IsRunning)
            _receiver.Stop();

        // Close transmitter if it exists
        if (_transmitter != null) _transmitter.Close();
    }
    #endregion
}
