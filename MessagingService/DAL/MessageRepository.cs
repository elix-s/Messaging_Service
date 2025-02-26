using MessagingService.Models;
using Npgsql;

namespace MessagingService.DAL
{
    public class MessageRepository
    {
        private readonly ILogger<MessageRepository> _logger;
        private readonly string _connectionString;

        public MessageRepository(ILogger<MessageRepository> logger, IConfiguration configuration)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? "Host=localhost;Port=5432;Database=messagesdb;Username=postgres;Password=password";
        }
        
        public void InitDatabase()
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    
                    string sql = @"
                        CREATE TABLE IF NOT EXISTS Messages (
                            Id SERIAL PRIMARY KEY,
                            MessageText VARCHAR(128) NOT NULL,
                            Timestamp TIMESTAMP NOT NULL,
                            OrderNumber INT NOT NULL
                        );";
                    
                    using (var command = new NpgsqlCommand(sql, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                    
                    _logger.LogInformation("Table 'Messages' created.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database initialization failed.");
                throw;
            }
        }
        
        public void InsertMessage(Message message)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    string sql = "INSERT INTO Messages(MessageText, Timestamp, OrderNumber) VALUES(@text, @timestamp, @orderNumber)";
                    using (var command = new NpgsqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@text", message.Text);
                        command.Parameters.AddWithValue("@timestamp", message.Timestamp);
                        command.Parameters.AddWithValue("@orderNumber", message.OrderNumber);
                        
                        int rows = command.ExecuteNonQuery();
                        _logger.LogInformation("Message inserted into DB (number of affected rows: {Rows}).", rows);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting message into database.");
            }
        }

        // Метод для получения сообщений за указанный период
        public List<Message> GetMessages(DateTime start, DateTime end)
        {
            var messages = new List<Message>();
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    string sql = "SELECT MessageText, Timestamp, OrderNumber FROM Messages WHERE Timestamp >= @start AND Timestamp <= @end ORDER BY Timestamp ASC";
                    
                    using (var command = new NpgsqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@start", start);
                        command.Parameters.AddWithValue("@end", end);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                messages.Add(new Message
                                {
                                    Text = reader.GetString(0),
                                    Timestamp = reader.GetDateTime(1),
                                    OrderNumber = reader.GetInt32(2)
                                });
                            }
                        }
                    }
                    
                    _logger.LogInformation("Received {Count} messages from BD.", messages.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading messages from the database.");
            }
            
            return messages;
        }
    }
}