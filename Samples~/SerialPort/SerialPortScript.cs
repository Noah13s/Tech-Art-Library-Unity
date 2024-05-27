using UnityEngine;
using System.Collections;

using System.IO.Ports;
using System;

using System.Threading;

using System.Collections.Generic;


public class SerialPortScript : MonoBehaviour
{
    public bool ShowDebugs = true;
    private string portStatus = "";


    public SerialPort SerialPort;
    [Header("SerialPort")]

    // Current com port and set of default
    public string ComPort = "COM5";

    // Current baud rate and set of default
    // 300, 600, 1200, 2400, 4800, 9600, 14400, 19200, 28800, 38400, 57600, 115200
    public int BaudRate = 115200;

    // The parity-checking protocol.
    public Parity Parity = Parity.None;

    // The standard number of stopbits per byte.
    public StopBits StopBits = StopBits.One;

    // The standard length of data bits per byte.
    public int DataBits = 8;

    // The state of the Data Terminal Ready(DTR) signal during serial communication.
    public bool DtrEnable;

    // Whether or not the Request to Send(RTS) signal is enabled during serial communication.
    public bool RtsEnable;

    // Read and write timeouts

    public int ReadTimeout = 10;
    public int WriteTimeout = 10;

    private ArrayList comPorts =
    new ArrayList();

    [Header("Misc")]
    public List<string> ComPorts =
    new List<string>();

    // Define a delegate for our event to use. Delegates 
    // encapsulate both an object instance and a method 
    // and are similar to c++ pointers.

    public delegate void SerialDataParseEventHandler(string[] data, string rawData);

    // Define the event that utilizes the delegate to
    // fire off a notification to all registered objs 

    public static event SerialDataParseEventHandler SerialDataParseEvent;

    // Delegate and event for serialport open notification

    public delegate void SerialPortOpenEventHandler();
    public static event SerialPortOpenEventHandler SerialPortOpenEvent;

    // Delegate and event for serialport close notification

    public delegate void SerialPortCloseEventHandler();
    public static event SerialPortCloseEventHandler SerialPortCloseEvent;

    // Delegate and event for serialport sentData notification

    public delegate void SerialPortSentDataEventHandler(string data);
    public static event SerialPortSentDataEventHandler SerialPortSentDataEvent;

    // Delegate and event for serialport sentLineData notification

    public delegate void SerialPortSentLineDataEventHandler(string data);
    public static event SerialPortSentLineDataEventHandler SerialPortSentLineDataEvent;

    // Start is called before the first frame update
    void Start()
    {
        // Register for a notification of the open port event

        SerialPortOpenEvent +=
            new SerialPortOpenEventHandler(UnitySerialPort_SerialPortOpenEvent);

        // Register for a notification of the close port event

        SerialPortCloseEvent +=
            new SerialPortCloseEventHandler(UnitySerialPort_SerialPortCloseEvent);

        // Register for a notification of data sent

        SerialPortSentDataEvent +=
            new SerialPortSentDataEventHandler(UnitySerialPort_SerialPortSentDataEvent);

        // Register for a notification of data sent

        SerialPortSentLineDataEvent +=
            new SerialPortSentLineDataEventHandler(UnitySerialPort_SerialPortSentLineDataEvent);

        // Register for a notification of the SerialDataParseEvent

        SerialDataParseEvent +=
            new SerialDataParseEventHandler(UnitySerialPort_SerialDataParseEvent);
    }

    // Update is called once per frame
    void Update()
    {
        if (SerialPort != null && SerialPort.IsOpen && SerialPort.BytesToRead > 0)
        {
            try
            {
                string line = SerialPort.ReadLine();
                // Process the line
                Debug.Log(line);
            }
            catch (TimeoutException)
            {
                Debug.LogWarning("ReadLine timed out.");
            }
            catch (Exception ex)
            {
                Debug.LogError("Error reading from serial port: " + ex.Message);
            }
        }
    }



    public void ShowDebugMessages(string portStatus)
    {
        print(portStatus);
    }

    [ContextMenu("Refresh COM Ports")]
    public void PopulateComPorts()
    {
        // Clear list before adding detected COM ports to fix duplicates 
        ComPorts.Clear();
        comPorts.Clear();

        // Loop through all available ports and add them to the list
        foreach (string cPort in SerialPort.GetPortNames())
        {
            ComPorts.Add(cPort);

            comPorts.Add(cPort);

            // Debug.Log(cPort.ToString());
        }
    }

    [ContextMenu("Connect to Port")]
    public void OpenSerialPort()
    {
        try
        {
            // Initialise the serial port
            SerialPort = new SerialPort(ComPort, BaudRate, Parity, DataBits, StopBits);

            SerialPort.ReadTimeout = ReadTimeout;
            SerialPort.WriteTimeout = WriteTimeout;

            SerialPort.DtrEnable = DtrEnable;
            SerialPort.RtsEnable = RtsEnable;

            SerialPort.Handshake = Handshake.RequestToSendXOnXOff;

            // Open the serial port
            SerialPort.Open();
            Debug.Log("Serial Connected");
            //StartSerialThread();
        }
        catch (Exception ex)
        {
            // Failed to open com port or start serial thread
            Debug.Log("Error 1: " + ex.Message.ToString());
            PopulateComPorts();
        }

    }

    [ContextMenu("Close connection")]
    public void CloseSerialPort()
    {

        try
        {
            // Close the serial port
            SerialPort.Close();
            Debug.Log("Connection closed");

        }
        catch (Exception ex)
        {
            if (SerialPort == null || SerialPort.IsOpen == false)
            {
                // Failed to close the serial port. Uncomment if
                // you wish but this is triggered as the port is
                // already closed and or null.

                Debug.Log("Error 2A: " + "Port already closed!");
            }
            else
            {
                // Failed to close the serial port
                Debug.Log("Error 2B: " + ex.Message.ToString());
            }
        }
    }

    #region Notification Events

    /// <summary>
    /// Data parsed serialport notification event
    /// </summary>
    /// <param name="Data">string</param>
    /// <param name="RawData">string[]</param>
    void UnitySerialPort_SerialDataParseEvent(string[] Data, string RawData)
    {
        // Not fired via portStatus to avoid hiding other messages from the GUI
        if (ShowDebugs)
            print("Data Recieved via port: " + RawData);
    }

    /// <summary>
    /// Open serialport notification event
    /// </summary>
    void UnitySerialPort_SerialPortOpenEvent()
    {
        portStatus = "The serialport:" + ComPort + " is now open!";

        if (ShowDebugs)
            ShowDebugMessages(portStatus);
    }

    /// <summary>
    /// Close serialport notification event
    /// </summary>
    void UnitySerialPort_SerialPortCloseEvent()
    {
        portStatus = "The serialport:" + ComPort + " is now closed!";

        if (ShowDebugs)
            ShowDebugMessages(portStatus);
    }

    /// <summary>
    /// Send data serialport notification event
    /// </summary>
    /// <param name="Data">string</param>
    void UnitySerialPort_SerialPortSentDataEvent(string Data)
    {
        portStatus = "Sent data: " + Data;

        if (ShowDebugs)
            ShowDebugMessages(portStatus);
    }

    /// <summary>
    /// Send data with "\n" serialport notification event
    /// </summary>
    /// <param name="Data">string</param>
    void UnitySerialPort_SerialPortSentLineDataEvent(string Data)
    {
        portStatus = "Sent data as line: " + Data;

        if (ShowDebugs)
            ShowDebugMessages(portStatus);
    }

    #endregion Notification Events
    
    private bool isRunning = false;
    // Thread for thread version of port
    Thread SerialLoopThread;
    void StartSerialThread()
    {
        isRunning = true;

        SerialLoopThread = new Thread(SerialThreadLoop);
        SerialLoopThread.Start();
    }

    void SerialThreadLoop()
    {
        while (isRunning)
        {
            if (isRunning == false)
                break;

            // Run the generic loop
            GenericSerialLoop();
        }

        portStatus = "Ending Serial Thread!";

        if (ShowDebugs)
            ShowDebugMessages(portStatus);
    }

    private void GenericSerialLoop()
    {
        try
        {
            // Check that the port is open. If not skip and do nothing
            if (SerialPort.IsOpen)
            {
                // Read serial data until...

                string rData = string.Empty;

                // swap between the ReadLine or ReadTo
                rData = SerialPort.ReadLine();

            }
        }
        catch (TimeoutException)
        {
            // This will be triggered lots with the coroutine method
        }
        catch (Exception ex)
        {
            // This could be thrown if we close the port whilst the thread 
            // is reading data. So check if this is the case!
            if (SerialPort.IsOpen)
            {
                // Something has gone wrong!
                Debug.Log("Error 4: " + ex.Message.ToString());
                CloseSerialPort();
            }
            else
            {
                // Error caused by closing the port whilst in use! This is 
                // not really an error but uncomment if you wish.

                Debug.Log("Error 5: Port Closed Exception!");
            }
        }
    }
}

