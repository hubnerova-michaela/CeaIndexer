using CeaIndexer.Data;
using CeaIndexer.FilterModels;
using CeaIndexer.Helpers;
using CeaIndexer.Services;     
using CeaIndexer.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace CeaIndexer.Views
{

    public partial class ConditionBuilderView : UserControl
    {
        public ObservableCollection<QuantityNode> QuantitiesTree { get; set; } = new ObservableCollection<QuantityNode>();

        public ConditionGroup RootCondition { get; set; }

        public Array AvailableCategories => Enum.GetValues(typeof(RuleCategory));
        public Array AvailableOperators => Enum.GetValues(typeof(RelationalOperator));

        public List<EnumDisplayItem> AvailableLogicOperators { get; } = new List<EnumDisplayItem>
        {
            new EnumDisplayItem { Value = LogicalOperator.And, DisplayName = "Splnit VŠECHNY (AND)" },
            new EnumDisplayItem { Value = LogicalOperator.Or, DisplayName = "Splnit ALESPOŇ JEDNU (OR)" }
        };

        public ObservableCollection<QuantityNode> QuantityTree { get; set; }

        public ConditionBuilderView()
        {
            InitializeComponent();

            RootCondition = new ConditionGroup { LogicOperator = LogicalOperator.And };


            var dbQuantities = new List<string>
            {
                "main_U_avg_U1", "main_U_avg_U2", "main_U_avg_U3",
                "main_I_avg_I1", "main_I_avg_I2", "main_I_avg_I3",
                "pqmain_freq"
            };
            QuantityTree = TreeHelper.BuildQuantityTree(dbQuantities);

            this.DataContext = this;
            RuleItemsControl.ItemsSource = RootCondition.Children;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var dbContext = new AppDbContext())
                {
                    var searchEngine = new SearchEngineService(dbContext);
                    var autocompleteData = await searchEngine.GetAutocompleteDataAsync();

                    ConditionRule.GlobalAutocompleteCache = autocompleteData;

                    if (RootCondition.Children.FirstOrDefault() is ConditionRule firstRule)
                    {
                        var temp = firstRule.TargetProperty;
                        firstRule.TargetProperty = null;
                        firstRule.TargetProperty = temp;
                    }
                }


                await LoadQuantitiesTreeAsync();

                DetailOperatorCombo.ItemsSource = new Dictionary<RelationalOperator, string>
        {
            { RelationalOperator.Equals, "Rovná se (=)" },
            { RelationalOperator.GreaterThan, "Je větší než (>)" },
            { RelationalOperator.LessThan, "Je menší než (<)" }
        }.ToList();
                DetailOperatorCombo.DisplayMemberPath = "Value";
                DetailOperatorCombo.SelectedValuePath = "Key";

                DetailTimeCombo.ItemsSource = new Dictionary<TimeFilterType, string>
        {
            { TimeFilterType.None, "Kdykoliv" },
            { TimeFilterType.Today, "Dnes" },
            { TimeFilterType.Yesterday, "Včera" },
            { TimeFilterType.LastWeek, "Poslední týden" },
            { TimeFilterType.LastMonth, "Poslední měsíc" },
            { TimeFilterType.LastYear, "Poslední rok" },
            { TimeFilterType.CustomInterval, "Vlastní interval (Od - Do)" }
        }.ToList();
                DetailTimeCombo.DisplayMemberPath = "Value";
                DetailTimeCombo.SelectedValuePath = "Key";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Chyba v UserControl_Loaded: {ex.Message}");
            }
        }

        private void BtnAddRule_Click(object sender, RoutedEventArgs e)
        {
            RootCondition.Children.Add(new ConditionRule());
        }

        private async void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            List<QuantityCondition> quantityConditions = TreeHelper.GetSelectedQuantities(QuantityTree);

            if (RootCondition.Children.Count == 0 && quantityConditions.Count == 0)
            {
                MessageBox.Show("Zadejte alespoň jednu textovou podmínku nahoře nebo vyberte veličinu ze stromu dole.", "Upozornění", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                using (var dbContext = new AppDbContext())
                {
                    var searchEngine = new SearchEngineService(dbContext);
                    var foundFiles = await searchEngine.ExecuteSearchAsync(RootCondition, quantityConditions);
                    ResultsGrid.ItemsSource = foundFiles;
                    if (foundFiles == null || foundFiles.Count == 0)
                    {
                        MessageBox.Show("Zadaným podmínkám nevyhovují žádné soubory.", "Výsledek", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Chyba při hledání:\n{ex.Message}", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }


        private void BtnAddRuleToGroup_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.DataContext is ConditionGroup targetGroup)
            {
                targetGroup.Children.Add(new ConditionRule());
            }
        }

        private void BtnAddGroupToGroup_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.DataContext is ConditionGroup targetGroup)
            {
                targetGroup.Children.Add(new ConditionGroup { LogicOperator = LogicalOperator.Or });
            }
        }

        private void BtnDeleteNode_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.DataContext is IConditionNode nodeToDelete)
            {
                RemoveNodeRecursive(RootCondition, nodeToDelete);
            }
        }

        private bool RemoveNodeRecursive(ConditionGroup currentGroup, IConditionNode nodeToRemove)
        {
            if (currentGroup.Children.Contains(nodeToRemove))
            {
                currentGroup.Children.Remove(nodeToRemove);
                return true;
            }

            foreach (var childGroup in currentGroup.Children.OfType<ConditionGroup>())
            {
                if (RemoveNodeRecursive(childGroup, nodeToRemove))
                    return true;
            }

            return false;
        }

        private void BtnAddRootGroup_Click(object sender, RoutedEventArgs e)
        {
            RootCondition.Children.Add(new ConditionGroup { LogicOperator = LogicalOperator.Or });
        }

        private void BtnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            if (ResultsGrid.SelectedItem == null)
            {
                System.Windows.MessageBox.Show("Nejprve vyberte soubor ze seznamu nalezených.", "Upozornění", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string filePath = ResultsGrid.SelectedItem.ToString();

            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    var processInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    };
                    System.Diagnostics.Process.Start(processInfo);
                }
                else
                {
                    System.Windows.MessageBox.Show($"Soubor nebyl na disku nalezen. Možná byl mezitím smazán nebo přesunut.\nCesta: {filePath}", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Nepodařilo se otevřít soubor:\n{ex.Message}", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private async Task LoadQuantitiesTreeAsync()
        {
            try
            {
                List<string> rawQuantities;

                using (var dbContext = new AppDbContext())
                {
                    rawQuantities = await dbContext.Quantities
                        .Select(q => q.Name)
                        .Distinct()
                        .ToListAsync();
                }

                if (!rawQuantities.Any())
                {
                    rawQuantities = new List<string> { "ZATÍM_PRÁZDNÁ_DB", "zkus_naimportovat_soubory" };
                }

                QuantityTree = TreeHelper.BuildQuantityTree(rawQuantities);

                QuantitiesTreeView.ItemsSource = QuantityTree;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Chyba při načítání veličin z databáze:\n{ex.Message}", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
