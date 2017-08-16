using System.Collections;
using System.Collections.Generic;
//using System;
using UnityEngine;

public class OSCReceiver : MonoBehaviour
{

    [HideInInspector]
    public string RemoteIP = "127.0.0.1"; //127.0.0.1 signifies a local host 
    [HideInInspector]
    public int SendToPort = 9000; //the port you will be sending from
    public int ListenerPort = 8050; //the port you will be listening on

    private Osc handler;
    private UDPPacketIO udp;

    public Dictionary<int, Vector4> positions;
    public Dictionary<int, GameObject> primitives;
    public float timeoutTime = 1f;

    [HideInInspector]
    public Vector3 mainsource;

    // Use this for initialization
    void Start()
    {
        positions = new Dictionary<int, Vector4>();
        primitives = new Dictionary<int, GameObject>();
        udp = new UDPPacketIO();
        udp.init(RemoteIP, SendToPort, ListenerPort);
        handler = new Osc();
        handler.init(udp);
        handler.SetAllMessageHandler(AllMessageHandler);
        Debug.Log("OSC Connection initialized");
        mainsource = new Vector3(32, 32, 32);
    }

    // Update is called once per frame
    void Update()
    {
        List<int> positionKeys = new List<int>(positions.Keys);


        foreach (var key in positionKeys)
        {
            positions[key] += new Vector4(0, 0, 0, Time.deltaTime);
            if (positions[key].w >= timeoutTime)
            {
                GameObject.Destroy(primitives[key]);
                primitives.Remove(key);
                positions.Remove(key);
            }
            else
            {
                if (primitives.ContainsKey(key))
                {
                    primitives[key].transform.position = Vector3.Lerp(primitives[key].transform.position, positions[key], Time.deltaTime);

                    primitives[key].transform.localScale = Vector3.zero; //Vector3.one * (2 - positions[key].w);
                    if(key == 0)
                    {
                        mainsource = primitives[key].transform.position;
                    }
                }
                else
                {
                    primitives[key] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    primitives[key].GetComponent<Renderer>().material.color = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
                    primitives[key].transform.position = positions[key];
                }
            }
        }
    }

    void OnDisable()
    {

        udp.Close();

    }

    public void AllMessageHandler(OscMessage oscMessage)
    {
        string msgString = Osc.OscMessageToString(oscMessage); //the message and value combined
        string msgAddress = oscMessage.Address; //the message address
        int contourNumber = int.Parse(msgAddress.Split('/')[2]);
        string value = oscMessage.Values[0].ToString();

        string dimension = msgAddress.Split('/')[3].ToString();

        if (positions.ContainsKey(contourNumber))
        {
            Vector4 newPosition = SetVectorCoordinate(dimension, value, positions[contourNumber]);
            newPosition.w = 0;
            positions[contourNumber] = newPosition;
        }
        else
        {
            positions.Add(contourNumber, Vector4.zero);
            Vector4 newPosition = SetVectorCoordinate(dimension, value, positions[contourNumber]);
            newPosition.w = 0;
            positions[contourNumber] = newPosition;

        }
    }

    private Vector4 SetVectorCoordinate(string dimension, string value, Vector4 input)
    {
        switch (dimension)
        {
            case "x":
                return new Vector4((float.Parse(value) / 640f * 64), input.y, input.z, input.w);
            case "y":
                return new Vector4(input.x, 64-(float.Parse(value) / 480f * 64), input.z, input.w);
            case "z":
                return new Vector4(input.x, input.y, (float.Parse(value) / 255f) * 64, input.w);
            default:
                return new Vector4(input.x, input.y, input.z, input.w);
        }
    }
}
