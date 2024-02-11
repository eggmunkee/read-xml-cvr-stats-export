using System.Diagnostics;
using System.Xml.Linq;

public enum CVRStructureType {
    SingleCVRFile,
    CastVoteRecordReport,
}

public class ParseStructureType
{
    public static void ProcessSingleCVRFile(CVRStructureType structureType, CVRStats stats, Dictionary<string, CVRStats> partyStats, 
        XElement cvrRoot, XNamespace ns, StreamWriter? csvWriter, DateTime? createDate, DateTime? modifyDate, int fileCount)
    {
        string recordPartyKey = "";
        CVRRow cvrRow = new CVRRow();
        // Apply the file dates to the row columns (because one file per cvr makes the date cvr specific)
        if (createDate != null) cvrRow.SetColumnValue("CreateDate", createDate.Value.ToString("yyyy-MM-dd HH:mm:ss"));
        if (modifyDate != null) cvrRow.SetColumnValue("ModifyDate", modifyDate.Value.ToString("yyyy-MM-dd HH:mm:ss"));
        CVRParse.CallIfHasStringValue(
            (partyNameKey) => {
                if (partyStats.ContainsKey(partyNameKey)) {
                    partyStats[partyNameKey].TotalCount++;
                } else {
                    partyStats[partyNameKey] = new CVRStats(partyNameKey);
                    partyStats[partyNameKey].TotalCount++;
                }
                recordPartyKey = partyNameKey;
            },
            CVRParse.CallIfHasValue(stats.IncrementParty, CVRParse.FindElement(cvrRoot, ns, "Party"))
        );
        CVRParse.CallOnPartyStats((s) => { s.IncrementGuids(); }, partyStats, recordPartyKey, 
            CVRParse.CallIfHasStringValueRow(cvrRow.SetColumnValue,  
                CVRParse.CallIfHasValue(stats.IncrementGuids, CVRParse.FindElement(cvrRoot, ns, "CvrGuid"))));
        CVRParse.CallOnPartyStats((s) => { s.IncrementBatchSequences(); }, partyStats, recordPartyKey,
            CVRParse.CallIfHasStringValueRow(cvrRow.SetColumnValue,
                CVRParse.CallIfHasValue(stats.IncrementBatchSequences, CVRParse.FindElement(cvrRoot, ns, "BatchSequence"))));
        CVRParse.CallOnPartyStatsInt((s, value) => { s.CheckSheetNumber(value); s.IncrementSheetNumbers(); }, partyStats,  recordPartyKey,
            CVRParse.CallIfHasStringValueRow(cvrRow.SetColumnValue,
                CVRParse.CallIfHasIntValue(stats.CheckSheetNumber, 
                    CVRParse.CallIfHasValue(stats.IncrementSheetNumbers, CVRParse.FindElement(cvrRoot, ns, "SheetNumber")))));
        CVRParse.CallOnPartyStats((s) => { s.IncrementBatchNumbers(); }, partyStats,  recordPartyKey,
            CVRParse.CallIfHasStringValueRow(cvrRow.SetColumnValue,
                CVRParse.CallIfHasValue(stats.IncrementBatchNumbers, CVRParse.FindElement(cvrRoot, ns, "BatchNumber"))));
        CVRParse.CallOnPartyStats((s) => { s.IncrementContests(); }, partyStats,  recordPartyKey,
            CVRParse.CallIfHasValue(stats.IncrementContests, CVRParse.FindElement(cvrRoot, ns, "Contests")));
        CVRParse.CallOnPartyStats((s) => { s.IncrementPrecinctSplit(); }, partyStats,  recordPartyKey,
            CVRParse.CallIfHasValue(stats.IncrementPrecinctSplit, CVRParse.FindElement(cvrRoot, ns, "PrecinctSplit")));

        // Add to overall stats total CVR count
        stats.TotalCount++;

        if (fileCount % 313 == 0) // Print every 73rd file as csv from CVRRow
        {
            Console.WriteLine(cvrRow.FormatCSVRow());
        }
        // write row to csv file
        if (csvWriter != null) csvWriter.WriteLine(cvrRow.FormatCSVRow());
    }

    public static void ProcessCVRReportFile(CVRStructureType structureType, CVRStats stats, Dictionary<string, CVRStats> partyStats, 
        XElement reportRoot, XNamespace ns, StreamWriter? csvWriter, DateTime createDate, DateTime modifyDate, int fileCount)
    {
        // go through each CVR element under the root
        foreach (XElement cvrRoot in reportRoot.Elements(ns + "CVR"))
        {
            ProcessSingleCVRFile(structureType, stats, partyStats, cvrRoot, ns, csvWriter, null, null, fileCount);
        }

    }
}