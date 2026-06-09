using System.Data.SQLite;

namespace ComputerEquipmentAccounting.Services
{
    public class AuthService
    {
        private readonly string _connectionString;

        public AuthService()
        {
            _connectionString = App.ConnectionString;
        }

        public User? Authenticate(string login, string password)
        {
            // Временный код: проверяем только логин, без пароля
            // Потом можешь вернуть проверку пароля, когда всё заработает
            string query = "SELECT Id, Login, FullName, Role FROM Users WHERE Login = @Login";

            using (var conn = new SQLiteConnection(_connectionString))
            using (var cmd = new SQLiteCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@Login", login);

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new User
                        {
                            Id = reader.GetInt32(0),
                            Login = reader.GetString(1),
                            FullName = reader.GetString(2),
                            Role = reader.GetString(3)
                        };
                    }
                }
            }
            return null;
        }
    }
}