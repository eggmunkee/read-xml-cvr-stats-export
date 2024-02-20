public class VoteSelections
{
    public Dictionary<string, ContestInfo> Contests { get; set; }

    public VoteSelections()
    {
        Contests = new Dictionary<string, ContestInfo>();
    }

    // public bool HasContest(string contestID)
    // {
    //     return Contests.ContainsKey(contestID);
    // }

    public ContestInfo AddOrReturnContest(string contestID, string contestName)
    {
        return AddOrReturnContest(contestID, contestName, "");
    }

    public ContestInfo AddOrReturnContest(string contestID, string contestName, string partyNameKey)
    {
        if (!Contests.ContainsKey(contestID))
        {
            ContestInfo contest = new ContestInfo { ContestID = contestID, ContestName = contestName, ContestParty = partyNameKey };
            Contests.Add(contestID, contest);
            return contest;
        }
        return Contests[contestID];
    }

    public ContestOptionInfo AddOrReturnContestOption(string contestID, string optionID, string optionName)
    {
        if (Contests.ContainsKey(contestID))
        {
            ContestOptionInfo optionInfo = Contests[contestID].AddOrReturnOption(optionID, optionName);
            Contests[contestID].AddOrReturnOption(optionID, optionName);
            return optionInfo;
        }
        throw new Exception($"The Contest must exist (contest id {contestID}) before adding an option (option id {optionID}, name {optionName})");
    }

    public ContestOptionSelection AddOrReturnContestOptionSelection (string contestID, string optionID, string selectionValue, string selectionName)
    {
        if (Contests.ContainsKey(contestID))
        {
            if (Contests[contestID].HasOption(optionID))
            {
                if (!Contests[contestID].ContestOptions[optionID].ValueSelections.ContainsKey(selectionValue))
                {
                    ContestOptionSelection selection = new ContestOptionSelection(optionID, selectionValue, selectionName);
                    Contests[contestID].ContestOptions[optionID].ValueSelections.Add(selectionValue, selection);
                    return selection;
                }
                return Contests[contestID].ContestOptions[optionID].ValueSelections[selectionValue];
            }
            throw new Exception($"The Contest Option must exist (contest id {contestID}, option id {optionID}) before adding a selection (selection value {selectionValue}, name {selectionName})");
        }
        throw new Exception($"The Contest must exist (contest id {contestID}) before adding a selection (option id {optionID}, selection value {selectionValue}, name {selectionName})");
    }

    // public bool HasContestOptionSelection(string contestID, string optionID, string selectionValue)
    // {
    //     if (Contests.ContainsKey(contestID))
    //     {
    //         if (Contests[contestID].HasOption(optionID))
    //         {
    //             return Contests[contestID].ContestOptions[optionID].ValueSelections.ContainsKey(selectionValue);
    //         }
    //     }
    //     return false;
    // }

    // public void AddContestOptionSelection(string contestID, string optionID, string selectionValue, string selectionName)
    // {
    //     if (Contests.ContainsKey(contestID))
    //     {
    //         if (Contests[contestID].HasOption(optionID))
    //         {
    //             if (!Contests[contestID].ContestOptions[optionID].ValueSelections.ContainsKey(selectionValue))
    //             {
    //                 Contests[contestID].ContestOptions[optionID].ValueSelections.Add(selectionValue, new ContestOptionSelection(contestID, optionID, selectionValue, selectionName));
    //             }
    //         }
    //     }
    // }

}