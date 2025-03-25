using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

public class PageConfig
{
    public List<PageItem> Pages { get; set; } = new();
    public List<SettingConfig> Settings { get; set; } = new();
}

public class PageDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("controls")]
    public List<ControlConfig> Controls { get; set; }
}

public class ControlConfig
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    [JsonPropertyName("dataSize")]
    public string? DataSize { get; set; }
    [JsonPropertyName("dataNum")]
    public string? DataNum { get; set; }
    [JsonPropertyName("dataPacket")]
    public string? DataPacket { get; set; }
    [JsonPropertyName("dataMask")]
    public string? DataMask { get; set; }
    [JsonPropertyName("dataDivider")]
    public string? DataDivider { get; set; }
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("backgroundColor")]
    public string? BackgroundColor { get; set; }
    [JsonPropertyName("horizontalAlignment")]
    public string? HorizontalAlignment { get; set; }
    [JsonPropertyName("verticalAlignment")]
    public string? VerticalAlignment { get; set; }
    [JsonPropertyName("fontSize")]
    public int? FontSize { get; set; }
    [JsonPropertyName("width")]
    public int? Width { get; set; }
    [JsonPropertyName("height")]
    public int? Height { get; set; }
    [JsonPropertyName("x")]
    public int? X { get; set; }
    [JsonPropertyName("y")]
    public int? Y { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("margin")]
    public ObjectArea? Margin { get; set; }
    [JsonPropertyName("padding")]
    public ObjectArea? Padding { get; set; }

    [JsonPropertyName("action")]
    public string? Action { get; set; }
    //public PositionConfig Position { get; set; }  // Добавляем поддержку position
}

public class ObjectArea
{
    [JsonPropertyName("left")]
    public int? Left { get; set; }
    [JsonPropertyName("top")]
    public int? Top { get; set; }
    [JsonPropertyName("right")]
    public int? Right { get; set; }
    [JsonPropertyName("bottom")]
    public int? Bottom { get; set; }
}

public class PositionConfig
{
    public int X { get; set; }
    public int Y { get; set; }
}

public class PageItem
{
    public string? Name { get; set; }
    public List<ControlConfig> Controls { get; set; } = new();
}

public class SettingConfig
{
    [JsonPropertyName("key")]
    public string? Key { get; set; }
    [JsonPropertyName("value")]
    public string? Value { get; set; }
}