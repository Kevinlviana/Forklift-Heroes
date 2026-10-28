using UnityEngine;

public static class SectionInfo
{
    public class Info
    {
        public string key, display, prefix;
        public Color color;
        public ProductShape[] shapes;
    }

    public static readonly Info[] All =
    {
        new Info { key = "Fruits",  display = "Produce", prefix = "F", color = new Color(0.30f, 0.75f, 0.30f),
                   shapes = new[] { ProductShape.Apple, ProductShape.Banana, ProductShape.Orange, ProductShape.Mango, ProductShape.Watermelon } },
        new Info { key = "Dairy",   display = "Dairy", prefix = "L", color = new Color(0.35f, 0.65f, 0.95f),
                   shapes = new[] { ProductShape.Milk, ProductShape.Yogurt, ProductShape.Eggs, ProductShape.Cheese } },
        new Info { key = "Drinks",  display = "Drinks",    prefix = "B", color = new Color(0.10f, 0.70f, 0.75f),
                   shapes = new[] { ProductShape.Can, ProductShape.Bottle, ProductShape.JuiceBox } },
        new Info { key = "Butcher", display = "Butcher",    prefix = "A", color = new Color(0.80f, 0.20f, 0.25f),
                   shapes = new[] { ProductShape.Steak, ProductShape.Sausage, ProductShape.Ham } },
        new Info { key = "Wine",    display = "Wine Cellar",      prefix = "W", color = new Color(0.55f, 0.15f, 0.40f),
                   shapes = new[] { ProductShape.WineBottle, ProductShape.Champagne, ProductShape.Barrel } },
    };

    public static Info Get(string key)
    {
        foreach (var i in All) if (i.key == key) return i;
        return null;
    }

    public static string Display(string key)
    {
        var i = Get(key);
        return i != null ? i.display : key;
    }

    public static Color ColorOf(string key)
    {
        var i = Get(key);
        return i != null ? i.color : Color.white;
    }
}

public class SectionZone
{
    public string section;
    public Bounds bounds;
}
