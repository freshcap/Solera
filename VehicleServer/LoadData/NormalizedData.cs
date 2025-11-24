using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Data.SqlClient;

namespace LoadData;

public class NormalizedData
{
    public static void Process(string connectionString, string csvFilePath, string tableName)
    {
        // Read CSV and normalize data
        DataTable dataTable = new DataTable();
        
        // Create normalized columns - adjust these based on your actual non-quarter columns
        dataTable.Columns.Add("Year", typeof(int));
        dataTable.Columns.Add("Quarter", typeof(int));
        
        List<string> staticColumns = new List<string>();
        List<(string columnName, int year, int quarter)> quarterColumns = new List<(string, int, int)>();
        
        using (var reader = new StreamReader(csvFilePath))
        using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
        {
            // Read header and identify column types
            csv.Read();
            csv.ReadHeader();
            var headers = csv.HeaderRecord;
            
            foreach (var header in headers)
            {
                // Check if column is a year/quarter column (e.g., "2024 Q1")
                var match = Regex.Match(header, @"(\d{4})\s+Q([1-4])");
                if (match.Success)
                {
                    int year = int.Parse(match.Groups[1].Value);
                    int quarter = int.Parse(match.Groups[2].Value);
                    quarterColumns.Add((header, year, quarter));
                }
                else
                {
                    // Static column (non-quarter data)
                    staticColumns.Add(header);
                    dataTable.Columns.Add(header);
                }
            }
            
            Console.WriteLine($"Found {staticColumns.Count} static columns and {quarterColumns.Count} quarter columns");
            
            // Read data rows and normalize
            int rowCount = 0;
            while (csv.Read())
            {
                // For each quarter column, create a separate row
                foreach (var (columnName, year, quarter) in quarterColumns)
                {
                    var row = dataTable.NewRow();
                    
                    // Set Year and Quarter
                    row["Year"] = year;
                    row["Quarter"] = quarter;
                    
                    // Copy static column values
                    foreach (var staticColumn in staticColumns)
                    {
                        row[staticColumn] = csv.GetField(staticColumn);
                    }
                    
                    // Add the quarter-specific value
                    var quarterValue = csv.GetField(columnName);
                    if (!dataTable.Columns.Contains("Value"))
                    {
                        dataTable.Columns.Add("Value");
                    }
                    row["Value"] = quarterValue;
                    
                    dataTable.Rows.Add(row);
                }
                rowCount++;
            }
            
            Console.WriteLine($"Read {rowCount} CSV rows, created {dataTable.Rows.Count} normalized rows");
        }
        
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
        
        Console.WriteLine($"Successfully imported {dataTable.Rows.Count} normalized rows into {tableName} table");
    }
}