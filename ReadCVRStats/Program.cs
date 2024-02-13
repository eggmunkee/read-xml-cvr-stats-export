using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

// create stats collection
CVRStats stats = new CVRStats();
Dictionary<string, CVRStats> partyStats = new Dictionary<string, CVRStats>();
// structure type to parse
CVRStructureType structureType = CVRStructureType.SingleCVRFile;

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
        // a third parameter selects the CVR structure type
        if (args.Length > 2) 
        {
            //Console.WriteLine($"CVR structure type: {args[2]}");
            switch (args[2]) {
                case "singlecvr":
                    structureType = CVRStructureType.SingleCVRFile;
                    break;
                case "cvrreport":
                    structureType = CVRStructureType.CastVoteRecordReport;
                    break;
            }
            
        }
    }
}

CVRRowBase headerCvrRow = ParseStructureType.GetOutputRow(structureType);
Console.WriteLine($"CVR structure type: {structureType}");
Console.WriteLine(headerCvrRow.FormatCSVHeader());

StreamWriter? csvWriter = null;
if (csvOutputPath != "")
{
    csvWriter = new StreamWriter(csvOutputPath);
    csvWriter.WriteLine(headerCvrRow.FormatCSVHeader());
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
            fileCount++;
            
            if (structureType == CVRStructureType.CastVoteRecordReport)
            {
                Console.WriteLine($"Processing CVR report file: {file.FullName}");
            }

            XElement rootElement = XElement.Load(file.FullName);
            XNamespace ns = rootElement.GetDefaultNamespace();

            // process single CVR file
            if (structureType == CVRStructureType.SingleCVRFile)
            {
                XElement cvrRoot = rootElement;

                ParseStructureType.ProcessCVRElement(structureType, stats, partyStats, cvrRoot, ns, csvWriter, createDate, modifyDate, fileCount);
            }
            else if (structureType == CVRStructureType.CastVoteRecordReport)
            {
                // process CVR report file
                XElement reportRoot = rootElement;
                ParseStructureType.ProcessCVRReportElement(structureType, stats, partyStats, reportRoot, ns, csvWriter, createDate, modifyDate, fileCount);
            }
            else
            {
                Console.WriteLine($"Unknown CVR structure type: {structureType}");
            }
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

