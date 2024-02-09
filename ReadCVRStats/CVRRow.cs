public class CVRRow
{
    public static string[] Columns = new string[] {
        "CvrGuid",
        "BatchNumber",
        "BatchSequence",
        "SheetNumber"
    };

    public string[] ColumnValues = new string[Columns.Length];

    public CVRRow() {}

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

    public static string FormatCSVHeader()
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