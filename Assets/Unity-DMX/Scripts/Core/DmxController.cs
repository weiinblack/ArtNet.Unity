using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using UnityEngine;

using ArtNet.Sockets;
using ArtNet.Packets;

public class DmxController : MonoBehaviour
{
    [Header("send dmx")]
    public bool useBroadcast;
    public string remoteIP = "localhost";
    IPEndPoint remote;

    [Header("dmx devices")]
    public UniverseDevices[] universes;

    [Header("artNet data")]
    ArtNetSocket artnet;
    [SerializeField] ArtNetDmxPacket latestDMX;
    [SerializeField] ArtNetDmxPacket dmxToSend;
    byte[] _dmxData;

    Dictionary<int, byte[]> dmxDataMap;

    public void Send(short universe, byte[] dmxData)
    {
        dmxToSend.Universe = universe;
        dmxToSend.DmxData = dmxData;

        if (useBroadcast)
            artnet.Send(dmxToSend);
        else
            artnet.Send(dmxToSend, remote);
    }

    private void OnValidate()
    {
        foreach (var u in universes)
            u.Initialize();
    }

    void Start()
    {
        var artnet = new ArtNetSocket();
        artnet.Open(IPAddress.Parse("127.0.0.1"), IPAddress.Parse("255.255.255.0"));

        artnet.NewPacket += (object sender, NewPacketEventArgs<ArtNetPacket> e) =>
        {
            if (e.Packet.OpCode == ArtNet.Enums.ArtNetOpCodes.Dmx)
            {
                var packet = latestDMX = e.Packet as ArtNetDmxPacket;

                if (packet.DmxData != _dmxData)
                    _dmxData = packet.DmxData;

                var universe = packet.Universe;
                if (dmxDataMap.ContainsKey(universe))
                    dmxDataMap[universe] = packet.DmxData;
                else
                    dmxDataMap.Add(universe, packet.DmxData);
            }
        };

        if (!useBroadcast)
            remote = new IPEndPoint(FindFromHostName(remoteIP), 6454);

        dmxDataMap = new Dictionary<int, byte[]>();
    }

    private void Update()
    {
        var keys = dmxDataMap.Keys.ToArray();

        for (var i = 0; i < keys.Length; i++)
        {
            var universe = keys[i];
            var dmxData = dmxDataMap[universe];
            if (dmxData == null)
                continue;

            var universeDevices = universes.Where(u => u.universe == universe).FirstOrDefault();
            if (universeDevices != null)
                foreach (var d in universeDevices.devices)
                    d.SetData(dmxData.Skip(d.startChannel).Take(d.NumChannels).ToArray());

            dmxDataMap[universe] = null;
        }
    }

    static IPAddress FindFromHostName(string hostname)
    {
        var address = IPAddress.None;
        try
        {
            if (IPAddress.TryParse(hostname, out address))
                return address;

            var addresses = Dns.GetHostAddresses(hostname);
            for (var i = 0; i < addresses.Length; i++)
            {
                if (addresses[i].AddressFamily == AddressFamily.InterNetwork)
                {
                    address = addresses[i];
                    break;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogErrorFormat(
                "Failed to find IP for :\n host name = {0}\n exception={1}",
                hostname, e);
        }
        return address;
    }

    [System.Serializable]
    public class UniverseDevices
    {
        public string universeName;
        public int universe;
        public DMXDevice[] devices;

        public void Initialize()
        {
            var startChannel = 0;
            foreach (var d in devices)
                if (d != null)
                {
                    d.startChannel = startChannel;
                    startChannel += d.NumChannels;
                }
            if (512 < startChannel)
                Debug.LogErrorFormat("The number({0}) of channels of the universe {1} exceeds the upper limit(512 channels)!", startChannel, universe);
        }
    }
}
