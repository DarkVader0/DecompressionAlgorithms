namespace Buhlmann.Zhl16c.Enums;

[Flags]
public enum CylinderUse : byte
{
    None = 0,
    Bottom = 1,
    Deco = 2,
    Diluent = 4,
    Bailout = 8
}