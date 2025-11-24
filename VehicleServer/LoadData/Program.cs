using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;
using CsvHelper;
using CsvHelper.Configuration;
using System.Text.RegularExpressions;

string connectionString = @"Server=(localdb)\MSSQLLocalDB;Database=Solera;Integrated Security=true;TrustServerCertificate=True;";
string csvFilePath = "df_VEH0120_GB.csv";
string tableName = "VehicleData";

Console.WriteLine("Starting CSV import...");

try
{
    // Read CSV into DataTable
    DataTable dataTable = new DataTable();
    
    using (var reader = new StreamReader(csvFilePath))
    using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
    {
        // Read header and create columns with underscores instead of spaces
        csv.Read();
        csv.ReadHeader();
        var headers = csv.HeaderRecord;
        
        foreach (var header in headers)
        {
            string columnName = header;

            // Change 2024 Q1 to Q1_2024
            if (Regex.IsMatch(header, @"\d{4} Q[1-4]"))
            {
                columnName = $"{header[5..]}_{header[..4]}";
            }
            dataTable.Columns.Add(columnName);
        }
        
        // Read data rows
        while (csv.Read())
        {
            var row = dataTable.NewRow();
            for (int i = 0; i < headers.Length; i++)
            {
                row[i] = csv.GetField(i);
            }
            dataTable.Rows.Add(row);
        }
    }
    
    Console.WriteLine($"Read {dataTable.Rows.Count} rows from CSV");
    
    // Bulk insert into SQL Server
    using (SqlConnection connection = new SqlConnection(connectionString))
    {
        connection.Open();
        
        using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection))
        {
            bulkCopy.DestinationTableName = tableName;
            bulkCopy.BulkCopyTimeout = 300;
            
            // Map columns
            foreach (DataColumn column in dataTable.Columns)
            {
                bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
            }
            
            bulkCopy.WriteToServer(dataTable);
        }
    }
    
    Console.WriteLine($"Successfully imported {dataTable.Rows.Count} rows into {tableName} table");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
}
