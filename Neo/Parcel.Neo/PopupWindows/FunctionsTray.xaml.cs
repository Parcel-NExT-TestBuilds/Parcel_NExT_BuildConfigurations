using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Parcel.Neo.Base.Framework;

namespace Parcel.Neo.PopupWindows
{
    public partial class FunctionsTray : Parcel.Neo.BaseWindow
    {
        public FunctionsTray(Window owner)
        {

            Owner = owner;
            InitializeComponent();

            // Toolbox setup
            Dictionary<string, ToolboxNodeExport[]> toolboxes = ToolboxIndexer.Toolboxes;
            PopulateToolboxItems(toolboxes);

            // GUI update
            NodeCountLabel.Content = $"Total Functions: {_availableNodes.Count}";

            // Focus on search bar
            SearchTextBox.Focus();
        }

        #region States
        private Dictionary<ToolboxNodeExport, string> _availableNodes; // From node to toolbox name mapping
        private Dictionary<SearchResult, ToolboxNodeExport> _searchResultLookup;
        #endregion

        #region View Properties
        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                SetField(ref _searchText, value);
                UpdateSearch(_searchText);
            }
        }
        public record SearchResult(string Label, string? Tooltip);
        private ObservableCollection<SearchResult> _searchResults;
        public ObservableCollection<SearchResult> SearchResults
        {
            get => _searchResults;
            set => SetField(ref _searchResults, value);
        }
        private Visibility _defaultCategoriesVisibility = Visibility.Visible;
        public Visibility DefaultCategoriesVisibility
        {
            get => _defaultCategoriesVisibility;
            set => SetField(ref _defaultCategoriesVisibility, value);
        }
        private Visibility _searchResultsVisibility = Visibility.Collapsed;
        public Visibility SearchResultsVisibility
        {
            get => _searchResultsVisibility;
            set => SetField(ref _searchResultsVisibility, value);
        }
        #endregion

        #region Routines
        private void AddMenuItem(ToolboxNodeExport? node, MenuItem toolboxMenu, string toolboxName)
        {
            // Seperator
            if (node == null)
                toolboxMenu.Items.Add(new Separator());
            // Button item
            else
            {
                MenuItem item = new() { Header = $"{node.Name}{(node.ArgumentsListSimple.Any() ? $"({node.ArgumentsListSimple})": string.Empty)}", Tag = node, ToolTip = node.Tooltip };
                item.Click += NodeMenuItemOnClick;
                toolboxMenu.Items.Add(item);
                
                _availableNodes.Add(node, toolboxName);
            }
        }
        private void UpdateSearch(string searchText)
        {
            Assembly[] ignoreAssemblies = [];
            _searchResultLookup = [];
            SearchResults = new ObservableCollection<SearchResult>(_availableNodes
                .Where(n => n.Key.Descriptor == null || !ignoreAssemblies.Contains(n.Key.Descriptor.Method.DeclaringType.Assembly)) // Ignore games
                .Where(n => n.Key.Name.Contains(searchText, StringComparison.CurrentCultureIgnoreCase))
                .Select(node =>
                {
                    string key = $"[{node.Value}] {node.Key.Name}{(node.Key.ArgumentsListFull.Any() ? $"({node.Key.ArgumentsListFull})" : string.Empty)}{(node.Key.HasReturnValue ? " -> " + string.Join(", ", node.Key.ReturnsList): string.Empty)}";
                    SearchResult result = new(key, node.Key.Tooltip);
                    _searchResultLookup[result] = node.Key;
                    return result;
                }));

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                DefaultCategoriesVisibility = Visibility.Collapsed;
                SearchResultsVisibility = Visibility.Visible;
                NodeCountLabel.Content = $"Found Matches: {SearchResults.Count}";
            }
            else
            {
                DefaultCategoriesVisibility = Visibility.Visible;
                SearchResultsVisibility = Visibility.Collapsed;
                NodeCountLabel.Content = $"Total Functions: {_availableNodes.Count}";
            }
        }
        private void PopulateToolboxItems(Dictionary<string, ToolboxNodeExport[]> toolboxes)
        {
            // TODO: The setup here need complete changes to conform to POS assembly formats
            _availableNodes = [];
                
            foreach ((string NodeName, ToolboxNodeExport[] Nodes) in toolboxes)
            {
                // Create menu instance; We are using a single menu for each toolbox because that's the only way to support vertical layout
                Menu toolboxMenu = new();
                MenuItem menuItem = new()
                {
                    Header = NodeName, 
                    Width = Width * 0.8,
                };
                toolboxMenu.Items.Add(menuItem);

                // Add to menu
                foreach (ToolboxNodeExport export in Nodes)
                    AddMenuItem(export, menuItem, NodeName);

                // Add menu to GUI
                ModulesListView.Items.Add(toolboxMenu);
            }
        }
        #endregion

        #region Interface
        public Action<ToolboxNodeExport> ItemSelectedAdditionalCallback { get; set; }
        #endregion

        #region GUI Events
        private void PopupTab_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            // TODO: Why do we have this? // Remark: It's so that it's possible to drag-move the popup panel when clicking on the title bar. Not super useful though.
            DragMove();
        }
        private void PopupTab_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Escape)
                Close();
        }
        private void NodeMenuItemOnClick(object sender, RoutedEventArgs e)
        {
            if (e.Source is not MenuItem item || item.Tag == null) return;
            
            ToolboxNodeExport? toolSelection = item.Tag as ToolboxNodeExport;
            ItemSelectedAdditionalCallback(toolSelection);
            Close();
        }
        private void SearchResultsListViewLabel_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            ItemSelectedAdditionalCallback(_searchResultLookup[(((Label) sender).DataContext as SearchResult)!]);
            Close();
        }
        private void SearchResultsListView_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && SearchResults.Count != 0)
            {
                ItemSelectedAdditionalCallback(_searchResultLookup[(SearchResult)((ListBox) sender).SelectedItem ?? SearchResults.First()]);
                Close();
                e.Handled = true;
            }
        }
        private void SearchTextBox_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && SearchResults.Count >= 1)
            {
                ToolboxNodeExport export = _searchResultLookup[SearchResults.First()];
                ItemSelectedAdditionalCallback(export);
                e.Handled = true;
                Close();
            }
            else if (e.Key == Key.Up || e.Key == Key.Down)
            {
                SearchResultsListView.Focus();
                e.Handled = true;
            }
        }
        private void NodesWindow_MouseLeave(object sender, MouseEventArgs e)
        {
            // Remark: There is good reason why we use MouseLeave to close the popup. 1) We have to close it somehow, 2) We cannot use Deactivated or LostFocus because LostFocus just never triggers and calling Close()/Hide() in deactivated deactivates parent window as well (making Neo lose focus)
            Close();
        }
        #endregion
    }
}