using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;

public enum CVRStructureType {
    SingleCVRFile,
    CastVoteRecordReport,
}

public class ParseStructureType
{
    private static Regex reFirstNumber = new Regex(@"^\d+");
    public static CVRRowBase GetOutputRow(CVRStructureType structureType)
    {
        if (structureType == CVRStructureType.SingleCVRFile)
        {
            return new SingleFileCVRRow();
        }
        else if (structureType == CVRStructureType.CastVoteRecordReport)
        {
            return new CVRReportRow();
        }
        else
        {
            throw new NotImplementedException();
        };
    }
    public static void ProcessCVRElement(CVRStructureType structureType, CVRStats stats, Dictionary<string, CVRStats> partyStats, 
        XElement cvrRoot, XNamespace ns, StreamWriter? csvWriter, DateTime? createDate, DateTime? modifyDate, int fileCount)
    {
        string recordPartyKey = "";
        CVRRowBase cvrRow = GetOutputRow(structureType);
        // PARTY ELEMENT
        CVRParse.CallFound_String( // Create or find and increment the party stat total count
            (partyNameKey) => {
                if (partyStats.ContainsKey(partyNameKey)) {
                    partyStats[partyNameKey].TotalCount++;
                } else {
                    partyStats[partyNameKey] = new CVRStats(partyNameKey);
                    partyStats[partyNameKey].TotalCount++;
                }
                recordPartyKey = partyNameKey;
            },
            CVRParse.CallFound_Void(stats.IncrementParty, // Increment found party count
                CVRParse.FindElement(cvrRoot, ns, "Party")) // Find Party elements
        );
        // Apply the file dates to the row columns (because one file per cvr makes the date cvr specific)
        if (createDate != null) cvrRow.SetColumnValue("CreateDate", createDate.Value.ToString("yyyy-MM-dd HH:mm:ss"));
        if (modifyDate != null) cvrRow.SetColumnValue("ModifyDate", modifyDate.Value.ToString("yyyy-MM-dd HH:mm:ss"));
        
        stats.CheckModifyDate(modifyDate.Value);
        if (partyStats.ContainsKey(recordPartyKey)) partyStats[recordPartyKey].CheckModifyDate(modifyDate.Value);

        // CVR GUID ELEMENT
        CVRParse.CallOnPartyStats((s) => { s.IncrementGuids(); }, partyStats, recordPartyKey, 
            CVRParse.CallFound_KeyValue(cvrRow.SetColumnValue,  
                CVRParse.CallFound_Void(stats.IncrementGuids, CVRParse.FindElement(cvrRoot, ns, "CvrGuid"))));
        // BATCH SEQUENCE ELEMENT
        CVRParse.CallOnPartyStats((s) => { s.IncrementBatchSequences(); }, partyStats, recordPartyKey,
            CVRParse.CallFound_KeyValue(cvrRow.SetColumnValue,
                CVRParse.CallFound_Void(stats.IncrementBatchSequences, CVRParse.FindElement(cvrRoot, ns, "BatchSequence"))));
        // SHEET NUMBER ELEMENT
        CVRParse.CallOnPartyStatsInt((s, value) => { s.CheckSheetNumber(value); s.IncrementSheetNumbers(); }, partyStats,  recordPartyKey,
            CVRParse.CallFound_KeyValue(cvrRow.SetColumnValue,
                CVRParse.CallFound_Int(stats.CheckSheetNumber, 
                    CVRParse.CallFound_Void(stats.IncrementSheetNumbers, CVRParse.FindElement(cvrRoot, ns, "SheetNumber")))));
        // BATCH NUMBER ELEMENT
        CVRParse.CallOnPartyStats((s) => { s.IncrementBatchNumbers(); }, partyStats,  recordPartyKey,
            CVRParse.CallFound_KeyValue(cvrRow.SetColumnValue,
                CVRParse.CallFound_Void(stats.IncrementBatchNumbers, CVRParse.FindElement(cvrRoot, ns, "BatchNumber"))));
        // CONTESTS ELEMENT
        CVRParse.CallOnPartyStats((s) => { s.IncrementContests(); }, partyStats,  recordPartyKey,
            CVRParse.CallFound_Void(stats.IncrementContests, CVRParse.FindElement(cvrRoot, ns, "Contests")));
        // PRECINCT SPLIT ELEMENT
        CVRParse.CallOnPartyStats((s) => { s.IncrementPrecinctSplit(); }, partyStats,  recordPartyKey,
            CVRParse.CallFound_Void(stats.IncrementPrecinctSplit, CVRParse.FindElement(cvrRoot, ns, "PrecinctSplit")));

        // Add to overall stats total CVR count
        stats.TotalCount++;

        if (fileCount % 11313 == 0) // Print every 73rd file as csv from CVRRow
        {
            Console.WriteLine(cvrRow.FormatCSVRow());
        }
        // write row to csv file
        if (csvWriter != null) csvWriter.WriteLine(cvrRow.FormatCSVRow());
    }

    public static void ProcessCVRReportElement(CVRStructureType structureType, CVRStats stats, Dictionary<string, CVRStats> partyStats, 
        XElement reportRoot, XNamespace ns, StreamWriter? csvWriter, DateTime createDate, DateTime modifyDate, int fileCount)
    {
        // go through each CVR element under the root
        foreach (XElement cvrRoot in reportRoot.Elements(ns + "CVR"))
        {
            ProcessReportCVRElement(structureType, stats, partyStats, cvrRoot, ns, csvWriter, null, null, fileCount);
        }

    }

    public static void ProcessReportCVRElement(CVRStructureType structureType, CVRStats stats, Dictionary<string, CVRStats> partyStats, 
        XElement cvrRoot, XNamespace ns, StreamWriter? csvWriter, DateTime? createDate, DateTime? modifyDate, int fileCount)
    {
        string recordPartyKey = "";
        CVRRowBase cvrRow = GetOutputRow(structureType);
        
        // BALLOT IMAGE ELEMENT
        CVRParse.CallFound((item) => {// set column BallotImageId from Image[FileName] elem of BallotImage
                XAttribute? fileNameAttr = item.Attribute("FileName");
                if (fileNameAttr != null) {
                    Match match = reFirstNumber.Match(fileNameAttr.Value);
                    if (match.Success) cvrRow.SetColumnValue("BallotImageId", match.Value);
                    else Console.WriteLine($"No number found in: {fileNameAttr.Value}");
                }
                else Console.WriteLine($"No file attribute on: {item}");
            }, 
            CVRParse.CallFound_Void(stats.IncrementGuids, 
                CVRParse.CallFound_If((el) => CVRParse.FindElement(el, ns, "Image"), 
                    CVRParse.FindElement(cvrRoot, ns, "BallotImage"))));
        // CREATING DEVICE ELEMENT
        CVRParse.CallFound_KeyValue(cvrRow.SetColumnValue,
            CVRParse.FindElement(cvrRoot, ns, "CreatingDeviceId"));
        // BALLOT STYLE ELEMENT
        CVRParse.CallFound_KeyValue(cvrRow.SetColumnValue,
            CVRParse.FindElement(cvrRoot, ns, "BallotStyleId"));
        // Count PartyID elements under Party stat
        CVRParse.CallFound_Void(stats.IncrementParty, 
                CVRParse.FindElement(cvrRoot, ns, "PartyIds"));
        // ELECTION ID ELEMENT
        CVRParse.CallFound_KeyValue(cvrRow.SetColumnValue,
            CVRParse.FindElement(cvrRoot, ns, "ElectionId"));

        // Add to overall stats total CVR count
        stats.TotalCount++;

        if (fileCount % 13 == 0) // Print every 73rd file as csv from CVRRow
        {
            Console.WriteLine(cvrRow.FormatCSVRow());
        }
        // write row to csv file
        if (csvWriter != null) csvWriter.WriteLine(cvrRow.FormatCSVRow());
    }
}