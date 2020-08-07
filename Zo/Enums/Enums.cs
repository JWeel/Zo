using Zo.Attributes;

namespace Zo.Enums
{
    public enum MouseButton
    {
        None = 0,
        Left = 1,
        Middle = 2,
        Right = 3,
        Four = 4,
        Five = 5
    }

    public enum Division
    {
        Barony = 0,
        County = 1,
        Geesing = 2,
        Duchy = 3,
        Herning = 4,
        Kingdom = 5,
        Empire = 6,
    }

    public enum MapType
    {
        Political,
        Natural,
        Geographical,
    }

    public enum InputSource
    {
        None,
        FiefName
    }
}