using System.Xml.Linq;
using System.Text.Json;

// // create stats collection
// CVRStats stats = new CVRStats();
// VoteSelections voteSelections = new VoteSelections();
// Dictionary<string, CVRStats> partyStats = new Dictionary<string, CVRStats>();
// // structure type to parse
// CVRStructureType structureType = CVRStructureType.SingleCVRFile;

ProgramData programData = new ProgramData (
    new CVRStats(),
    new VoteSelections(),
    new Dictionary<string, CVRStats>(),
    CVRStructureType.SingleCVRFile
);
ProgramConfiguration config = programData.config;

// set folder path of cvrs
config.cvrsPaths = new string[] { 
    // "/home/ user name /Documents/folder path" for linux
    // "C:\\Users\\ user name \\Documents\\folder path" for windows
};

// If command arguments passed, use first as folder path and second as CSV output file path
if (args.Length > 0) 
{
    if (args[0] != "") {
        config.cvrsPaths = new string[] { args[0] }; // set CVR folder path
    }
    if (args.Length > 1) 
    {
        Console.WriteLine($"Writing to file {args[1]}");
        config.csvOutputPath = args[1];
        // a third parameter selects the CVR structure type
        if (args.Length > 2) 
        {
            switch (args[2]) {
                case "singlecvr":
                    config.structureType = CVRStructureType.SingleCVRFile;
                    break;
                case "cvrreport":
                    config.structureType = CVRStructureType.CastVoteRecordReport;
                    break;
            }
            // fourth parameter and fifth parameter are for limit to files and limit to CVRs
            if (args.Length > 3) 
            {
                config.maxFileProcessCount = int.Parse(args[3]);
                if (args.Length > 4) 
                {
                    config.maxCVRProcessCount = int.Parse(args[4]);
                }
            }
        }

    }
}

CVRRowBase headerCvrRow = ParseStructureType.GetOutputRow(programData.config.structureType);
Console.WriteLine($"CVR structure type: {programData.config.structureType}");
Console.WriteLine(headerCvrRow.FormatCSVHeader());

ContestColumnInfo? prerunContestsConfigInfo = null;

if (config.contestsJsonOutputPath != "" && File.Exists(config.contestsJsonOutputPath))
{
    string jsonContestColumnInfo = File.ReadAllText(config.contestsJsonOutputPath);
    prerunContestsConfigInfo = JsonSerializer.Deserialize<ContestColumnInfo>(jsonContestColumnInfo);
    programData.contestColumnInfo = prerunContestsConfigInfo;
}

StreamWriter? csvWriter = null;
if (config.csvOutputPath != "")
{
    // store the contest info if any with the header row
    headerCvrRow.contestsInfo = prerunContestsConfigInfo;
    csvWriter = new StreamWriter(config.csvOutputPath);
    csvWriter.WriteLine(headerCvrRow.FormatCSVHeader());
}

int tickerCount = 0;
// go through all folders
foreach (string cvrsPath in config.cvrsPaths)
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
            programData.fileCount++;
            
            if (programData.structureType == CVRStructureType.CastVoteRecordReport)
            {
                Console.WriteLine($"Processing CVR report file: {file.FullName}");
            }

            XElement rootElement = XElement.Load(file.FullName);
            XNamespace ns = rootElement.GetDefaultNamespace();

            // process single CVR file
            if (programData.config.structureType == CVRStructureType.SingleCVRFile)
            {
                XElement cvrRoot = rootElement;

                ParseStructureType.ProcessCVRElement(programData, cvrRoot, ns, csvWriter, createDate, modifyDate);
            }
            else if (programData.config.structureType == CVRStructureType.CastVoteRecordReport)
            {
                // process CVR report file
                XElement reportRoot = rootElement;
                ParseStructureType.ProcessCVRReportElement(programData, reportRoot, ns, csvWriter, createDate, modifyDate);
            }
            else
            {
                Console.WriteLine($"Unknown CVR structure type: {programData.config.structureType}");
            }
        }
        
        tickerCount = programData.WriteTickerCheck(tickerCount);

        // Check file and CVR process limits
        if (programData.config.maxFileProcessCount > 0 && programData.fileCount > programData.config.maxFileProcessCount) break;
        if (programData.config.maxCVRProcessCount > 0 && programData.stats.TotalCount > programData.config.maxCVRProcessCount) break;
    }
}

// if there is a csv writer, close it
if (csvWriter != null) csvWriter.Close();

// Write the stats and contest results out to console
programData.WriteOutResults();
