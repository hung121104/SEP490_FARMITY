using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
/// Newtonsoft.Json converter that reads the "itemType" integer field from JSON
/// and instantiates the correct ItemData subclass.
///
/// This lets the catalog endpoint return heterogeneous items in one array
/// and have them deserialized to ToolData, SeedData, etc. automatically.
///
/// Usage:
///   var settings = new JsonSerializerSettings();
///   settings.Converters.Add(new ItemDataConverter());
///   var catalog = JsonConvert.DeserializeObject<ItemCatalogResponse>(json, settings);
/// </summary>
public class ItemDataConverter : JsonConverter<ItemData>
{
    public override ItemData ReadJson(JsonReader reader, Type objectType,
        ItemData existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var jObj = JObject.Load(reader);

        // Read the discriminator
        int typeInt = jObj["itemType"]?.Value<int>() ?? -1;
        var itemType = (ItemType)typeInt;

        // Instantiate correct subclass
        ItemData target = itemType switch
        {
            ItemType.Tool       => new ToolData(),
            ItemType.Seed       => new SeedData(),
            ItemType.Crop       => new CropData(),
            ItemType.Pollen     => new PollenData(),
            ItemType.Consumable => new ConsumableData(),
            ItemType.Weapon     => new WeaponData(),
            ItemType.Fish       => new FishData(),
            ItemType.Forage     => new ForageData(),
            ItemType.Resource   => new ResourceData(),
            ItemType.Material   => new MaterialData(),
            ItemType.Gift       => new GiftItemData(),
            ItemType.Quest      => new QuestItemData(),
            ItemType.Cooking    => new CookingData(),
            _                   => new ItemData()
        };

        serializer.Populate(jObj.CreateReader(), target);
        return target;
    }

    // We only override reading — writing uses default serialization
    public override void WriteJson(JsonWriter writer, ItemData value, JsonSerializer serializer)
        => serializer.Serialize(writer, value);

    public override bool CanWrite => false;
}
