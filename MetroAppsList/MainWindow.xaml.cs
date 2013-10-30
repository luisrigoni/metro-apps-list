using System.Windows.Controls;
using MetroAppsList.Win32;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Windows;
using Microsoft.Win32;
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
                            itemView.DisplayName = ExtractDisplayName(dir, package, application);
                            itemView.ForegroundText = application.VisualElements.ForegroundText;
                            //itemView.DisplayIcon = ExtractDisplayIcon(dir, application);
                            itemView.DisplayIcon = ExtractDisplayIcon2(dir, application);
                            itemView.IconBackground = application.VisualElements.BackgroundColor;
                            itemView.AppUserModelId = GetAppUserModelId(package.Id.FullName, application.Id);

                            Packages.Add(itemView);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }

        }

        public static string GetAppUserModelId(string packageFullName, string applicationId)
        {
            string str = string.Empty;
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(string.Format(
               @"SOFTWARE\Classes\ActivatableClasses\Package\{0}\Server\", packageFullName)))
            {
                if (key == null) return str;
                var appKeys = from k in key.GetSubKeyNames()
                              where k.StartsWith(applicationId)
                              select k;
                foreach (var appKey in appKeys)
                {
                    using (RegistryKey serverKey = key.OpenSubKey(appKey))
                    {
                        if (serverKey.GetValue("AppUserModelId") != null)
                        {
                            str = serverKey.GetValue("AppUserModelId").ToString();
                            serverKey.Close();
                            break;
                        }
                    }
                }
            }
            return str;
        } 

        private static string ExtractDisplayIcon2(string dir, Application application)
        {
            var logo = Path.Combine(dir, application.VisualElements.SmallLogo);
            if (File.Exists(logo))
                return logo;

            logo = Path.Combine(dir, Path.ChangeExtension(logo, "scale-100.png"));
            if (File.Exists(logo))
                return logo;

            var localized = Path.Combine(dir, "en-us", application.VisualElements.SmallLogo); //TODO: How determine if culture parameter is necessary?
            localized = Path.Combine(dir, Path.ChangeExtension(localized, "scale-100.png"));
            return localized;
        }

        private static string ExtractDisplayIcon(string dir, Application application)
        {
            var imageFile = Path.Combine(dir, application.VisualElements.Logo);
            var scaleImage = Path.ChangeExtension(imageFile, "scale-100.png");
            if (File.Exists(scaleImage))
                return scaleImage;

            if (File.Exists(imageFile))
                return imageFile;

            imageFile = Path.Combine(dir, "en-us", application.VisualElements.Logo); //TODO: How determine if culture parameter is necessary?
            if (File.Exists(imageFile))
                return imageFile;

            return Path.ChangeExtension(imageFile, "scale-100.png");
        }

        private static string ExtractDisplayName(string dir, Windows.ApplicationModel.Package package, Application application)
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

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = e.AddedItems[0] as PackageView;

            var appActiveManager = new NativeMethods.ApplicationActivationManager();
            uint pid;
            appActiveManager.ActivateApplication(item.AppUserModelId, null, NativeMethods.ActivateOptions.None, out pid);
        }
    }
}
