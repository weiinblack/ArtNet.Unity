using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class DMXDevice : MonoBehaviour
{
    public byte[] dmxData;
    public int startChannel;
    public abstract int NumChannels { get; }

    public virtual void SetData(byte[] dmxData)
    {
        this.dmxData = dmxData;
    }
}
