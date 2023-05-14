using DamageLogger.Data.Enums;
using DamageLogger.Data.Enums.Friendly;
using Spectre.Console;

namespace DamageLogger.Core.Logging;

public static class Theme
{
    public class ThemeColors
    {
        public readonly Color On = Color.Green;
        public readonly Color Off = Color.Red;
        public readonly Color Avatar = Color.Aqua;
        public readonly Color Damage = Color.Yellow;
        public readonly Color Dps = Color.Red;
        public readonly Color Ratio = Color.Fuchsia;
        public readonly Color Source = Color.Blue;
        public readonly Color Count = Color.Lime;
    }

    public static readonly ThemeColors Colors = new();
    
    private static readonly Dictionary<FriendlyElementType, Color> Element2ColorMap = new()
    {
        { FriendlyElementType.Physical, Color.White },
        { FriendlyElementType.Pyro, Color.Red },
        { FriendlyElementType.Hydro, Color.Navy },
        { FriendlyElementType.Dendro, Color.Green },
        { FriendlyElementType.Electro, Color.Purple },
        { FriendlyElementType.Cryo, Color.Blue },
        { FriendlyElementType.Anemo, Color.Aqua },
        { FriendlyElementType.Geo, Color.Yellow },
    };
    
    private static readonly List<Color> ColorList = new()
    {
        Color.Maroon, Color.Green, Color.Olive, Color.Navy, Color.Purple, Color.Teal
    };
    
    private static int _colorIndex = 0;

    public static Color GetNextColor()
    {
        if (_colorIndex >= ColorList.Count)
            _colorIndex = 0;
        return ColorList[_colorIndex++];
    }

    public static Color GetColorFromElement(FriendlyElementType elementType)
    {
        return Element2ColorMap.TryGetValue(elementType, out var color) ? color : Color.Default;
    }

    public static Color GetColorFromElement(ElementType elementType)
    {
        return GetColorFromElement((FriendlyElementType)elementType);
    }
}