using CeaIndexer.Data;
using CeaIndexer.FilterModels;
using CeaIndexer.Helpers;
using CeaIndexer.Services;     
using CeaIndexer.ViewModels;
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

        public ConditionGroup RootCondition { get; set; }

        public Array AvailableCategories => Enum.GetValues(typeof(RuleCategory));
        public Array AvailableOperators => Enum.GetValues(typeof(RelationalOperator));

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


        private void BtnDeleteRule_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            if (button?.DataContext is ConditionRule ruleToDelete)
            {
                RootCondition.Children.Remove(ruleToDelete);
            }
        }


        private async void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (RootCondition.Children.Count == 0)
            {
                MessageBox.Show("Přidej alespoň jednu podmínku pro hledání.", "Upozornění", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }


            List<string> selectedQuantities = TreeHelper.GetSelectedQuantities(QuantityTree);

            Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                using (var dbContext = new AppDbContext())
                {
                    var searchEngine = new SearchEngineService(dbContext);

                    var foundFiles = await searchEngine.ExecuteSearchAsync(RootCondition, selectedQuantities);
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
