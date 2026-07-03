namespace DecompressionAlgorithms.Core.Units;

/// <summary>
/// This class represents the different pressure units available in the decompression algorithm. The pressure units are:
/// - Bar: A unit of pressure, commonly used in diving and other applications.
/// - Psi: Pounds per square inch, a unit of pressure commonly used in the United States and other countries that use the imperial system.
/// </summary>
public enum PressureUnit : byte
{
    Bar,
    Psi
}