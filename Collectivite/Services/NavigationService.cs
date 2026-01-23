using System;
using System.Windows.Controls;

namespace Collectivite.Services
{
    public class NavigationService
    {
        private Frame? _mainFrame;
        private static NavigationService? _instance;

        public static NavigationService Instance => _instance ?? (_instance = new NavigationService());

        public Frame MainFrame
        {
            get => _mainFrame ?? throw new InvalidOperationException("MainFrame n'est pas initialisé");
            set => _mainFrame = value;
        }

        public void NavigateTo(Page page)
        {
            if (_mainFrame != null)
            {
                _mainFrame.Navigate(page);
            }
        }

        public void NavigateTo(Type pageType)
        {
            if (_mainFrame != null && pageType.IsSubclassOf(typeof(Page)))
            {
                var page = Activator.CreateInstance(pageType) as Page;
                _mainFrame.Navigate(page);
            }
        }

        public void GoBack()
        {
            if (_mainFrame?.CanGoBack == true)
            {
                _mainFrame.GoBack();
            }
        }

        public void GoForward()
        {
            if (_mainFrame?.CanGoForward == true)
            {
                _mainFrame.GoForward();
            }
        }

        public void ClearHistory()
        {
            if (_mainFrame != null)
            {
                while (_mainFrame.CanGoBack)
                {
                    _mainFrame.RemoveBackEntry();
                }
            }
        }
    }
}
