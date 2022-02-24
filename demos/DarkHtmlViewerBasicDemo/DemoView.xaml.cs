using DarkHelpers;
using DarkHelpers.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace DarkHtmlViewerBasicDemo
{
    public partial class DemoView : Window
    {
        private DemoItem _currentItem;

        public ICommand LoadItemCommand { get; }
        public ICommand HandleLinkClickCommand { get; }

        public DarkObservableCollection<DemoItem> Items { get; } = new DarkObservableCollection<DemoItem>();

        public DemoView()
        {
            InitializeComponent();

            LoadItemCommand = new DarkCommand<DemoItem>(LoadItem);
            HandleLinkClickCommand = new DarkCommand<string>(HandleLinkClick);

            DataContext = this;
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            var items = GenereateItems();
            Items.AddRange(items);

            var firstItem = items.First();
            LoadItem(firstItem);
        }

        private void LoadItem(DemoItem item)
        {
            _currentItem = item;
            darkHtmlViewer.LoadCommand.TryExecute(item.Html);
        }

        private IEnumerable<DemoItem> GenereateItems()
        {
            return new List<DemoItem>
            {
                new DemoItem
                {
                    Title = "Home",
                    ItemCode = "home",
                    Html = LoadHtml("home")
                },
                new DemoItem
                {
                    Title = "Super cool looking code!",
                    ItemCode = "page1",
                    Html = LoadHtml("page1")
                },new DemoItem
                {
                    Title = "What the hell are NFTs?",
                    ItemCode = "page2",
                    Html = LoadHtml("page2")
                },
            };
        }

        private string LoadHtml(string itemCode)
        {
            var rawHtml = itemCode switch
            {
                "home" => Properties.Resources.home,
                "page1" => Properties.Resources.page1,
                "page2" => Properties.Resources.page2,
                _ => null
            };

            var preparedHtml = rawHtml.Replace("{htmlResDir}", "https://darkassets.local/");

            return preparedHtml;
        }

        private static readonly Regex ItemCodeRegex = new Regex(@"(?<itemCode>[a-zA-Z0-9]+)-.+");

        private void HandleLinkClick(string link)
        {
            var itemCodeMatch = ItemCodeRegex.Match(link);
            if (!itemCodeMatch.Success)
            {
                return;
            }

            var itemCode = itemCodeMatch.Groups["itemCode"].Value;
            var item = Items.FirstOrDefault(a => a.ItemCode == itemCode);
            if (item is null)
            {
                return;
            }

            if (item == _currentItem)
            {
                darkHtmlViewer.ScrollCommand.TryExecute(link);
                return;
            }

            darkHtmlViewer.ScrollOnNextLoadCommand.TryExecute(link);

            LoadItem(item);
        }
    }
}
