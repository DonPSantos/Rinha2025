using Microsoft.Data.Sqlite;
using Rinha2025.DTO;

namespace Rinha2025
{
    public class PaymentRepository
    {
        public PaymentRepository()
        {
        }

        public async Task SaveDefault(ProcessorRequest processorRequest)
        {
            using var connection = new SqliteConnection($"Data Source={Environment.GetEnvironmentVariable("DB_PATH")}");

            await connection.OpenAsync();

            var command = connection.CreateCommand();

            command.CommandText = "INSERT INTO paymentDefault (CorrelationId,Amount,RequestedAt) VALUES ($CorrelationId,$Amount,$RequestedAt)";
            command.Parameters.AddWithValue("$CorrelationId", processorRequest.CorrelationId);
            command.Parameters.AddWithValue("$Amount", processorRequest.Amount);
            command.Parameters.AddWithValue("$RequestedAt", processorRequest.RequestedAt);

            await command.ExecuteNonQueryAsync();
        }

        public async Task SaveFallback(ProcessorRequest processorRequest)
        {
            using var connection = new SqliteConnection($"Data Source={Environment.GetEnvironmentVariable("DB_PATH")}");

            await connection.OpenAsync();

            var command = connection.CreateCommand();

            command.CommandText = "INSERT INTO paymentFallback (CorrelationId,Amount,RequestedAt) VALUES ($CorrelationId,$Amount,$RequestedAt)";
            command.Parameters.AddWithValue("$CorrelationId", processorRequest.CorrelationId);
            command.Parameters.AddWithValue("$Amount", processorRequest.Amount);
            command.Parameters.AddWithValue("$RequestedAt", processorRequest.RequestedAt);

            await command.ExecuteNonQueryAsync();
        }

        //public void SelectPayments()
        //{
        //    using var connection = new SqliteConnection("Data Source=mydb.db");
        //    connection.Open();
        //    var command = connection.CreateCommand();
        //    command.CommandText = "SELECT CorrelationId, Amount, RequestedAt FROM payment";
        //    using var reader = command.ExecuteReader();
        //    while (reader.Read())
        //    {
        //        var correlationId = reader.GetGuid(0);
        //        var amount = reader.GetDecimal(1);
        //        var requestedAt = reader.GetDateTime(2);
        //        Console.WriteLine($"CorrelationId: {correlationId}, Amount: {amount}, RequestedAt: {requestedAt}");
        //    }
        //}
    }
}