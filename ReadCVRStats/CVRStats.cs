using System.Collections;

public class CVRStats
{
    public int TotalCount { get; set; } = 0;
    public int TotalCVRsWithBatchSequence { get; set; } = 0;
    public int TotalCVRsWithBatchNumber { get; set; } = 0;
    public int TotalCVRsWithSheetNumber { get; set; } = 0;
    public int TotalCVRsWithGuid { get; set; } = 0;
    public int TotalCVRsWithContests { get; set; } = 0;
    public int TotalCVRsWithPrecinctSplit { get; set; } = 0;
    public int TotalCVRsWithParty { get; set; } = 0;
    public int MinSheetNumber { get; set; } = int.MaxValue;
    public int MaxSheetNumber { get; set; } = int.MinValue;
    public DateTime MinModifyDate { get; set; } = DateTime.MaxValue;
    public DateTime MaxModifyDate { get; set; } = DateTime.MinValue;
    public string PartyFilter { get; set; } = "";
    public Hashtable FoundGuids { get; set; } = new Hashtable();
    public CVRStats()
    {
    }
    public CVRStats(string partyFilter)
    {
        PartyFilter = partyFilter;
    }
    
    public void IncrementBatchSequences()
    {
        TotalCVRsWithBatchSequence++;
    }
    public void IncrementSheetNumbers()
    {
        TotalCVRsWithSheetNumber++;
    }
    public void IncrementBatchNumbers()
    {
        TotalCVRsWithBatchNumber++;
    }
    public void IncrementGuids()
    {
        TotalCVRsWithGuid++;
    }
    public void IncrementContests()
    {
        TotalCVRsWithContests++;
    }
    public void IncrementPrecinctSplit()
    {
        TotalCVRsWithPrecinctSplit++;
    }
    public void IncrementParty()
    {
        TotalCVRsWithParty++;
    }
    public void CheckSheetNumber(int sheetNumber)
    {
        if (sheetNumber < MinSheetNumber) MinSheetNumber = sheetNumber;
        if (sheetNumber > MaxSheetNumber) MaxSheetNumber = sheetNumber;
    }
    public void CheckModifyDate(DateTime modifyDate)
    {
        if (modifyDate < MinModifyDate) MinModifyDate = modifyDate;
        if (modifyDate > MaxModifyDate) MaxModifyDate = modifyDate;
    }
    

}