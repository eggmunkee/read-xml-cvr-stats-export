using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

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
    public static void ProcessCVRElement(ProgramData programData, 
        XElement cvrRoot, XNamespace ns, StreamWriter? csvWriter, DateTime? createDate, DateTime? modifyDate)
    {
        string recordPartyKey = "";
        CVRRowBase cvrRow = GetOutputRow(programData.structureType);
        if (programData.contestColumnInfo != null) {
            cvrRow.AddContestInfo(programData.contestColumnInfo);
        }
        // PARTY ELEMENT
        CVRParse.CallFound_String( // Create or find and increment the party stat total count
            (partyNameKey) => {
                if (programData.partyStats.ContainsKey(partyNameKey)) {
                    programData.partyStats[partyNameKey].TotalCount++;
                } else {
                    programData.partyStats[partyNameKey] = new CVRStats(partyNameKey);
                    programData.partyStats[partyNameKey].TotalCount++;
                }
                recordPartyKey = partyNameKey;
            },
            CVRParse.CallFound_Void(programData.stats.IncrementParty, // Increment found party count
                CVRParse.CallFound_If((partyElem) => partyElem.Element(ns + "Name"), // Find Name element and pass along
                    CVRParse.FindElement(cvrRoot, ns, "Party"))) // Find Party elements
        );
        // Apply the file dates to the row columns (because one file per cvr makes the date cvr specific)
        if (createDate != null) cvrRow.SetColumnValue("CreateDate", createDate.Value.ToString("yyyy-MM-dd HH:mm:ss"));
        if (modifyDate != null) cvrRow.SetColumnValue("ModifyDate", modifyDate.Value.ToString("yyyy-MM-dd HH:mm:ss"));
        
        programData.stats.CheckModifyDate(modifyDate.Value);
        if (programData.partyStats.ContainsKey(recordPartyKey)) programData.partyStats[recordPartyKey].CheckModifyDate(modifyDate.Value);

        // CVR GUID ELEMENT
        CVRParse.CallOnPartyStats((s) => { s.IncrementGuids(); }, programData.partyStats, recordPartyKey, 
            CVRParse.CallFound_KeyValue(cvrRow.SetColumnValue,  
                CVRParse.CallFound_Void(programData.stats.IncrementGuids, CVRParse.FindElement(cvrRoot, ns, "CvrGuid"))));
        // BATCH SEQUENCE ELEMENT
        CVRParse.CallOnPartyStats((s) => { s.IncrementBatchSequences(); }, programData.partyStats, recordPartyKey,
            CVRParse.CallFound_KeyValue(cvrRow.SetColumnValue,
                CVRParse.CallFound_Void(programData.stats.IncrementBatchSequences, CVRParse.FindElement(cvrRoot, ns, "BatchSequence"))));
        // SHEET NUMBER ELEMENT
        CVRParse.CallOnPartyStatsInt((s, value) => { s.CheckSheetNumber(value); s.IncrementSheetNumbers(); }, programData.partyStats,  recordPartyKey,
            CVRParse.CallFound_KeyValue(cvrRow.SetColumnValue,
                CVRParse.CallFound_Int(programData.stats.CheckSheetNumber, 
                    CVRParse.CallFound_Void(programData.stats.IncrementSheetNumbers, CVRParse.FindElement(cvrRoot, ns, "SheetNumber")))));
        // BATCH NUMBER ELEMENT
        CVRParse.CallOnPartyStats((s) => { s.IncrementBatchNumbers(); }, programData.partyStats,  recordPartyKey,
            CVRParse.CallFound_KeyValue(cvrRow.SetColumnValue,
                CVRParse.CallFound_Void(programData.stats.IncrementBatchNumbers, CVRParse.FindElement(cvrRoot, ns, "BatchNumber"))));
        // CONTESTS ELEMENT
        CVRParse.CallOnPartyStats((s) => { s.IncrementContests(); }, programData.partyStats,  recordPartyKey,
            CVRParse.CallFound_Void(programData.stats.IncrementContests, CVRParse.FindElement(cvrRoot, ns, "Contests")));
        // PRECINCT SPLIT ELEMENT
        CVRParse.CallOnPartyStats((s) => { s.IncrementPrecinctSplit(); }, programData.partyStats,  recordPartyKey,
            CVRParse.CallFound_Void(programData.stats.IncrementPrecinctSplit, CVRParse.FindElement(cvrRoot, ns, "PrecinctSplit")));
        // Process the contests and options
        foreach (XElement contestElem in cvrRoot.Descendants(ns + "Contest"))
        {
            if (programData.Canceling) return;

            string contestId = contestElem.Element(ns + "Id").Value;
            string contestName = contestElem.Element(ns + "Name").Value;
            contestName = CVRRowBase.CleanValue(contestName); // normalize the contest name

            ContestInfo contestInfo = programData.voteSelections.AddOrReturnContest(contestId, contestName, recordPartyKey);

            // mark another participant in this contest
            contestInfo.AddParticipant();
            // mark in the stats that this contest was found
            programData.stats.CheckContest(recordPartyKey, contestName);
            if (programData.contestColumnInfo != null) {
                // mark the cvr row for this contest with a null option (marks the participation in the contest)
                cvrRow.SetContestColumnMarked(recordPartyKey, contestId, contestName, "", "", "X");
            }

            // mark the party stats that this contest was found (if party being included)
            if (programData.partyStats.ContainsKey(recordPartyKey)) programData.partyStats[recordPartyKey].CheckContest("", contestName);
            try {

                XElement? optionsContainer = contestElem.Element(ns + "Options");
                if (optionsContainer == null) continue;
                foreach (XElement optionElem in optionsContainer.Elements(ns + "Option"))
                {
                    string optionId = optionElem.Element(ns + "Id").Value;
                    XElement? nameElem = optionElem.Element(ns + "Name");
                    string? optionName = nameElem == null ? "No Name" : nameElem.Value;
                    optionName = CVRRowBase.CleanValue(optionName); // normalize the option name

                    ContestOptionInfo optionInfo = contestInfo.AddOrReturnOption(optionId, optionName);

                    bool foundValue = false;
                    foreach (XElement selectionElem in optionElem.Descendants(ns + "Value"))
                    {
                        string selectionValue = selectionElem.Value;
                        foundValue = true;
                        ContestOptionSelection selectionInfo = optionInfo.AddOrReturnSelection(selectionValue, selectionValue);
                        selectionInfo.AddVote();
                    }

                    if (programData.contestColumnInfo != null) {
                        // Mark this Contest Name and this Option Name as found in the CVRRow with a true if the selection value was found
                        cvrRow.SetContestColumnMarked(recordPartyKey, contestId, contestName, optionId, optionName, foundValue ? "1" : "0");
                    }

                    if (!foundValue)
                    {
                        programData.WriteLogLine($"  No value found in {contestName} - {optionName} - {optionElem}");
                    }
                }
            }
            catch (Exception e)
            {
                programData.WriteLogLine($"Error processing contest: {contestId} - {contestName}");
                programData.WriteLogLine(e.Message);
            }

        }

        // Add to overall stats total CVR count
        programData.stats.TotalCount++;

        if (programData.stats.TotalCount == 1) // Print every 73rd file as csv from CVRRow
        {
            programData.WriteLogLine("First Row Data Sample:");
            programData.WriteLogLine(cvrRow.FormatCSVRow());
        }
        if (programData.contestColumnInfo != null) {
            // write row to csv file
            if (csvWriter != null) csvWriter.WriteLine(cvrRow.FormatCSVRow());
        }
    }

    public static void ProcessCVRReportElement(ProgramData programData, 
        XElement reportRoot, XNamespace ns, StreamWriter? csvWriter, DateTime createDate, DateTime modifyDate)
    {
        // go through each CVR element under the root
        foreach (XElement cvrRoot in reportRoot.Elements(ns + "CVR"))
        {
            if (programData.Canceling) return;

            ProcessReportCVRElement(programData, cvrRoot, ns, csvWriter, null, null);

            // check cvr process limit
            if (programData.stats.TotalCount > programData.config.maxCVRProcessCount) break;
        }

    }

    public static void ProcessReportCVRElement(ProgramData programData, 
        XElement cvrRoot, XNamespace ns, StreamWriter? csvWriter, DateTime? createDate, DateTime? modifyDate)
    {
        string recordPartyKey = "";
        CVRRowBase cvrRow = GetOutputRow(programData.structureType);
        
        // BALLOT IMAGE ELEMENT
        CVRParse.CallFound((item) => {// set column BallotImageId from Image[FileName] elem of BallotImage
                XAttribute? fileNameAttr = item.Attribute("FileName");
                if (fileNameAttr != null) {
                    Match match = reFirstNumber.Match(fileNameAttr.Value);
                    if (match.Success) cvrRow.SetColumnValue("BallotImageId", match.Value);
                    else programData.WriteLogLine($"No number found in: {fileNameAttr.Value}");
                }
                else programData.WriteLogLine($"No file attribute on: {item}");
            }, 
            CVRParse.CallFound_Void(programData.stats.IncrementGuids, 
                CVRParse.CallFound_If((el) => CVRParse.FindElement(el, ns, "Image"), 
                    CVRParse.FindElement(cvrRoot, ns, "BallotImage"))));
        // CREATING DEVICE ELEMENT
        CVRParse.CallFound_KeyValue(cvrRow.SetColumnValue,
            CVRParse.FindElement(cvrRoot, ns, "CreatingDeviceId"));
        // BALLOT STYLE ELEMENT
        CVRParse.CallFound_KeyValue(cvrRow.SetColumnValue,
            CVRParse.FindElement(cvrRoot, ns, "BallotStyleId"));
        // Count PartyID elements under Party stat
        CVRParse.CallFound_Void(programData.stats.IncrementParty, 
                CVRParse.FindElement(cvrRoot, ns, "PartyIds"));
        // ELECTION ID ELEMENT
        CVRParse.CallFound_KeyValue(cvrRow.SetColumnValue,
            CVRParse.FindElement(cvrRoot, ns, "ElectionId"));

        // Add to overall stats total CVR count
        programData.stats.TotalCount++;

        if (programData.stats.TotalCount == 1) // Print every 73rd file as csv from CVRRow
        {
            programData.WriteLogLine(cvrRow.FormatCSVRow());
        }
        // write row to csv file
        if (csvWriter != null) csvWriter.WriteLine(cvrRow.FormatCSVRow());
    }
}