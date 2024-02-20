using System.Security.Cryptography.X509Certificates;

public class CVRRowBase
{
    public string[] Columns;

    public string[] ColumnValues;

    public Dictionary<int, string> contestColumnVotes;

    public ContestColumnInfo? contestsInfo;

    public CVRRowBase() {
        Columns = new string[] {};
        ColumnValues = new string[Columns.Length];
    }

    public void AddContestInfo(ContestColumnInfo contestColInfo)
    {
        contestsInfo = contestColInfo;
        contestColumnVotes = new Dictionary<int, string>();
    }

    public bool SetColumnValue(string columnName, string value)
    {
        for (int i = 0; i < Columns.Length; i++)
        {
            if (Columns[i] == columnName)
            {
                ColumnValues[i] = value;
                return true;
            }
        }
        return false;
    }

    public bool SetContestColumnMarked(string partyName, string contestId, string contestName, string optionId, string optionName, string marked)
    {
        if (contestsInfo == null) return false;

        if (!string.IsNullOrEmpty(optionId))
        {
            var (contestIdx, optionIdx) = contestsInfo.FindContestOptionColumnIndices(partyName, contestId, contestName, optionId, optionName);
            
            if (contestIdx < 0) return false;
            //contestColumnVotes[contestIdx] = marked; // contest column is not marked when an option is specified
            if (optionIdx >= 0)
            {
                if (contestColumnVotes.ContainsKey(optionIdx))
                {
                    throw new Exception($"Trying to mark the same option twice - [{partyName}] {contestName}  - {optionName}");
                }
                contestColumnVotes[optionIdx] = marked;
                return true; // option was found and marked
            }
            return false; // option not found
        }
        // no option, mark contest column
        else
        {
            var contestIdx = contestsInfo.FindContestColumnIndex(partyName, contestId, contestName);
            if (contestIdx < 0) return false;
            contestColumnVotes[contestIdx] = marked;
            return true; // a contest was found and marked
        }
    }

    public string FormatCSVHeader()
    {
        string result = "";
        for (int i = 0; i < Columns.Length; i++)
        {
            result += WrapValue(Columns[i]);
            if (i < Columns.Length - 1) result += ",";
        }
        if (contestsInfo != null) {
            result += ",";
            for (int j = 0; j < contestsInfo.contestColumns.Count; j++)
            {
                result += WrapValue(contestsInfo.contestColumns[j]);
                if (j < contestsInfo.contestColumns.Count - 1) result += ",";
            }
        }
        return result;
    }

    public string FormatCSVRow()
    {
        string result = "";
        for (int i = 0; i < Columns.Length; i++)
        {
            result += WrapValue(ColumnValues[i]);
            if (i < Columns.Length - 1) result += ",";
        }
        if (contestsInfo != null) {
            result += ",";
            for (int j = 0; j < contestsInfo.contestColumns.Count; j++)
            {
                if (contestColumnVotes.ContainsKey(j))
                {
                    string value = contestColumnVotes[j];
                    result += value;
                }
                if (j < contestsInfo.contestColumns.Count - 1) result += ",";
            }
            
        }
        return result;
    }

    public static string WrapValue(string value)
    {
        value = CleanValue(value);
        // If any reserved characters are found in the value, wrap it in double quotes
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
        {
            value = value.Replace("\"", "\"\""); // double up any double quotes
            return $"\"{value}\"";
        }
        return value;
    }

    public static string CleanValue(string value)
    {
        if (value == null) return "";
        value = value.Replace("\r", "").Replace("\n", "");
        //value = value.Replace("\u0022", "");
        return value;
    }
}

public class SingleFileCVRRow : CVRRowBase
{

    public SingleFileCVRRow() {
        Columns = new string[] {
            "CvrGuid",
            "BatchNumber",
            "BatchSequence",
            "SheetNumber",
            "CreateDate",
            "ModifyDate"
        };
        ColumnValues = new string[Columns.Length];
    }
}

public class CVRReportRow : CVRRowBase
{
    public CVRReportRow() {
        Columns = new string[] {
            "BallotImageId",
            "CreatingDeviceId",
            "BallotStyleId",
            "ObjectId",
            "ElectionId"
        };
        ColumnValues = new string[Columns.Length];
    }
}