using MetroAppsList.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Windows;
using Windows.Management.Deployment;

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
                            //itemView.DisplayName = application.VisualElements.DisplayName;
                            //itemView.DisplayName = package.Id.Name;
                            itemView.DisplayName = TryExtractDisplayName(dir, package, application);
                            itemView.ForegroundText = application.VisualElements.ForegroundText;
                            itemView.DisplayIcon = ExtractDisplayIcon(dir, application);
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

        private string ExtractDisplayIcon(string dir, Application application)
        {
            var imageFile = Path.Combine(dir, application.VisualElements.Logo);
            if (File.Exists(imageFile))
                return imageFile;

            imageFile = Path.ChangeExtension(imageFile, "scale-100.png");
            if (File.Exists(imageFile))
                return imageFile;

            imageFile = Path.Combine(dir, "en-us", application.VisualElements.Logo); //TODO: How determine if culture parameter is necessary?
            if (File.Exists(imageFile))
                return imageFile;

            return Path.ChangeExtension(imageFile, "scale-100.png");
        }

        private string ExtractDisplayName(string dir, Windows.ApplicationModel.Package package, Application application)
        {
            var priPath = Path.Combine(dir, "resources.pri");
            Uri uri;
            if (!Uri.TryCreate(application.VisualElements.DisplayName, UriKind.Absolute, out uri))
                return application.VisualElements.DisplayName;

            var resource = string.Format("ms-resource://{0}/resources/{1}", package.Id.Name, uri.Segments.Last());
            var name = NativeMethods.ExtractStringFromPRIFile(priPath, resource);
            if (!string.IsNullOrWhiteSpace(name))
                return name;

            var res = string.Concat(uri.Segments.Skip(1));
            resource = string.Format("ms-resource://{0}/{1}", package.Id.Name, res);
            return NativeMethods.ExtractStringFromPRIFile(priPath, resource);
        }
    }
}
