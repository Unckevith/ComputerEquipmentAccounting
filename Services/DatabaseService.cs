using System;
using System.Data;
using System.Data.SQLite;

namespace ComputerEquipmentAccounting.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService()
        {
            _connectionString = App.ConnectionString;
        }

        // ========== СПРАВОЧНИКИ ==========

        public DataTable GetEquipmentTypes()
        {
            string query = "SELECT Id, TypeName FROM EquipmentTypes ORDER BY TypeName";
            using (var conn = new SQLiteConnection(_connectionString))
            using (var adapter = new SQLiteDataAdapter(query, conn))
            {
                var dt = new DataTable();
                conn.Open();
                adapter.Fill(dt);
                return dt;
            }
        }

        public DataTable GetLocations()
        {
            string query = "SELECT Id, Building, Floor, RoomNumber FROM Locations ORDER BY Building, Floor, RoomNumber";
            using (var conn = new SQLiteConnection(_connectionString))
            using (var adapter = new SQLiteDataAdapter(query, conn))
            {
                var dt = new DataTable();
                conn.Open();
                adapter.Fill(dt);
                return dt;
            }
        }

        public DataTable GetResponsiblePersons()
        {
            string query = "SELECT Id, LastName, FirstName FROM ResponsiblePersons ORDER BY LastName, FirstName";
            using (var conn = new SQLiteConnection(_connectionString))
            using (var adapter = new SQLiteDataAdapter(query, conn))
            {
                var dt = new DataTable();
                conn.Open();
                adapter.Fill(dt);
                return dt;
            }
        }

        // ========== ОБОРУДОВАНИЕ ==========

        public DataTable GetAllEquipment()
        {
            string query = @"
                SELECT 
                    e.Id,
                    e.InventoryNumber,
                    e.Name,
                    et.TypeName AS EquipmentType,
                    e.Status,
                    l.Building || ', каб.' || l.RoomNumber AS Location,
                    rp.LastName || ' ' || rp.FirstName AS ResponsiblePerson,
                    e.Cost
                FROM Equipment e
                INNER JOIN EquipmentTypes et ON e.TypeId = et.Id
                INNER JOIN Locations l ON e.LocationId = l.Id
                INNER JOIN ResponsiblePersons rp ON e.ResponsiblePersonId = rp.Id
                ORDER BY e.Name";

            using (var conn = new SQLiteConnection(_connectionString))
            using (var adapter = new SQLiteDataAdapter(query, conn))
            {
                var dt = new DataTable();
                conn.Open();
                adapter.Fill(dt);
                return dt;
            }
        }

        public DataTable SearchEquipment(string searchText)
        {
            string query = @"
                SELECT 
                    e.Id,
                    e.InventoryNumber,
                    e.Name,
                    et.TypeName AS EquipmentType,
                    e.Status,
                    l.Building || ', каб.' || l.RoomNumber AS Location,
                    rp.LastName || ' ' || rp.FirstName AS ResponsiblePerson
                FROM Equipment e
                INNER JOIN EquipmentTypes et ON e.TypeId = et.Id
                INNER JOIN Locations l ON e.LocationId = l.Id
                INNER JOIN ResponsiblePersons rp ON e.ResponsiblePersonId = rp.Id
                WHERE e.InventoryNumber LIKE @Search OR e.Name LIKE @Search
                ORDER BY e.Name";

            using (var conn = new SQLiteConnection(_connectionString))
            using (var cmd = new SQLiteCommand(query, conn))
            using (var adapter = new SQLiteDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("@Search", $"%{searchText}%");
                var dt = new DataTable();
                conn.Open();
                adapter.Fill(dt);
                return dt;
            }
        }

        public bool IsInventoryNumberExists(string inventoryNumber)
        {
            string query = "SELECT COUNT(*) FROM Equipment WHERE InventoryNumber = @InventoryNumber";
            using (var conn = new SQLiteConnection(_connectionString))
            using (var cmd = new SQLiteCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@InventoryNumber", inventoryNumber);
                conn.Open();
                long count = (long)cmd.ExecuteScalar();
                return count > 0;
            }
        }

        public bool AddEquipment(
            string inventoryNumber, string serialNumber, string name, int typeId,
            string manufacturer, string model, string processor, string ram, string storage, string os,
            DateTime? acquisitionDate, DateTime? commissioningDate, decimal cost, int? usefulLife,
            string status, int locationId, int responsiblePersonId, string notes)
        {
            string query = @"
                INSERT INTO Equipment 
                (InventoryNumber, Name, TypeId, Status, LocationId, ResponsiblePersonId, Cost)
                VALUES 
                (@InventoryNumber, @Name, @TypeId, @Status, @LocationId, @ResponsiblePersonId, @Cost)";

            using (var conn = new SQLiteConnection(_connectionString))
            using (var cmd = new SQLiteCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@InventoryNumber", inventoryNumber);
                cmd.Parameters.AddWithValue("@Name", name);
                cmd.Parameters.AddWithValue("@TypeId", typeId);
                cmd.Parameters.AddWithValue("@Status", status);
                cmd.Parameters.AddWithValue("@LocationId", locationId);
                cmd.Parameters.AddWithValue("@ResponsiblePersonId", responsiblePersonId);
                cmd.Parameters.AddWithValue("@Cost", cost);

                conn.Open();
                int result = cmd.ExecuteNonQuery();
                return result > 0;
            }
        }

        public bool MoveEquipment(int equipmentId, int newLocationId, string reason)
        {
            string getOldQuery = "SELECT LocationId FROM Equipment WHERE Id = @Id";
            int oldLocationId = 0;

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(getOldQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", equipmentId);
                    oldLocationId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                if (oldLocationId == newLocationId)
                    return false;

                string updateQuery = "UPDATE Equipment SET LocationId = @NewLocationId WHERE Id = @Id";
                using (var cmd = new SQLiteCommand(updateQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@NewLocationId", newLocationId);
                    cmd.Parameters.AddWithValue("@Id", equipmentId);
                    cmd.ExecuteNonQuery();
                }

                string historyQuery = @"
                    INSERT INTO MovementHistory (EquipmentId, OldLocationId, NewLocationId, MoveDate, Reason, UserName)
                    VALUES (@EquipmentId, @OldLocationId, @NewLocationId, @MoveDate, @Reason, @UserName)";

                using (var cmd = new SQLiteCommand(historyQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@EquipmentId", equipmentId);
                    cmd.Parameters.AddWithValue("@OldLocationId", oldLocationId);
                    cmd.Parameters.AddWithValue("@NewLocationId", newLocationId);
                    cmd.Parameters.AddWithValue("@MoveDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@Reason", reason ?? "");
                    cmd.Parameters.AddWithValue("@UserName", "Пользователь");
                    cmd.ExecuteNonQuery();
                }
            }
            return true;
        }

        public bool UpdateEquipmentStatus(int equipmentId, string newStatus)
        {
            string query = "UPDATE Equipment SET Status = @Status WHERE Id = @Id";
            using (var conn = new SQLiteConnection(_connectionString))
            using (var cmd = new SQLiteCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@Status", newStatus);
                cmd.Parameters.AddWithValue("@Id", equipmentId);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        // ========== ЗАЯВКИ НА РЕМОНТ ==========

        public bool CreateRepairRequest(int equipmentId, string issueDescription, string priority)
        {
            string query = @"
                INSERT INTO RepairRequests (EquipmentId, IssueDescription, Priority, Status, CreatedDate)
                VALUES (@EquipmentId, @IssueDescription, @Priority, 'новая', @CreatedDate)";

            using (var conn = new SQLiteConnection(_connectionString))
            using (var cmd = new SQLiteCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@EquipmentId", equipmentId);
                cmd.Parameters.AddWithValue("@IssueDescription", issueDescription);
                cmd.Parameters.AddWithValue("@Priority", priority);
                cmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                conn.Open();
                int result = cmd.ExecuteNonQuery();

                if (result > 0)
                {
                    UpdateEquipmentStatus(equipmentId, "на ремонте");
                }
                return result > 0;
            }
        }

        public DataTable GetRepairRequests()
        {
            string query = @"
                SELECT 
                    rr.Id,
                    e.InventoryNumber || ' - ' || e.Name AS Equipment,
                    rr.IssueDescription,
                    rr.CreatedDate,
                    rr.Priority,
                    rr.Status
                FROM RepairRequests rr
                INNER JOIN Equipment e ON rr.EquipmentId = e.Id
                ORDER BY rr.CreatedDate DESC";

            using (var conn = new SQLiteConnection(_connectionString))
            using (var adapter = new SQLiteDataAdapter(query, conn))
            {
                var dt = new DataTable();
                conn.Open();
                adapter.Fill(dt);
                return dt;
            }
        }
    }
}