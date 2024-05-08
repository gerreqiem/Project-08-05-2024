using System.Data.SQLite;
using System.Windows;
using System;

namespace MetalFactoryApp
{
    public partial class MainWindow : Window
    {
        private string connectionString = "Data Source=metal_factory7.db;Version=3;";
        private readonly object dbLock = new object();
        private SQLiteConnection connection; // Общее соединение

        public MainWindow()
        {
            InitializeComponent();
            connection = new SQLiteConnection(connectionString);
            connection.Open();
            CreateDatabase();
        }

        private void CreateDatabase()
        {
            string sql = "CREATE TABLE IF NOT EXISTS Inventory (Item TEXT, Quantity INTEGER)";
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();

            string[] initialItems = { "Саморезы", "Большая дверь", "Дверь тумбочки", "Полка" };
            foreach (string item in initialItems)
            {
                sql = "INSERT INTO Inventory (Item, Quantity) VALUES (@Item, 100)";
                command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@Item", item);
                command.ExecuteNonQuery();
            }
        }

        private void btnCreateDatabase_Click(object sender, RoutedEventArgs e)
        {
            CreateDatabase();
            MessageBox.Show("База данных успешно создана.");
        }

        private void btnUpdateInventory_Click(object sender, RoutedEventArgs e)
        {
            string item = txtItem.Text;
            int quantity = Convert.ToInt32(txtQuantity.Text);
            UpdateInventory(item, quantity);
            MessageBox.Show($"Инвентарь обновлён для {item}.");
        }

        private void btnGetQuantity_Click(object sender, RoutedEventArgs e)
        {
            string item = txtItem.Text;
            int quantity = GetQuantity(item);
            txtResult.Text = $"Количество {item}: {quantity}";
        }

        private void btnMakeWorkbench_Click(object sender, RoutedEventArgs e)
        {
            MakeItem("Верстак", "Саморезы", 10, "Большая дверь", 2, "Полка", 2);
        }

        private void btnMakeCabinet_Click(object sender, RoutedEventArgs e)
        {
            MakeItem("Архивный шкаф", "Саморезы", 20, "Большая дверь", 2, "Полка", 5);
        }

        private void btnUnlimitedResources_Click(object sender, RoutedEventArgs e)
        {
            int screwsAvailable = GetQuantity("Саморезы");
            int shelvesAvailable = GetQuantity("Полка");

            int screwsRequired = 10;
            int shelvesRequired = 2;
            int screwsRequired1 = 20;
            int shelvesRequired1 = 5;

            int screwsNeeded = Math.Max(0, screwsRequired - screwsAvailable);
            int shelvesNeeded = Math.Max(0, shelvesRequired - shelvesAvailable);

            int screwsNeeded1 = Math.Max(0, screwsRequired1 - screwsAvailable);
            int shelvesNeeded1 = Math.Max(0, shelvesRequired1 - shelvesAvailable);

            if (screwsNeeded > 0 || shelvesNeeded > 0)
            {
                string message = $"Для изготовления верстака не хватает: ";
                if (screwsNeeded > 0)
                {
                    message += $"{screwsNeeded} саморезов";
                    if (shelvesNeeded > 0)
                    {
                        message += " и ";
                    }
                }
                if (shelvesNeeded > 0)
                {
                    message += $"{shelvesNeeded} полок";
                }
                MessageBox.Show(message);
            }
            else
            {
                MessageBox.Show("Для изготовления верстака достаточно ресурсов.");
            }
            if (screwsNeeded1 > 0 || shelvesNeeded1 > 0)
            {
                string message = $"Для изготовления архивного шкафа не хватает: ";
                if (screwsNeeded1> 0)
                {
                    message += $"{screwsNeeded1} саморезов";
                    if (shelvesNeeded1 > 0)
                    {
                        message += " и ";
                    }
                }
                if (shelvesNeeded1 > 0)
                {
                    message += $"{shelvesNeeded1} полок";
                }
                MessageBox.Show(message);
            }
            else
            {
                MessageBox.Show("Для изготовления архивного шкафа достаточно ресурсов.");
            }
        }
        private void UpdateInventory(string item, int quantity)
        {
            lock (dbLock)
            {
                using (SQLiteTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string sql = "UPDATE Inventory SET Quantity = @Quantity WHERE Item = @Item";
                        SQLiteCommand command = new SQLiteCommand(sql, connection, transaction);
                        command.Parameters.AddWithValue("@Item", item);
                        command.Parameters.AddWithValue("@Quantity", quantity);
                        command.ExecuteNonQuery();
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show($"Ошибка: {ex.Message}");
                    }
                }
            }
        }
        private int GetQuantity(string item)
        {
            int quantity = 0;

            lock (dbLock)
            {
                using (SQLiteTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string sql = "SELECT Quantity FROM Inventory WHERE Item = @Item";
                        SQLiteCommand command = new SQLiteCommand(sql, connection, transaction);
                        command.Parameters.AddWithValue("@Item", item);
                        SQLiteDataReader reader = command.ExecuteReader();
                        if (reader.Read())
                        {
                            quantity = Convert.ToInt32(reader["Quantity"]);
                        }
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show($"Ошибка: {ex.Message}");
                    }
                }
            }

            return quantity;
        }

        private void MakeItem(string itemName, params object[] materials)
        {
            lock (dbLock)
            {
                using (SQLiteTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        bool enoughMaterials = true;
                        for (int i = 0; i < materials.Length; i += 2)
                        {
                            string materialName = (string)materials[i];
                            int materialQuantity = (int)materials[i + 1];
                            int currentQuantity = GetQuantity(materialName);
                            if (currentQuantity < materialQuantity)
                            {
                                enoughMaterials = false;
                                break;
                            }
                        }
                        if (enoughMaterials)
                        {
                            for (int i = 0; i < materials.Length; i += 2)
                            {
                                string materialName = (string)materials[i];
                                int materialQuantity = (int)materials[i + 1];
                                UpdateInventory(materialName, GetQuantity(materialName) - materialQuantity);
                            }
                            MessageBox.Show($"{itemName} успешно создан.");
                        }
                        else
                        {
                            MessageBox.Show($"Недостаточно материалов для создания {itemName}.");
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show($"Ошибка: {ex.Message}");
                    }
                }
            }
        }
    }
}