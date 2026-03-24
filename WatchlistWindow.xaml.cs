using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace CeaIndexer
{
    public partial class WatchlistWindow : Window
    {
        private ObservableCollection<QuantityViewModel> _allQuantities;
        private ObservableCollection<QuantityViewModel> _filteredQuantities;

        public WatchlistWindow()
        {
            InitializeComponent();
            _allQuantities = new ObservableCollection<QuantityViewModel>();
            _filteredQuantities = new ObservableCollection<QuantityViewModel>();
            QuantitiesListBox.ItemsSource = _filteredQuantities;
            UpdateUIText();
        }

        public string[] GetSelectedQuantities()
        {
            return _allQuantities
                .Where(q => q.IsSelected)
                .Select(q => q.Name)
                .ToArray();
        }

        public void LoadQuantities(ObservableCollection<QuantityViewModel> quantities)
        {
            try
            {
                LoadingPanel.Visibility = Visibility.Visible;

                _allQuantities.Clear();

                foreach (var q in quantities)
                {
                    q.IsSelectedAtLoad = q.IsSelected;
                    q.IsPendingScan = false;
                    _allQuantities.Add(q);
                }

                UpdateFilteredQuantities();
                UpdateCountLabel();
            }
            finally
            {
                LoadingPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateUIText()
        {
            Title = LocalizationManager.GetString("WatchlistTitle");
            SearchLabel.Text = LocalizationManager.GetString("SearchQuantity");
            ShowSelectedOnlyCheckBox.Content = LocalizationManager.GetString("WatchlistShowSelectedOnly");
            SelectAllButton.Content = LocalizationManager.GetString("SelectAll");
            DeselectAllButton.Content = LocalizationManager.GetString("DeselectAll");
            CancelButton.Content = LocalizationManager.GetString("Cancel");
            SaveButton.Content = LocalizationManager.GetString("SaveSelection");
            LoadingTextBlock.Text = LocalizationManager.GetString("WatchlistLoadingQuantities");
            PendingLegendTextBlock.Text = LocalizationManager.GetString("WatchlistPendingLegend");
        }

        private void UpdateCountLabel()
        {
            int selected = _allQuantities.Count(q => q.IsSelected);
            CountLabel.Text = LocalizationManager.GetString("WatchlistCount", _filteredQuantities.Count, _allQuantities.Count, selected);
        }

        private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateFilteredQuantities();
            UpdateCountLabel();
        }

        private void UpdateFilteredQuantities()
        {
            var searchTerm = SearchBox.Text.Trim().ToLower();
            bool selectedOnly = ShowSelectedOnlyCheckBox.IsChecked == true;

            _filteredQuantities.Clear();

            var filtered = _allQuantities.Where(q =>
                (!selectedOnly || q.IsSelected) &&
                (string.IsNullOrEmpty(searchTerm) || q.Name.ToLower().Contains(searchTerm) || q.Archive.ToLower().Contains(searchTerm)));

            foreach (var q in filtered)
            {
                _filteredQuantities.Add(q);
            }
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var q in _allQuantities)
            {
                q.IsSelected = true;
            }

            UpdateFilteredQuantities();
            UpdateCountLabel();
        }

        private void DeselectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var q in _allQuantities)
            {
                q.IsSelected = false;
            }

            UpdateFilteredQuantities();
            UpdateCountLabel();
        }

        private void ShowSelectedOnlyCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateFilteredQuantities();
            UpdateCountLabel();
        }

        private void QuantitySelectionChanged(object sender, RoutedEventArgs e)
        {
            UpdateFilteredQuantities();
            UpdateCountLabel();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
