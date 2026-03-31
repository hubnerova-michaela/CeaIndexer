using CeaIndexer.Data;
using CeaIndexer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.EntityFrameworkCore;

namespace CeaIndexer.Views
{

    public partial class DatabaseExplorerView : UserControl
    {
        private List<MeasurePoint> _allData = new List<MeasurePoint>();

        public DatabaseExplorerView()
        {
            InitializeComponent();
            LoadDataFromDatabase();
        }

        private async void LoadDataFromDatabase()
        {
            TxtStats.Text = "Načítám data z databáze...";

            using (var db = new AppDbContext())
            {
                db.Database.EnsureCreated();

                _allData = await db.MeasurePoints
                    .Include(m => m.FileEntry)
                    .Include(m => m.Archives)
                        .ThenInclude(a => a.Quantities)
                    .ToListAsync();
            }

            CmbFilterFile.ItemsSource = _allData.Select(x => x.FileEntry?.FileName).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToList();
            CmbFilterDevice.ItemsSource = _allData.Select(x => x.DeviceType).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToList();
            CmbFilterQuantity.ItemsSource = _allData
                .SelectMany(m => m.Archives)   
                .SelectMany(a => a.Quantities)      
                .Select(q => q.Name)                
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct()                         
                .OrderBy(x => x)                   
                .ToList();

            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (_allData == null || _allData.Count == 0) return;

            var query = _allData.AsEnumerable();

            string fileFilter = CmbFilterFile.Text?.ToLower();
            if (!string.IsNullOrWhiteSpace(fileFilter))
                query = query.Where(m => m.FileEntry != null && m.FileEntry.FileName.ToLower().Contains(fileFilter));

            string deviceFilter = CmbFilterDevice.Text?.ToLower();
            if (!string.IsNullOrWhiteSpace(deviceFilter))
                query = query.Where(m => m.DeviceType != null && m.DeviceType.ToLower().Contains(deviceFilter));

            string qtyFilter = CmbFilterQuantity.Text?.ToLower();
            if (!string.IsNullOrWhiteSpace(qtyFilter))
            {
                query = query.Where(m => m.Archives.Any(a => a.Quantities.Any(q => q.Name.ToLower().Contains(qtyFilter))));
            }

            string searchFilter = TxtSearch.Text?.ToLower();
            if (!string.IsNullOrWhiteSpace(searchFilter))
                query = query.Where(m =>
                    (m.Name != null && m.Name.ToLower().Contains(searchFilter)) ||
                    (m.SerialNumber != null && m.SerialNumber.ToLower().Contains(searchFilter))
                );

            var result = query.ToList();
            GridMeasurePoints.ItemsSource = result;
            TxtStats.Text = $"Zobrazeno {result.Count} z celkových {_allData.Count} přístrojů.";
        }

        private void Filter_Changed(object sender, RoutedEventArgs e) => ApplyFilters();
        private void Filter_Changed(object sender, KeyEventArgs e) => ApplyFilters();

        private void BtnClearFilters_Click(object sender, RoutedEventArgs e)
        {
            CmbFilterFile.Text = "";
            CmbFilterDevice.Text = "";
            CmbFilterQuantity.Text = "";
            TxtSearch.Text = "";
            ApplyFilters();
        }

        private void GridMeasurePoints_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GridMeasurePoints.SelectedItem is MeasurePoint selectedPoint)
            {
                GridIntervals.ItemsSource = selectedPoint.Archives;
                LstQuantities.ItemsSource = selectedPoint.Archives.SelectMany(a => a.Quantities).ToList();
            }
            else
            {
                GridIntervals.ItemsSource = null;
                LstQuantities.ItemsSource = null;
            }
        }
    }
}
