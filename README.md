# Read XML CVRs, Generate Stats and Export to CSV format
.NET Console Program to read a folder of XML CVR records and generate statistics and optionally output a CSV of all CVRs.

## Usage - Install and Run
1. Install .NET 8.0 SDK
2. `dotnet build` in the root directory
3. `dotnet run --project ReadCVRStats/ReadCVRStats.csproj` to process the CVRs in the folder hardcoded in Program.cs line 12 (comment out and set)
4. `dotnet run --project ReadCVRStats/ReadCVRStats.csproj -- "/mnt/.../full folder path"` to process the CVRs in the folder passed as the first argument after "--".
5. `dotnet run --project ReadCVRStats/ReadCVRStats.csproj -- "/mnt/.../full folder path" "custom_csv_export.csv"` to process the CVRs in the folder passed in and export to the csv file passed in second.

## Stats
* Total CVRs
* CVRs With BatchSequence
* CVRs With BatchNumber
* CVRs With SheetNumber
* CVRs With CvrGuid
* CVRs With Contests
* CVRs With PrecinctSplit
* CVRs With Party
* Min/Max SheetNumber
* Per Party Ballot Guid found:
    - Same stats for subset with that party ballot
* TODO: Add stats per Contents and Option to complete stats

## CSV Output Fields
* CvrGuid
* BatchNumber
* BatchSequence
* SheetNumber
* CreateDate
* ModifyDate
* TODO: Add inputs to capture selected ballot selections in columns

## Read/Export Unordered
CVR files are read in the order returned by reading the directory. The CSV file generated may not generate the same order on subsequent runs.

### Notes and Plans
To capture all contents of a dataset, a project could be added that generates a list of candidates among the CVRs which could feed into the 
CSV export columns. A file format could created by hand or generated that would be passed in and read to generate columns for the export from it.

Generic format needed (expanded for all options found):
* Party Ballot Guid
    - Contest Name
        - Contest Option
        - Contest Option 2
    - Contest Two Name
        - Contest Option
        - Contest Option 2

Columns generated from example above alone would be:
| Party Ballot Guid | Contest Name | Option | Option 2 | Contest Two Name | Contest Option | Contest Option 2 |