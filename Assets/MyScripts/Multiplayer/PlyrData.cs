using System;

public struct PlyrData : IEquatable<PlyrData>
{
    public ulong clientId;

    public bool Equals(PlyrData other)
    {
       return clientId==other.clientId;
    }
}

