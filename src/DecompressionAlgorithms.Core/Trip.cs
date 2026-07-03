using DecompressionAlgorithms.Core.Configurations;

namespace DecompressionAlgorithms.Core;

public struct Trip
{
    public Trip(AlgorithmType algorithmType,
        AlgorithmConfiguration algorithmConfiguration,
        DiveMode diveMode,
        CCRConfiguration ccrConfiguration,
        EnvironmentConfiguration environmentConfiguration,
        MovementConfiguration movementConfiguration,
        SacConfiguration sacConfiguration,
        StopConfiguration stopConfiguration)
    {
        throw new NotImplementedException();
    }

    public List<Dive> Dives { get; set; }
}

public struct Dive
{
    List<DiveWaypoint> DiveWaypoints { get; set; }
    List<Cylinder> Cylinders { get; set; }
}

public struct DiveWaypoint
{
    
}

public struct Cylinder
{
    
}