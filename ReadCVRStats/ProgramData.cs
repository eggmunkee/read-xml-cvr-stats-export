using System.Text.Json;
using System.Xml.Linq;

public class ProgramData
{
    public CVRStats stats { get; set; }
    public VoteSelections voteSelections { get; set; }
    public Dictionary<string, CVRStats> partyStats { get; set; }
    public CVRStructureType structureType { get; set; }
    public int fileCount { get; set; }

    public CVRRowBase headerCvrRow { get; set; }

    public ProgramConfiguration config { get; set; }

    public ContestColumnInfo contestColumnInfo { get; set; }

    public bool Canceling = false;

    public bool ReadyForFolderScan { get { return config.firstCVRsPath != "" && headerCvrRow != null; }}
    public bool ReadyForContestOutput { get { return contestColumnInfo != null; } }

    public StreamWriter logWriter { get; set; }

    public ProgramData(string[] args)
    {
        stats = new CVRStats();
        voteSelections = new VoteSelections();
        partyStats = new Dictionary<string, CVRStats>();
        fileCount = 0;
        config = new ProgramConfiguration();
        config.runArgs = args;
        config.structureType = CVRStructureType.SingleCVRFile;
    }

    public ProgramData(string[] args, CVRStats stats, VoteSelections voteSelections, Dictionary<string, CVRStats> partyStats, CVRStructureType structureType)
    {
        this.stats = stats;
        this.voteSelections = voteSelections;
        this.partyStats = partyStats;
        //this.structureType = structureType;
        fileCount = 0;
        config = new ProgramConfiguration();
        config.runArgs = args;
        config.structureType = structureType;
    }

    public void OpenLog()
    {
        if (config.runLogPath != "") {
            try {
                logWriter = new StreamWriter(config.runLogPath);
            }
            catch (Exception ex) {
                Console.WriteLine($"Error opening log file [{config.runLogPath}]: " + ex.Message);
            }
        }
    }

    public void CloseLog()
    {
        if (logWriter != null) logWriter.Close();
    }

    public void WriteLogLine(string message, bool writeConsole = true)
    {
        if (logWriter != null) logWriter.WriteLine(message);
        Console.WriteLine(message);
    }

    public bool RunProgram()
    {
        Console.CancelKeyPress += new ConsoleCancelEventHandler(HandleConsoleCancelEvent);

        // Load the command line arguments
        LoadParameters();

        try {
            OpenLog();

            // Load the contests file if found
            if (!Canceling) LoadContestColumnsInfoFile();

            // process the folder and export the csv file
            if (!Canceling) RunFullProcess();

            // Write the stats and contest results out to console
            if (!Canceling) WriteOutResults();
        }
        catch (Exception ex) {
            WriteLogLine("Error running program: " + ex.Message);
            WriteLogLine(ex.StackTrace);
            throw;
        }
        finally {
            //CloseLog();
        }

        return !Canceling;
    }

    public void HandleConsoleCancelEvent(object sender, ConsoleCancelEventArgs args)
    {
        WriteLogLine("Canceling process...");
        args.Cancel = true;
        // Set the canceling flag to true to stop processing
        Canceling = true;
    }

    public void LoadParameters()
    {
        // If command arguments passed, use first as folder path and second as CSV output file path
        if (config.runArgs.Length > 0) 
        {
            if (config.runArgs[0] != "") {
                config.cvrsPaths = new string[] { config.runArgs[0] }; // set CVR folder path
            }
            if (config.runArgs.Length > 1) 
            {
                //Console.WriteLine($"Writing to file {args[1]}");
                //config.csvOutputPath = args[1]; // IGNORED - auto named now
                // a third parameter selects the CVR structure type
                if (config.runArgs.Length > 2) 
                {
                    switch (config.runArgs[2]) {
                        case "singlecvr":
                            config.structureType = CVRStructureType.SingleCVRFile;
                            break;
                        case "cvrreport":
                            config.structureType = CVRStructureType.CastVoteRecordReport;
                            break;
                    }
                    // fourth parameter and fifth parameter are for limit to files and limit to CVRs
                    if (config.runArgs.Length > 3) 
                    {
                        config.maxFileProcessCount = int.Parse(config.runArgs[3]);
                        if (config.runArgs.Length > 4) 
                        {
                            config.maxCVRProcessCount = int.Parse(config.runArgs[4]);
                        }
                    }
                }

            }
        }
    }

    public void LoadContestColumnsInfoFile()
    {

        headerCvrRow = ParseStructureType.GetOutputRow(config.structureType);
        WriteLogLine($"CVR structure type: {config.structureType}");
        WriteLogLine($"Base CSV columns: {headerCvrRow.FormatCSVHeader()}");
        WriteLogLine($"Output CSV file: {config.contestsCsvOutputPath}");

        ContestColumnInfo? prerunContestsConfigInfo = null;

        if (config.contestsJsonOutputPath != "" && File.Exists(config.contestsJsonOutputPath))
        {
            WriteLogLine($"Using Contests Json file: {config.contestsJsonOutputPath}");

            try {
                string jsonContestColumnInfo = File.ReadAllText(config.contestsJsonOutputPath);
                prerunContestsConfigInfo = JsonSerializer.Deserialize<ContestColumnInfo>(jsonContestColumnInfo);
                contestColumnInfo = prerunContestsConfigInfo;
            }
            catch (Exception ex)
            {
                WriteLogLine("Error reading contests json file: " + ex.Message);
                WriteLogLine("Delete the file to generate a new one. Exiting process...");
                return;
            }
        }
        else if (config.contestsJsonOutputPath != "")
        {
            WriteLogLine($"Creating Contests Json file: {config.contestsJsonOutputPath}");
        }
    }

    public void RunFullProcess()
    {
        if (!ReadyForFolderScan) {
            WriteLogLine("No CVR folder path or cvr heading row not generated, exiting...");
            return;
        }

        // if there is no contest column info, create a new one by processing the CVR folder without writing out the CSV file
        if (!ReadyForContestOutput)
        {
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Cyan;
            Console.Write("***** Scanning Folder For Contest Information *****");
            Console.ResetColor();
            Console.WriteLine("");
            WriteLogLine("***** Scanning Folder For Contest Information *****", false);

            // process the folder and populate the 
            if (!Canceling) ProcessCVRFolder(false, true);

            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Cyan;
            Console.Write("***** Generating Contest Information File *****");
            Console.ResetColor();
            Console.WriteLine("");
            WriteLogLine("***** Generating Contest Information File *****", false);

            if (!Canceling) contestColumnInfo = CreateContestsJsonFile();
        }
        
        if (!ReadyForContestOutput) {
            WriteLogLine("Could not build contest column info, exiting...");
            return;
        }

        Console.ForegroundColor = ConsoleColor.Black;
        Console.BackgroundColor = ConsoleColor.Cyan;
        Console.Write($"***** Generating CSV Results and Contests Summary *****");
        Console.ResetColor();
        Console.WriteLine("");
        WriteLogLine("***** Generating CSV Results and Contests Summary *****", false);

        // process the folder again, this time writing out the CSV file
        if (!Canceling) ProcessCVRFolder(true, false);

        // write out the contest totals file
        if (!Canceling) CreateContestsTotalsFile();
    }

    public void ProcessCVRFolder(bool writeCSV = true, bool writeJSON = true)
    {
        StreamWriter? csvWriter = null;
        if (writeCSV) {
            if (config.contestsCsvOutputPath != "")
            {
                try {
                    // store the contest info if any with the header row
                    headerCvrRow.contestsInfo = contestColumnInfo;
                    csvWriter = new StreamWriter(config.contestsCsvOutputPath);
                    csvWriter.WriteLine(headerCvrRow.FormatCSVHeader());
                }
                catch (Exception ex) {
                    WriteLogLine("Error opening and writing to CSV output file: " + ex.Message);
                }

            }
            else {
                WriteLogLine("No CSV output file being written");
            }
        }
        else {
            WriteLogLine("Scanning folder to build contests information...");
        }

        try {
            int tickerCount = 0;
            // go through all folders
            foreach (string cvrsPath in config.cvrsPaths)
            {
                if (Canceling) break;

                // get folder
                DirectoryInfo diCVRs = new DirectoryInfo(cvrsPath);
                // process each file
                foreach (FileInfo? file in diCVRs.EnumerateFiles())
                {
                    if (Canceling) break;

                    if (file.Extension.ToLower() == ".xml") // only process xml files
                    {
                        DateTime createDate = file.CreationTime;
                        DateTime modifyDate = file.LastWriteTime;
                        fileCount++;
                        
                        if (structureType == CVRStructureType.CastVoteRecordReport)
                        {
                            WriteLogLine($"Processing CVR report file: {file.FullName}");
                        }

                        XElement rootElement = XElement.Load(file.FullName);
                        XNamespace ns = rootElement.GetDefaultNamespace();

                        if (Canceling) break;

                        // process single CVR file
                        if (config.structureType == CVRStructureType.SingleCVRFile)
                        {
                            XElement cvrRoot = rootElement;

                            ParseStructureType.ProcessCVRElement(this, cvrRoot, ns, csvWriter, createDate, modifyDate);
                        }
                        else if (config.structureType == CVRStructureType.CastVoteRecordReport)
                        {
                            // process CVR report file
                            XElement reportRoot = rootElement;
                            ParseStructureType.ProcessCVRReportElement(this, reportRoot, ns, csvWriter, createDate, modifyDate);
                        }
                        else
                        {
                            WriteLogLine($"Unknown CVR structure type: {config.structureType}");
                        }
                    }
                    
                    tickerCount = WriteTickerCheck(tickerCount);

                    // Check file and CVR process limits
                    if (config.maxFileProcessCount > 0 && fileCount > config.maxFileProcessCount) break;
                    if (config.maxCVRProcessCount > 0 && stats.TotalCount > config.maxCVRProcessCount) break;
                }
            }

            // if there is a csv writer, close it
            if (csvWriter != null) csvWriter.Close();
        }
        catch (Exception ex) {
            WriteLogLine("Error processing CVR folder: " + ex.Message);
        }
    }

    public ContestColumnInfo CreateContestsJsonFile()
    {
        // Write out to the current directory a JSON file with the contest column information       
        ContestColumnInfo buildContestColumnInfo = new ContestColumnInfo();
        if (config.contestsCsvOutputPath != "" || config.contestsJsonOutputPath != "")
        {
            // StreamWriter? csvWriter = null;
            // if (contestColumnInfo != null) {
            //     if (config.contestsCsvOutputPath != "") csvWriter = new StreamWriter(config.contestsCsvOutputPath);
            // }

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
            //string[] quotedContestColumns = buildContestColumnInfo.contestColumns.Select(x => CVRRowBase.WrapValue(x)).ToArray();

            // // write out the CSV header row
            // if (csvWriter != null) csvWriter.WriteLine(string.Join(",", quotedContestColumns));

            // collect the total participants in each contest column and the vote counts for each option
            // List<string> rowValues = new List<string>();
            // foreach (ContestInfo contest in (from keyVal in voteSelections.Contests orderby keyVal.Value.ContestParty + keyVal.Value.ContestName select keyVal.Value ))
            // {
            //     rowValues.Add(contest.ContestParticipantCount.ToString());

            //     foreach (ContestOptionInfo option in (from keyVal in contest.ContestOptions orderby keyVal.Value.OptionName select keyVal.Value ))
            //     {
            //         rowValues.Add(option.ValueSelections.Sum(x => x.Value.SelectionCount).ToString());
            //     }
            // }

            // // write out results line of CSV and close the writer
            // if (csvWriter != null) {
            //     csvWriter.WriteLine(string.Join(",", rowValues));
            //     csvWriter.Close();
            // }

            // write out the JSON file all at once
            if (config.contestsJsonOutputPath != "") { // && !File.Exists(config.contestsJsonOutputPath)) {
                WriteLogLine($"Writing contest info to JSON file: {config.contestsJsonOutputPath}");
                StreamWriter? jsonWriter = new StreamWriter(config.contestsJsonOutputPath);
                jsonWriter.WriteLine(JsonSerializer.Serialize(buildContestColumnInfo));
                jsonWriter.Close();
            }

        }

        return buildContestColumnInfo;
    }

    public void CreateContestsTotalsFile()
    {
        // Write out to the current directory a JSON file with the contest column information       
        //ContestColumnInfo buildContestColumnInfo = new ContestColumnInfo();
        if (contestColumnInfo != null && config.contestTotalsCsvOutputPath != "")
        {
            StreamWriter? csvWriter = new StreamWriter(config.contestTotalsCsvOutputPath);

            // wrap columns for csv
            string[] quotedContestColumns = contestColumnInfo.contestColumns.Select(x => CVRRowBase.WrapValue(x)).ToArray();

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

        }

    }


    public void WriteOutResults()
    {
        WriteLogLine("");
        WriteLogLine($"Total CVRs processed: {stats.TotalCount:0######}");
        WriteLogLine($"  With BatchSequence  {stats.TotalCVRsWithBatchSequence:0######}");
        WriteLogLine($"  With BatchNumber    {stats.TotalCVRsWithBatchNumber:0######}");
        WriteLogLine($"  With SheetNumber    {stats.TotalCVRsWithSheetNumber:0######}");
        WriteLogLine($"  With CvrGuid:       {stats.TotalCVRsWithGuid:0######}");
        WriteLogLine($"  With Contests:      {stats.TotalCVRsWithContests:0######}");
        WriteLogLine($"  With PrecinctSplit: {stats.TotalCVRsWithPrecinctSplit:0######}");
        WriteLogLine($"  With Party:         {stats.TotalCVRsWithParty:0######}");
        WriteLogLine($"  Min SheetNumber:    {stats.MinSheetNumber:0######}");
        WriteLogLine($"  Max SheetNumber:    {stats.MaxSheetNumber:0######}");
        WriteLogLine($"  Min Modify Date:    {stats.MinModifyDate:yyyy-MM-dd HH:mm:ss}");
        WriteLogLine($"  Max Modify Date:    {stats.MaxModifyDate:yyyy-MM-dd HH:mm:ss}");

        // output contest stats
        foreach (KeyValuePair<string, int> contestStat in stats.ContestCounts)
        {
            WriteLogLine($"  Contest: {contestStat.Key} - {contestStat.Value:0######}");
        }

        // output any party stats
        foreach (KeyValuePair<string, CVRStats> partyStat in partyStats)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            WriteLogLine($"  Party: {partyStat.Key} - - - - - - - - - - -");
            Console.ForegroundColor = ConsoleColor.Green;
            WriteLogLine($"    Total CVRs: {partyStat.Value.TotalCount:0######}");
            Console.ResetColor();
            WriteLogLine($"      With BatchSequence  {partyStat.Value.TotalCVRsWithBatchSequence:0######}");
            WriteLogLine($"      With BatchNumber    {partyStat.Value.TotalCVRsWithBatchNumber:0######}");
            WriteLogLine($"      With SheetNumber    {partyStat.Value.TotalCVRsWithSheetNumber:0######}");
            WriteLogLine($"      With CvrGuid:       {partyStat.Value.TotalCVRsWithGuid:0######}");
            WriteLogLine($"      With Contests:      {partyStat.Value.TotalCVRsWithContests:0######}");
            WriteLogLine($"      With PrecinctSplit: {partyStat.Value.TotalCVRsWithPrecinctSplit:0######}");
            WriteLogLine($"      With Party:         {partyStat.Value.TotalCVRsWithParty:0######}");
            WriteLogLine($"      Min SheetNumber:    {partyStat.Value.MinSheetNumber:0######}");
            WriteLogLine($"      Max SheetNumber:    {partyStat.Value.MaxSheetNumber:0######}");
            WriteLogLine($"      Min Modify Date:    {partyStat.Value.MinModifyDate:yyyy-MM-dd HH:mm:ss}");
            WriteLogLine($"      Max Modify Date:    {partyStat.Value.MaxModifyDate:yyyy-MM-dd HH:mm:ss}");

            // output contest stats
            foreach (KeyValuePair<string, int> contestStat in partyStat.Value.ContestCounts)
            {
                WriteLogLine($"      Contest: {contestStat.Key} - {contestStat.Value:0######}");
            }
        }

        // output all the contests, options, and selections w/ vote count

        WriteLogLine("");
        WriteLogLine("Contests and Options w/ Vote Counts");
        foreach (ContestInfo contest in (from keyVal in voteSelections.Contests orderby keyVal.Value.ContestParty + keyVal.Value.ContestName select keyVal.Value ))
        {            
            string partySegment = contest.ContestParty == "" ? "" : $"Party: {contest.ContestParty} ";
            Console.ForegroundColor = ConsoleColor.Cyan;
            WriteLogLine($"  {partySegment}Contest Name: {contest.ContestName} Total Included: {contest.ContestParticipantCount}"); // : {contest.ContestID}
            int totalSelections = 0;
            foreach (ContestOptionInfo option in (from keyVal in contest.ContestOptions orderby keyVal.Value.OptionName select keyVal.Value ))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                WriteLogLine($"    Option: {option.OptionName}"); // {option.OptionID} - 
                Console.ResetColor();

                foreach (KeyValuePair<string, ContestOptionSelection> selection in option.ValueSelections)
                {
                    Console.Write($"      Selection: {selection.Key} - {selection.Value.SelectionName} - ");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    WriteLogLine($"{selection.Value.SelectionCount}");
                    Console.ResetColor();
                    totalSelections += selection.Value.SelectionCount;
                }
            }
            WriteLogLine($"    Total Selections: {totalSelections}");
        }

        
    }

    public int WriteTickerCheck(int tickerCount)
    {
        int tickerFrames = 8 * 4;
        //int colorFrames = 4;
        if (config.writeTickEvery > 0 && fileCount % config.writeTickEvery == 0) {
            var character = (tickerCount % (tickerFrames / 4)) switch {
                0 => "/",
                1 => "|",
                2 => "\\",
                3 => "=",
                4 => "\\",
                5 => "|",
                6 => "/",
                7 => "=",
                _ => "_"
            };
            var blinkColor = (tickerCount % 4) switch {
                0 => ConsoleColor.White,
                1 => ConsoleColor.Red,
                2 => ConsoleColor.Green,
                3 => ConsoleColor.Cyan,
                // 4 => ConsoleColor.Blue,
                // 5 => ConsoleColor.Magenta,
                // 6 => ConsoleColor.Yellow,
                // 7 => ConsoleColor.Gray,
                _ => ConsoleColor.White
            };
            Console.ForegroundColor = blinkColor;
            // set cursor back one character after first ticker frame writes
            if ((tickerCount % tickerFrames) > 0) {
                var currentPosition = Console.GetCursorPosition();
                if (currentPosition.Left > 0) {
                    Console.SetCursorPosition(currentPosition.Left - 1, currentPosition.Top);
                }
            }
            else if (Console.CursorLeft > Console.BufferWidth - 2) {
                Console.WriteLine("");
            }
            Console.Write(character); // write ticker character
            Console.ResetColor(); // reset color
            tickerCount = (tickerCount + 1) % (tickerFrames * 1); // increment ticker count based on ticker/color frame #s
        }
        return tickerCount;
    }
}

public class ProgramConfiguration
{
    public int maxFileProcessCount { get; set; } = -1;
    public int maxCVRProcessCount { get; set; } = -1;
    public int writeTickEvery { get; set; } = 25;

    public string[] cvrsPaths { get; set; } = new string[] { };

    private string startDateTimeStr = DateTime.Now.ToFileTimeUtc().ToString().Substring(0, 12);

    public string firstCVRsPath {
        get { return (cvrsPaths != null && cvrsPaths.Length > 0) ? cvrsPaths[0] : ""; }
    }
    public string contestsCsvOutputPath { 
        //get { return this.csvOutputPath == "" ? "" : this.csvOutputPath.Replace(".csv", "") + "-contests.csv"; }
        get {
            return firstCVRsPath != "" ? firstCVRsPath.TrimEnd('/').TrimEnd('\\') + $".{startDateTimeStr}.csv" : "";
        }
    }
    public string contestTotalsCsvOutputPath { 
        //get { return this.csvOutputPath == "" ? "" : this.csvOutputPath.Replace(".csv", "") + "-contests.csv"; }
        get {
            return firstCVRsPath != "" ? firstCVRsPath.TrimEnd('/').TrimEnd('\\') + $".{startDateTimeStr}.totals.csv" : "";
        }
    }
    public string contestsJsonOutputPath { 
        get { 
            var v = (cvrsPaths != null && cvrsPaths.Length > 0) ? cvrsPaths[0].TrimEnd('/').TrimEnd('\\') + ".contests.json" : ""; 
            return v;
        }
    }

    public string runLogPath {
        get { return (firstCVRsPath != "") ? firstCVRsPath.TrimEnd('/').TrimEnd('\\') + $".{startDateTimeStr}.log" : ""; }
    }

    public CVRStructureType structureType { get; set; } = CVRStructureType.SingleCVRFile;

    public string[] runArgs { get; set; }

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