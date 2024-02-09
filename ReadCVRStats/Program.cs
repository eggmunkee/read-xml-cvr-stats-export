using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

// create stats collection
CVRStats stats = new CVRStats();
Dictionary<string, CVRStats> partyStats = new Dictionary<string, CVRStats>();

int fileCount = 0;
// set folder path of cvrs
string[] cvrsPaths = new string[] { 
    // "/home/ user name /Documents/folder path" for linux
    // "C:\\Users\\ user name \\Documents\\folder path" for windows
};

// If command arguments passed, use first as folder path and second as CSV output file path
string csvOutputPath = "";
if (args.Length > 0) 
{
    cvrsPaths = new string[] { args[0] }; // set CVR folder path
    if (args.Length > 1) 
    {
        Console.WriteLine($"Writing to file {args[1]}");
        csvOutputPath = args[1];
    }
}

Console.WriteLine(CVRRow.FormatCSVHeader());

StreamWriter? csvWriter = null;
if (csvOutputPath != "")
{
    csvWriter = new StreamWriter(csvOutputPath);
    csvWriter.WriteLine(CVRRow.FormatCSVHeader());
}

// go through all folders
foreach (string cvrsPath in cvrsPaths)
{
    // get folder
    DirectoryInfo diCVRs = new DirectoryInfo(cvrsPath);
    // process each file
    foreach (FileInfo? file in diCVRs.EnumerateFiles())
    {
        if (file.Extension.ToLower() == ".xml") // only process xml files
        {
            DateTime createDate = file.CreationTime;
            DateTime modifyDate = file.LastWriteTime;
            // if (fileCount % 73 == 0) // Print every 73rd file
            // {
            //     Console.WriteLine($"Processing file {file.Name} created {createDate} modified {modifyDate}");
            // }
            XElement cvrRoot = XElement.Load(file.FullName);
            XNamespace ns = cvrRoot.GetDefaultNamespace();
            string recordPartyKey = "";
            CVRRow cvrRow = new CVRRow();
            cvrRow.SetColumnValue("CreateDate", createDate.ToString("yyyy-MM-dd HH:mm:ss"));
            cvrRow.SetColumnValue("ModifyDate", modifyDate.ToString("yyyy-MM-dd HH:mm:ss"));
            CallIfHasStringValue(
                (partyNameKey) => {
                    if (partyStats.ContainsKey(partyNameKey)) {
                        partyStats[partyNameKey].TotalCount++;
                    } else {
                        partyStats[partyNameKey] = new CVRStats(partyNameKey);
                        partyStats[partyNameKey].TotalCount++;
                    }
                    recordPartyKey = partyNameKey;
                },
                CallIfHasValue(stats.IncrementParty, FindElement(cvrRoot, ns, "Party"))
            );
            CallOnPartyStats((s) => { s.IncrementGuids(); }, partyStats, recordPartyKey, 
                CallIfHasStringValueRow(cvrRow.SetColumnValue,  
                    CallIfHasValue(stats.IncrementGuids, FindElement(cvrRoot, ns, "CvrGuid"))));
            CallOnPartyStats((s) => { s.IncrementBatchSequences(); }, partyStats, recordPartyKey,
                CallIfHasStringValueRow(cvrRow.SetColumnValue,
                    CallIfHasValue(stats.IncrementBatchSequences, FindElement(cvrRoot, ns, "BatchSequence"))));
            CallOnPartyStatsInt((s, value) => { s.CheckSheetNumber(value); s.IncrementSheetNumbers(); }, partyStats,  recordPartyKey,
                CallIfHasStringValueRow(cvrRow.SetColumnValue,
                    CallIfHasIntValue(stats.CheckSheetNumber, 
                        CallIfHasValue(stats.IncrementSheetNumbers, FindElement(cvrRoot, ns, "SheetNumber")))));
            CallOnPartyStats((s) => { s.IncrementBatchNumbers(); }, partyStats,  recordPartyKey,
                CallIfHasStringValueRow(cvrRow.SetColumnValue,
                    CallIfHasValue(stats.IncrementBatchNumbers, FindElement(cvrRoot, ns, "BatchNumber"))));
            CallOnPartyStats((s) => { s.IncrementContests(); }, partyStats,  recordPartyKey,
                CallIfHasValue(stats.IncrementContests, FindElement(cvrRoot, ns, "Contests")));
            CallOnPartyStats((s) => { s.IncrementPrecinctSplit(); }, partyStats,  recordPartyKey,
                CallIfHasValue(stats.IncrementPrecinctSplit, FindElement(cvrRoot, ns, "PrecinctSplit")));
            fileCount++;
            // Add to overall stats total CVR count
            stats.TotalCount++;

            if (fileCount % 313 == 0) // Print every 73rd file as csv from CVRRow
            {
                Console.WriteLine(cvrRow.FormatCSVRow());
            }
            // write row to csv file
            if (csvWriter != null) csvWriter.WriteLine(cvrRow.FormatCSVRow());
            
        }
        //if (fileCount > 2100) break;
    }
}

// if there is a csv writer, close it
if (csvWriter != null) csvWriter.Close();

Console.WriteLine("");
Console.WriteLine($"Total CVRs processed: {stats.TotalCount:0######}");
Console.WriteLine($"  With BatchSequence  {stats.TotalCVRsWithBatchSequence:0######}");
Console.WriteLine($"  With BatchNumber    {stats.TotalCVRsWithBatchNumber:0######}");
Console.WriteLine($"  With SheetNumber    {stats.TotalCVRsWithSheetNumber:0######}");
Console.WriteLine($"  With CvrGuid:       {stats.TotalCVRsWithGuid:0######}");
Console.WriteLine($"  With Contests:      {stats.TotalCVRsWithContests:0######}");
Console.WriteLine($"  With PrecinctSplit: {stats.TotalCVRsWithPrecinctSplit:0######}");
Console.WriteLine($"  With Party:         {stats.TotalCVRsWithParty:0######}");
Console.WriteLine($"  Min SheetNumber:    {stats.MinSheetNumber:0######}");
Console.WriteLine($"  Max SheetNumber:    {stats.MaxSheetNumber:0######}");
// output any party stats
foreach (KeyValuePair<string, CVRStats> partyStat in partyStats)
{
    Console.WriteLine($"  Party: {partyStat.Key} - - - - - - - - - - -");
    Console.WriteLine($"    Total CVRs: {partyStat.Value.TotalCount:0######}");
    Console.WriteLine($"      With BatchSequence  {partyStat.Value.TotalCVRsWithBatchSequence:0######}");
    Console.WriteLine($"      With BatchNumber    {partyStat.Value.TotalCVRsWithBatchNumber:0######}");
    Console.WriteLine($"      With SheetNumber    {partyStat.Value.TotalCVRsWithSheetNumber:0######}");
    Console.WriteLine($"      With CvrGuid:       {partyStat.Value.TotalCVRsWithGuid:0######}");
    Console.WriteLine($"      With Contests:      {partyStat.Value.TotalCVRsWithContests:0######}");
    Console.WriteLine($"      With PrecinctSplit: {partyStat.Value.TotalCVRsWithPrecinctSplit:0######}");
    Console.WriteLine($"      With Party:         {partyStat.Value.TotalCVRsWithParty:0######}");
    Console.WriteLine($"      Min SheetNumber:    {partyStat.Value.MinSheetNumber:0######}");
    Console.WriteLine($"      Max SheetNumber:    {partyStat.Value.MaxSheetNumber:0######}");
}

XElement? FindElement(XElement cvrElement, XNamespace ns, string elemName)
{
    IEnumerable<XElement> batchSequences = from elem in cvrElement.Elements(ns + elemName) select elem;
    foreach (XElement elem in batchSequences) return elem;
    return null;
}
XElement? CallIfHasValue(StatUpdate callback, XElement? item)
{
    if (item != null) callback();
    return item;
}
XElement? CallIfHasIntValue(StatUpdateInt callback, XElement? item)
{
    if (item != null) callback(Convert.ToInt32(item.Value));
    return item;
}
XElement? CallIfHasStringValue(StatUpdateString callback, XElement? item)
{
    if (item != null) callback(item.Value);
    return item;
}
XElement? CallIfHasIntValueRow(StatUpdateRowString callback, XElement? item)
{
    if (item != null) {
        callback(item.Name.LocalName, item.Value);
    }
    return item;
}
XElement? CallIfHasStringValueRow(StatUpdateRowString callback, XElement? item)
{
    if (item != null) {
        callback(item.Name.LocalName, item.Value);
    }
    return item;
}
XElement? CallOnAllPartyStats(StatUpdateStats callback, Dictionary<string, CVRStats> allPartyStats, XElement? item)
{
    if (item != null) foreach (KeyValuePair<string, CVRStats> partyStat in allPartyStats) callback(partyStat.Value);
    return item;
}
XElement? CallOnAllPartyStatsInt(StatUpdateStatsInt callback, Dictionary<string, CVRStats> allPartyStats, XElement? item)
{
    if (item != null) foreach (KeyValuePair<string, CVRStats> partyStat in allPartyStats) callback(partyStat.Value, Convert.ToInt32(item.Value));
    return item;
}
XElement? CallOnAllPartyStatsString(StatUpdateStatsString callback, Dictionary<string, CVRStats> allPartyStats, XElement? item)
{
    if (item != null) foreach (KeyValuePair<string, CVRStats> partyStat in allPartyStats) callback(partyStat.Value, item.Value);
    return item;
}
XElement? CallOnPartyStats(StatUpdateStats callback, Dictionary<string, CVRStats> allPartyStats, string partyNameKey, XElement? item)
{
    if (item != null && allPartyStats.ContainsKey(partyNameKey)) callback(allPartyStats[partyNameKey]);
    return item;
}
XElement? CallOnPartyStatsInt(StatUpdateStatsInt callback, Dictionary<string, CVRStats> allPartyStats, string partyNameKey, XElement? item)
{
    if (item != null && allPartyStats.ContainsKey(partyNameKey)) callback(allPartyStats[partyNameKey], Convert.ToInt32(item.Value));
    return item;
}
void DoNothing() {}
delegate void StatUpdate();
delegate void StatUpdateInt(int value);
delegate void StatUpdateString(string value);
delegate void StatUpdateStats(CVRStats stats);
delegate void StatUpdateStatsInt(CVRStats stats, int value);
delegate void StatUpdateStatsString(CVRStats stats, string value);
delegate void StatUpdateRowString(string columnName, string value);
delegate void StatUpdateRowInt(string columnName, int value);
