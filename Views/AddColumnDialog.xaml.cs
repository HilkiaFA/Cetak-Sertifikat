using System.Windows;

namespace CetakSertifikatApp.Views
{
    public partial class AddColumnDialog : Window
    {
        public string ColumnName { get; private set; } = string.Empty;

        public AddColumnDialog()
        {
            InitializeComponent();
            TxtColumnName.Focus();
            TxtColumnName.SelectAll();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            string name = TxtColumnName.Text.Trim().Trim('{', '}').Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Nama kolom tidak boleh kosong.", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ColumnName = name;
            DialogResult = true;
            Close();
        }
    }
}
