using System.Security.Cryptography;
using System.Text.Json;

public class ProgramData
{
    public CVRStats stats { get; set; }
    public VoteSelections voteSelections { get; set; }
    public Dictionary<string, CVRStats> partyStats { get; set; }
    public CVRStructureType structureType { get; set; }
    public int fileCount { get; set; }

    public ProgramConfiguration config { get; set; }

    public ContestColumnInfo contestColumnInfo { get; set; }

    public ProgramData(CVRStats stats, VoteSelections voteSelections, Dictionary<string, CVRStats> partyStats, CVRStructureType structureType)
    {
        this.stats = stats;
        this.voteSelections = voteSelections;
        this.partyStats = partyStats;
        //this.structureType = structureType;
        fileCount = 0;
        config = new ProgramConfiguration();
        config.structureType = structureType;
    }

    public void WriteOutResults()
    {
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
        Console.WriteLine($"  Min Modify Date:    {stats.MinModifyDate:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"  Max Modify Date:    {stats.MaxModifyDate:yyyy-MM-dd HH:mm:ss}");

        // output contest stats
        foreach (KeyValuePair<string, int> contestStat in stats.ContestCounts)
        {
            Console.WriteLine($"  Contest: {contestStat.Key} - {contestStat.Value:0######}");
        }

        // output any party stats
        foreach (KeyValuePair<string, CVRStats> partyStat in partyStats)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"  Party: {partyStat.Key} - - - - - - - - - - -");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"    Total CVRs: {partyStat.Value.TotalCount:0######}");
            Console.ResetColor();
            Console.WriteLine($"      With BatchSequence  {partyStat.Value.TotalCVRsWithBatchSequence:0######}");
            Console.WriteLine($"      With BatchNumber    {partyStat.Value.TotalCVRsWithBatchNumber:0######}");
            Console.WriteLine($"      With SheetNumber    {partyStat.Value.TotalCVRsWithSheetNumber:0######}");
            Console.WriteLine($"      With CvrGuid:       {partyStat.Value.TotalCVRsWithGuid:0######}");
            Console.WriteLine($"      With Contests:      {partyStat.Value.TotalCVRsWithContests:0######}");
            Console.WriteLine($"      With PrecinctSplit: {partyStat.Value.TotalCVRsWithPrecinctSplit:0######}");
            Console.WriteLine($"      With Party:         {partyStat.Value.TotalCVRsWithParty:0######}");
            Console.WriteLine($"      Min SheetNumber:    {partyStat.Value.MinSheetNumber:0######}");
            Console.WriteLine($"      Max SheetNumber:    {partyStat.Value.MaxSheetNumber:0######}");
            Console.WriteLine($"      Min Modify Date:    {partyStat.Value.MinModifyDate:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"      Max Modify Date:    {partyStat.Value.MaxModifyDate:yyyy-MM-dd HH:mm:ss}");

            // output contest stats
            foreach (KeyValuePair<string, int> contestStat in partyStat.Value.ContestCounts)
            {
                Console.WriteLine($"      Contest: {contestStat.Key} - {contestStat.Value:0######}");
            }
        }

        // output all the contests, options, and selections w/ vote count

        Console.WriteLine("");
        Console.WriteLine("Contests and Options w/ Vote Counts");
        foreach (ContestInfo contest in (from keyVal in voteSelections.Contests orderby keyVal.Value.ContestParty + keyVal.Value.ContestName select keyVal.Value ))
        {            
            string partySegment = contest.ContestParty == "" ? "" : $"Party: {contest.ContestParty} ";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"  {partySegment}Contest Name: {contest.ContestName} Total Included: {contest.ContestParticipantCount}"); // : {contest.ContestID}
            int totalSelections = 0;
            foreach (ContestOptionInfo option in (from keyVal in contest.ContestOptions orderby keyVal.Value.OptionName select keyVal.Value ))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"    Option: {option.OptionName}"); // {option.OptionID} - 
                Console.ResetColor();

                foreach (KeyValuePair<string, ContestOptionSelection> selection in option.ValueSelections)
                {
                    Console.Write($"      Selection: {selection.Key} - {selection.Value.SelectionName} - ");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"{selection.Value.SelectionCount}");
                    Console.ResetColor();
                    totalSelections += selection.Value.SelectionCount;
                }
            }
            Console.WriteLine($"    Total Selections: {totalSelections}");
        }

        // Write out to the current directory a JSON file with the 
        
        ContestColumnInfo buildContestColumnInfo = new ContestColumnInfo();
        if (config.contestsCsvOutputPath != "" || config.contestsJsonOutputPath != "")
        {
            StreamWriter? csvWriter = null;            
            if (config.contestsCsvOutputPath != "") csvWriter = new StreamWriter(config.contestsCsvOutputPath);

            foreach (ContestInfo contest in (from keyVal in voteSelections.Contests orderby keyVal.Value.ContestParty + keyVal.Value.ContestName select keyVal.Value ))
            {
                // add a column for the contest name
                //buildContestColumnInfo.contestColumns.Add(CVRRowBase.CleanValue(contest.ContestName));
                buildContestColumnInfo.AddContestColumn(contest.ContestParty, contest.ContestID, CVRRowBase.CleanValue(contest.ContestName));

                // go through each option and add a column for each
                foreach (ContestOptionInfo option in (from keyVal in contest.ContestOptions orderby keyVal.Value.OptionName select keyVal.Value ))
                {
                    //buildContestColumnInfo.contestColumns.Add(CVRRowBase.CleanValue(option.OptionName));
                    buildContestColumnInfo.AddContestColumn(contest.ContestParty, option.OptionID, CVRRowBase.CleanValue(option.OptionName));
                }
            }

            // wrap columns for csv
            string[] quotedContestColumns = buildContestColumnInfo.contestColumns.Select(x => CVRRowBase.WrapValue(x)).ToArray();

            // write out the CSV header row
            if (csvWriter != null) csvWriter.WriteLine(string.Join(",", quotedContestColumns));

            // collect the total participants in each contest column and the vote counts for each option
            List<string> rowValues = new List<string>();
            foreach (ContestInfo contest in (from keyVal in voteSelections.Contests orderby keyVal.Value.ContestParty + keyVal.Value.ContestName select keyVal.Value ))
            {
                rowValues.Add(contest.ContestParticipantCount.ToString());

                foreach (ContestOptionInfo option in (from keyVal in contest.ContestOptions orderby keyVal.Value.OptionName select keyVal.Value ))
                {
                    rowValues.Add(option.ValueSelections.Sum(x => x.Value.SelectionCount).ToString());
                }
            }

            // write out results line of CSV and close the writer
            if (csvWriter != null) {
                csvWriter.WriteLine(string.Join(",", rowValues));
                csvWriter.Close();
            }

            // write out the JSON file all at once, but don't overwrite it since its used on next run
            if (config.contestsJsonOutputPath != "" && !File.Exists(config.contestsJsonOutputPath)) {
                Console.WriteLine($"Writing contest info to JSON file: {config.contestsJsonOutputPath}");
                StreamWriter? jsonWriter = new StreamWriter(config.contestsJsonOutputPath);
                jsonWriter.WriteLine(JsonSerializer.Serialize(buildContestColumnInfo));
                jsonWriter.Close();
            }

        }
    }

    public int WriteTickerCheck(int tickerCount)
    {
        if (config.writeTickEvery > 0 && fileCount % config.writeTickEvery == 0) {
            var character = (tickerCount % 8) switch {
                0 => ".",
                1 => "_",
                2 => "/",
                3 => "|",
                4 => "\\",
                5 => "_",
                6 => "=",
                7 => "-",
                _ => "."
            };
            var blinkColor = (tickerCount / 8) switch {
                0 => ConsoleColor.Red,
                1 => ConsoleColor.Yellow,
                2 => ConsoleColor.Green,
                3 => ConsoleColor.Cyan,
                4 => ConsoleColor.Blue,
                5 => ConsoleColor.Magenta,
                6 => ConsoleColor.White,
                7 => ConsoleColor.Gray,
                _ => ConsoleColor.White
            };
            Console.ForegroundColor = blinkColor;
            Console.Write(character);
            // set cursor back one character
            if ((tickerCount % 8) < 7) {
                var currentPosition = Console.GetCursorPosition();
                Console.SetCursorPosition(currentPosition.Left - 1, currentPosition.Top);
            }
            Console.ResetColor();
            tickerCount = (tickerCount + 1) % 64;
        }
        return tickerCount;
    }
}

public class ProgramConfiguration
{
    public int maxFileProcessCount { get; set; } = -1;
    public int maxCVRProcessCount { get; set; } = -1;
    public int writeTickEvery { get; set; } = 1000;

    public string[] cvrsPaths { get; set; } = new string[] { };
    public string csvOutputPath { get; set; } = "";
    public string contestsCsvOutputPath { 
        get { return this.csvOutputPath == "" ? "" : this.csvOutputPath.Replace(".csv", "") + "-contests.csv"; }
    }
    public string contestsJsonOutputPath { 
        get { 
            var v = (cvrsPaths != null && cvrsPaths.Length > 0) ? cvrsPaths[0].TrimEnd('/').TrimEnd('\\') + ".contests.json" : ""; 
            return v;
        }
    }

    public CVRStructureType structureType { get; set; } = CVRStructureType.SingleCVRFile;

}

public class ContestColumnInfo
{
    public const string jsonStuctureType = "ReadCVRStats.ContestColumnInfo";
    public Dictionary<string, ContestColumnSpan> partyColumnSpans { get; set; }
    public List<string> contestColumns { get; set; }
    public List<string> columnItemIds { get; set; }


    public ContestColumnInfo()
    {
        contestColumns = new List<string>();
        columnItemIds = new List<string>();
        partyColumnSpans = new Dictionary<string, ContestColumnSpan>();
    }

    public int AddContestColumn(string partyName, string columnItemId, string contestColumnName)
    {
        int colIndex = contestColumns.Count;
        contestColumns.Add(contestColumnName);
        columnItemIds.Add(columnItemId);
        // update party column span
        if (partyColumnSpans.ContainsKey(partyName))
        {
            var currentSpan = partyColumnSpans[partyName];
            currentSpan.Max = colIndex;
            partyColumnSpans[partyName] = currentSpan;
        }
        else if (partyName != "") 
        {
            partyColumnSpans[partyName] = new ContestColumnSpan { Min = colIndex, Max = colIndex };
        }
        return colIndex;
    }

    public int FindContestColumnIndex(string partyName, string contestId, string contestName)
    {
        if (partyName != "")
        {
            if (partyColumnSpans.ContainsKey(partyName))
            {
                var span = partyColumnSpans[partyName];
                for (int i = span.Min; i <= span.Max; i++)
                {
                    if (contestColumns[i] == contestName && columnItemIds[i] == contestId) return i;
                }
            }
            return -1;            
        }
        
        for (var i=0; i < contestColumns.Count; i++) {
            if (contestColumns[i] == contestName && columnItemIds[i] == contestId) {
                return i;
            }
        }
        return -1;
    }

    public (int, int) FindContestOptionColumnIndices(string partyName, string contestId, string contestName, string optionId, string optionName)
    {
        if (partyName != "")
        {
            if (partyColumnSpans.ContainsKey(partyName))
            {
                var span = partyColumnSpans[partyName];
                for (int i = span.Min; i <= span.Max; i++)
                {
                    // Find the contest column before the options first
                    if (contestColumns[i] == contestName && columnItemIds[i] == contestId) 
                    {
                        int optionNameIndex = -1;
                        if (!string.IsNullOrEmpty(optionId))
                        {
                            // search for option after contest column until end of party span or end of columns
                            for (var j=i+1; j < contestColumns.Count && j <= span.Max; j++) {
                                if (contestColumns[j] == optionName && columnItemIds[j] == optionId) {
                                    optionNameIndex = j;
                                    break;
                                }
                            }
                        }

                        return (i, optionNameIndex);
                    }
                }
            }
            return (-1, -1);
        }
        else
        {
            // Do a full search for the column match by name and id
            int contestNameIndex = FindContestColumnIndex("",contestId, contestName);
            if (contestNameIndex == -1) return (-1, -1);

            int optionNameIndex = -1;
            if (!string.IsNullOrEmpty(optionId))
            {
                for (var i=0; i < contestColumns.Count; i++) {
                    if (contestColumns[i] == optionName && columnItemIds[i] == optionId) {
                        optionNameIndex = i;
                        break;
                    }
                }
            }

            return (contestNameIndex, optionNameIndex);
        }
    }
}

public class ContestColumnSpan
{
    public int Min { get; set; }
    public int Max { get; set; }

    public ContestColumnSpan()
    {}
}