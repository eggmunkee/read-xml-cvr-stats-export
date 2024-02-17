# Read XML CVRs, Generate Stats and Export to CSV format
.NET Console Program to read a folder of XML CVR records and generate statistics and optionally output a CSV of all CVRs.

## Usage - Install and Run
1. Install .NET 8.0 SDK
2. `dotnet build` in the root directory
3. `dotnet run --project ReadCVRStats/ReadCVRStats.csproj` to process the CVRs in the folder hardcoded in Program.cs line 12 (comment out and set)
4. `dotnet run --project ReadCVRStats/ReadCVRStats.csproj -- "/mnt/.../full folder path"` to process the CVRs in the folder passed as the first argument after "--".
5. `dotnet run --project ReadCVRStats/ReadCVRStats.csproj -- "/mnt/.../full folder path" "custom_csv_export.csv"` to process the CVRs in the folder passed in and export to the csv file passed in second.
6. For CVRReport files with CVR child elements, use "cvrreport" type as the 3rd parameter. `dotnet run --project ReadCVRStats/ReadCVRStats.csproj -- "/mnt/.../full folder path" "custom_csv_export.csv" "cvreport"`

For an alternative run method, see the [Docker](#build-and-run-using-docker) instructions below.

#### CVR Report format is a work in progress
I plan to allow it to count CVRs that represent 1 vote on 1 race, instead of a CVR containing all votes for a voter. File date is not applied to output rows in this mode since they would apply to the report file instead of the CVR record file.

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
Expanding ability to process CVR Report files and different way that CVRs may represent a single contest choice or an entire set of selections for a voter.

--

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

# Build and Run using Docker
### Cross Platform -- No need to install other dependencies

After installing Docker for your OS and it is running. Download the ReadCVRStats source to a folder then navigate to that folder in the terminal. Run these steps to install the program in docker.

Note: The docker build command as described must be run from the root folder of the source code. The run, create, or start commands can be run in any directory after build has run in a terminal. All uses require the Docker service to be running.

### Run Once: Build an Image from the project (compiles and stores):
`docker build -t read-cvr-stats -f Dockerfile .`

Either run the Image directly or create a permanent Container from the Image from the terminal.

### Option A: Run the Image Immediately in a Container:
`docker run -it --rm read-cvr-stats`

After this runs only the Image will remain in Docker, as the Container is removed after use.

### Option B: Create a Container from the Image (that can be re-run):
`docker create --name run-cvr-stats read-cvr-stats`

This will create the Docker container with the name "run-cvr-stats" which you should see in the list if you run `docker system df -v` which lists all images and containers.

#### Run the program from the Container
`docker start -ia run-cvr-stats`

### Note: The program requires arguments/parameters to be passed in
You will need to provide arguments to the program to tell it what files to examine for either method of running the program. See below for syntax to pass in the parameters.

## Run ReadCVRStats on a Folder, Ouput a CSV File, Set Format

Running this docker command will run the program from the docker container and send standard output to the terminal window. You can also abort the program by pressing Ctrl-C like normal.

These examples use the Image to run the program. To run from a created container use this command before the parameters: `docker start -ia run-cvr-stats` instead.

### Run stats only on the "cvr_xml_files" folder:

`docker run -it --rm read-cvr-stats "/folder/path_to/cvr_xml_files"`

| Option | Value |
| ------ | ------ |
| CVRs Folder Path | "/folder/path_to/cvr_xml_files" |
| CSV Ouput Filename | No CSV output |
| CVR Format Type | SingleCVRFile |

### Run stats and export CSV:

`docker run -it --rm read-cvr-stats "/folder/path_to/cvr_xml_files" "/documents/results/primary_results.csv"`

| Option | Value |
| ------ | ------ |
| CVRs Folder Path | "/folder/path_to/cvr_xml_files" |
| CSV Ouput Filename | "/documents/results/primary_results.csv" |
| CVR Format Type | SingleCVRFile |

### Run stats and export CSV for a CVRReport style list of xml files:

`docker run -it --rm read-cvr-stats "/folder/path_to/cvr_xml_files" "/documents/results/general_cvr_report_export.csv" "cvrreport"`

| Option | Value |
| ------ | ------ |
| CVRs Folder Path | "/folder/path_to/cvr_xml_files" |
| CSV Ouput Filename | "/documents/results/general_cvr_report_export.csv" |
| CVR Format Type | CastVoteRecordReport |

### CVR Format Type Options
| Parameter Value | Type | Element Used for CVR ID |
| ------- | ------ | ------- |
| "singlecvr" | SingleCVRFile | CvrGuid |
| "cvrreport" | CastVoteRecordReport | BallotImage/Image/(FileName Attribute) |
