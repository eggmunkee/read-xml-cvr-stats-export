/// ReadCVRStats program - scan CVR files, build contest information, build stats, and export to CSV file

ProgramData programData = new ProgramData(args);

try {
    programData.RunProgram();

    programData.WriteLogLine("-----------------------------------------------");

    if (programData.Canceling) {
        programData.WriteLogLine("Full Process Was Canceled. The CSV file may have written out the data up to the point of cancelation.");
    }
    else {
        programData.WriteLogLine("Program completed successfully");
    }
}
finally {
    programData.CloseLog();
}

Console.WriteLine("-----------------------------------------------");
Console.WriteLine("Press any key to exit");
try {
    Console.ReadKey();
}
catch {
    // ignore if console input is not available
}
