public class CVRRowBase
{
    public string[] Columns;

    public string[] ColumnValues;

    public CVRRowBase() {
        Columns = new string[] {};
        ColumnValues = new string[Columns.Length];
    }

    public void SetColumnValue(string columnName, string value)
    {
        for (int i = 0; i < Columns.Length; i++)
        {
            if (Columns[i] == columnName)
            {
                ColumnValues[i] = value;
                return;
            }
        }
    }

    public string FormatCSVHeader()
    {
        string result = "";
        for (int i = 0; i < Columns.Length; i++)
        {
            result += Columns[i];
            if (i < Columns.Length - 1) result += ",";
        }
        return result;
    }
    public string FormatCSVRow()
    {
        string result = "";
        for (int i = 0; i < Columns.Length; i++)
        {
            result += ColumnValues[i];
            if (i < Columns.Length - 1) result += ",";
        }
        return result;
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