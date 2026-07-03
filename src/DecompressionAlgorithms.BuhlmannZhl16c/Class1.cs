// var options = new Options(Circuit, Algorithm, AlgorithmSettings[GfHigh, GfLow], 
// WaterType, DepthUnit, GasVolumeUnit, BottomGasSac, DecoGasSac, CCRSetpointLow, CCRSetpointHigh,
// DecoStepSize, LastStop, StopTimeMinimum,
// , DescentSpeed, AscentSpeed75, AscentSpeed50,
// AscentSpeed6, AscentSpeedSurface,
// O2Narcotic, 
// ppo2High, ppo2Low, TimeToGasSwitch Elevation)
// var trip = new Trip(options)
// var diveWaypoints = [(Depth, Time, Gas), (Depth, Time, Gas), ...]
// var cylinders = [(Gas, Size, StartPressure), (Gas, Size, StartPressure), ...]
// var plan = trip.PlanDive(diveWaypoints, cylinders)

// 2 libraries, DecompressionAlgorithms.Core and DecompressionAlgorithms.Buhlmann.Zhl16c
// In library DecompressionAlgorithms.Buhlmann.Zhl16c I want logic to calculate decompression only
// In Core library I want to have logic to see a trip (multiple consecutive dives with surface intervals), I want to be able to set up different algorithm based on users wishes
// I want to configure algorithm correctly and I want to be able to abstract mm/mbar logic from the user, he knows he will put in feet/meters or any other unit and that
// I will translate it and when i return I will return him his unit
// Core will be layer between user and algorithm. I will add more algorithms in the future so user will have no issue to easily switch and adopt. 
