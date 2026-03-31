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
using CeaIndexer.FilterModels;
using CeaIndexer.Services;     
using CeaIndexer.Data;

namespace CeaIndexer.Views
{

    public partial class ConditionBuilderView : UserControl
    {

        public ConditionGroup RootCondition { get; set; }

        public Array AvailableCategories => Enum.GetValues(typeof(RuleCategory));
        public Array AvailableOperators => Enum.GetValues(typeof(RelationalOperator));

        public ConditionBuilderView()
        {
            InitializeComponent();

            RootCondition = new ConditionGroup { LogicOperator = LogicalOperator.And };
            this.DataContext = this;
            RuleItemsControl.ItemsSource = RootCondition.Children;
        }

        // PŘIDÁNÍ ŘÁDKU

        private void BtnAddRule_Click(object sender, RoutedEventArgs e)
        {
            RootCondition.Children.Add(new ConditionRule
            {
                Category = RuleCategory.File,
                Operator = RelationalOperator.Contains,
                TargetProperty = "FileName",
                Value = ""
            });
        }

        // SMAZÁNÍ ŘÁDKU
        private void BtnDeleteRule_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            if (button?.DataContext is ConditionRule ruleToDelete)
            {
                RootCondition.Children.Remove(ruleToDelete);
            }
        }

        // SPUŠTĚNÍ (HLEDAT)
        private async void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (RootCondition.Children.Count == 0)
            {
                MessageBox.Show("Přidej alespoň jednu podmínku pro hledání.", "Upozornění", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                using (var dbContext = new AppDbContext())
                {
                    var searchEngine = new SearchEngineService(dbContext);

                    var foundFiles = await searchEngine.ExecuteSearchAsync(RootCondition);
                    ResultsGrid.ItemsSource = foundFiles;
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
    }
}
