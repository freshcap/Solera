using LoadData;

string connectionString = @"Server=(localdb)\MSSQLLocalDB;Database=Solera;Integrated Security=true;TrustServerCertificate=True;";
string csvFilePath = "df_VEH0120_GB.csv";
string rawTableName = "VehicleDataRaw";
string normalizedTableName = "VehicleData";

Console.WriteLine("Starting CSV import...");

try
{
    // Import raw data
    //RawData.Process(connectionString, csvFilePath, rawTableName);
    
    // Import normalized data
    Console.WriteLine("\nStarting normalized import...");
    NormalizedData.Process(connectionString, csvFilePath, normalizedTableName);
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
}
