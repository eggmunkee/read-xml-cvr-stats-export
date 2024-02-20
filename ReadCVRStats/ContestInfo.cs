public class ContestInfo
{
    public string ContestName { get; set; }
    public string ContestID { get; set; }
    public string ContestParty { get; set; } = "";
    public int ContestParticipantCount { get; set; } = 0;

    public Dictionary<string, ContestOptionInfo> ContestOptions { get; set; }

    public ContestInfo()
    {
        ContestOptions = new Dictionary<string, ContestOptionInfo>();
    }

    public bool HasOption(string optionID)
    {
        return ContestOptions.ContainsKey(optionID);
    }

    public ContestOptionInfo AddOrReturnOption(string optionID, string optionName)
    {
        if (!ContestOptions.ContainsKey(optionID))
        {
            ContestOptionInfo optionInfo = new ContestOptionInfo { OptionID = optionID, OptionName = optionName };
            ContestOptions.Add(optionID, optionInfo);
            return optionInfo;
        }
        return ContestOptions[optionID];
    }

    public void AddParticipant()
    {
        ContestParticipantCount++;
    }
}

// Information on a contest option for a given contest
public class ContestOptionInfo
{
    public string OptionName { get; set; }
    public string OptionID { get; set; }

    public Dictionary<string, ContestOptionSelection> ValueSelections { get; set; }

    public ContestOptionInfo()
    {
        // hold the unique values for this option and the count on each value by voters
        ValueSelections = new Dictionary<string, ContestOptionSelection>();
    }

    public bool HasSelection(string selectionValue)
    {
        return ValueSelections.ContainsKey(selectionValue);
    }

    public ContestOptionSelection AddOrReturnSelection(string selectionValue, string selectionName)
    {
        if (!ValueSelections.ContainsKey(selectionValue))
        {
            ContestOptionSelection selection = new ContestOptionSelection(OptionID, selectionValue, selectionName);
            ValueSelections.Add(selectionValue, selection);
            return selection;
        }
        return ValueSelections[selectionValue];
    }
}

// Information on a particular voter selection value for a given contest option
public class ContestOptionSelection
{
    public string ContestOptionID { get; set; }
    public string SelectionValue { get; set; }
    public string SelectionName { get; set; } // for convenience
    public int SelectionCount { get; set; }

    public ContestOptionSelection(string optionId, string selectedValue, string selectedName)
    {
        ContestOptionID = optionId;
        SelectionValue = selectedValue;
        SelectionName = selectedName;
        SelectionCount = 0;
    }

    public void AddVote()
    {
        SelectionCount++;
    }
}