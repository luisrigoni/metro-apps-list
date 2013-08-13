using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
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
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.Resources.Core;
using Windows.Management.Deployment;
using Windows.Storage;

namespace MetroAppsList
{
    public partial class MainWindow
    {
        #region ObservableCollection<PackageView> Packages { get; set; }
        public ObservableCollection<PackageView> Packages
        {
            get { return (ObservableCollection<PackageView>)GetValue(PackagesProperty); }
            set { SetValue(PackagesProperty, value); }
        }

        public static readonly DependencyProperty PackagesProperty = DependencyProperty.Register(
            "Packages",
            typeof(ObservableCollection<PackageView>),
            typeof(MainWindow),
            new PropertyMetadata(new ObservableCollection<PackageView>()));
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            try
            {
                LoadApps();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void LoadApps()
        {
            var packageManager = new PackageManager();
            var userSecurityId = WindowsIdentity.GetCurrent().User.Value;
            var packages = packageManager.FindPackagesForUser(userSecurityId);
            foreach (var package in packages)
            {
                try
                {
                    var dir = package.InstalledLocation.Path;
                    //var name = package.Id.Name;
                    var file = Path.Combine(dir, "AppxManifest.xml");
                    var obj = SerializationExtensions.DeSerializeObject<Package>(file);

                    if (obj.Applications == null)
                        continue;

                    foreach (var application in obj.Applications)
                    {
                        try
                        {
                            var itemView = new PackageView();

                            itemView.DisplayName = application.VisualElements.DisplayName;
                            //itemView.DisplayName = package.Id.Name;
                            itemView.ForegroundText = application.VisualElements.ForegroundText;
                            itemView.DisplayIcon = Path.Combine(dir, application.VisualElements.Logo);
                            itemView.IconBackground = application.VisualElements.BackgroundColor;

                            Packages.Add(itemView);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }

        }
    }
}
