using System.Xml.Linq;

public class CVRParse {
    // Find first child element by name and return, or null
    public static XElement? FindElement(XElement cvrElement, XNamespace ns, string elemName)
    {
        IEnumerable<XElement> batchSequences = from elem in cvrElement.DescendantsAndSelf(ns + elemName) select elem;
        foreach (XElement elem in batchSequences) return elem;
        return null;
    }

    public static XElement? CallFound(StatUpdateElement callback, XElement? item)
    {
        if (item != null) callback(item);
        return item;
    }
    // Call callback if item is not null, return item for additional processing
    public static XElement? CallFound_Void(StatUpdate callback, XElement? item)
    {
        if (item != null) callback();
        return item;
    }
    // Call callback with expected int value, return item for additional processing
    public static XElement? CallFound_Int(StatUpdateInt callback, XElement? item)
    {
        if (item != null) callback(Convert.ToInt32(item.Value));
        return item;
    }
    // Call callback with string value, return item for additional processing
    public static XElement? CallFound_String(StatUpdateString callback, XElement? item)
    {
        if (item != null) callback(item.Value);
        return item;
    }
    
    // Call callback with column name and string value, return item for additional processing
    public static XElement? CallFound_KeyValue(StatUpdateKeyValue callback, XElement? item)
    {
        if (item != null) {
            callback(item.Name.LocalName, item.Value);
        }
        return item;
    }
    public static XElement? CallFound_If(PassElement callback, XElement? item)
    {
        if (item != null) return callback(item);
        return item;
    }
    // Process data for a party stats object's on items - i.e. count stats for subset of CVRs by party
    // Call callback with the matching stats object if the item is not null, return item for additional processing
    public static XElement? CallOnPartyStats(StatUpdateStats callback, Dictionary<string, CVRStats> allPartyStats, string partyNameKey, XElement? item)
    {
        if (item != null && allPartyStats.ContainsKey(partyNameKey)) callback(allPartyStats[partyNameKey]);
        return item;
    }
    // Call callback with the matching stats object and int value if the item is not null, return item for additional processing
    public static XElement? CallOnPartyStatsInt(StatUpdateStatsInt callback, Dictionary<string, CVRStats> allPartyStats, string partyNameKey, XElement? item)
    {
        if (item != null && allPartyStats.ContainsKey(partyNameKey)) callback(allPartyStats[partyNameKey], Convert.ToInt32(item.Value));
        return item;
    }
    // Call callback with the matching stats object and string value if the item is not null, return item for additional processing
    public static XElement? CallOnPartyStatsString(StatUpdateStatsString callback, Dictionary<string, CVRStats> allPartyStats, string partyNameKey, XElement? item)
    {
        if (item != null && allPartyStats.ContainsKey(partyNameKey)) callback(allPartyStats[partyNameKey], item.Value);
        return item;
    }
    // empty stat update delegate
    public static void DoNothing() {}
    public delegate void StatUpdate();
    public delegate void StatUpdateElement(XElement? item);
    public delegate void StatUpdateInt(int value);
    public delegate void StatUpdateString(string value);
    public delegate void StatUpdateStats(CVRStats stats);
    public delegate void StatUpdateStatsInt(CVRStats stats, int value);
    public delegate void StatUpdateStatsString(CVRStats stats, string value);
    public delegate void StatUpdateKeyValue(string key, string value);
    public delegate XElement? PassElement(XElement? item);
    
}