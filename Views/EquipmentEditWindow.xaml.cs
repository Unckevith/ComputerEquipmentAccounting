using System;
using System.Data;
using System.Windows;
using ComputerEquipmentAccounting.Services;

namespace ComputerEquipmentAccounting.Views
{
    public partial class EquipmentEditWindow : Window
    {
        private readonly DatabaseService _dbService;

        public EquipmentEditWindow()
        {
            InitializeComponent();
            _dbService = new DatabaseService();
            LoadComboBoxes();
        }

        private void LoadComboBoxes()
        {
            // Типы оборудования
            var types = _dbService.GetEquipmentTypes();
            cmbType.ItemsSource = types.DefaultView;
            cmbType.DisplayMemberPath = "TypeName";
            cmbType.SelectedValuePath = "Id";
            if (types.Rows.Count > 0) cmbType.SelectedIndex = 0;

            // Местоположения (конкатенация в C#, а не в SQL)
            var locations = _dbService.GetLocations();
            locations.Columns.Add("FullAddress", typeof(string), "Building + ', каб.' + RoomNumber");
            cmbLocation.ItemsSource = locations.DefaultView;
            cmbLocation.DisplayMemberPath = "FullAddress";
            cmbLocation.SelectedValuePath = "Id";
            if (locations.Rows.Count > 0) cmbLocation.SelectedIndex = 0;

            // Ответственные лица
            var persons = _dbService.GetResponsiblePersons();
            persons.Columns.Add("FullName", typeof(string), "LastName + ' ' + FirstName");
            cmbResponsible.ItemsSource = persons.DefaultView;
            cmbResponsible.DisplayMemberPath = "FullName";
            cmbResponsible.SelectedValuePath = "Id";
            if (persons.Rows.Count > 0) cmbResponsible.SelectedIndex = 0;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Проверка обязательных полей
            if (string.IsNullOrWhiteSpace(txtInvNum.Text))
            {
                MessageBox.Show("Введите инвентарный номер", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Введите наименование", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Проверка уникальности инвентарного номера
            if (_dbService.IsInventoryNumberExists(txtInvNum.Text))
            {
                MessageBox.Show("Инвентарный номер уже существует", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Получение значений с преобразованием из long в int
            int typeId = Convert.ToInt32(cmbType.SelectedValue);
            int locationId = Convert.ToInt32(cmbLocation.SelectedValue);
            int responsibleId = Convert.ToInt32(cmbResponsible.SelectedValue);

            // Стоимость
            decimal cost = 0;
            if (!string.IsNullOrWhiteSpace(txtCost.Text))
            {
                decimal.TryParse(txtCost.Text, out cost);
            }

            // Добавление оборудования
            bool result = _dbService.AddEquipment(
                txtInvNum.Text,           // inventoryNumber
                "",                       // serialNumber
                txtName.Text,             // name
                typeId,                   // typeId
                "",                       // manufacturer
                txtModel.Text,            // model
                "",                       // processor
                "",                       // ram
                "",                       // storage
                "",                       // os
                null,                     // acquisitionDate
                null,                     // commissioningDate
                cost,                     // cost
                null,                     // usefulLife
                "в эксплуатации",         // status
                locationId,               // locationId
                responsibleId,            // responsiblePersonId
                ""                        // notes
            );

            if (result)
            {
                MessageBox.Show("Оборудование успешно добавлено", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Ошибка при добавлении оборудования", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}