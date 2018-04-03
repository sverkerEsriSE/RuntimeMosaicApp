using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Rasters;
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
using System.Windows.Forms;

namespace RuntimeMosaicApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Map map = new Map(Basemap.CreateDarkGrayCanvasVector());
        private MosaicDatasetRaster mdr;
        private string geodatabasePath;
        private string selectedMosaicName;
        public MainWindow()
        {
            InitializeComponent();
            MyMapView.Map = map;
        }

        private void AddMosaicToCombo(string name)
        {
            MosaicDatasetCombo.Items.Add(name);
        }

        private void btnBrowserRasterPath_Click(object sender, RoutedEventArgs e)
        {
            using (var fb = new FolderBrowserDialog())
            {
                DialogResult res = fb.ShowDialog();
                if (res == System.Windows.Forms.DialogResult.OK)
                {
                    string pathname = fb.SelectedPath;
                    RasterPathTextBox.Text = pathname;
                    btnAddRasters.IsEnabled = true;
                }
            }
        }


        public void OnMosaicLoaded(object sender, EventArgs e)
        {
            var md = sender as MosaicDatasetRaster;
            Dispatcher.Invoke(() =>
            {
                debugListBox.Items.Add("Mosaic loaded with status: " + md.LoadStatus);
            });
            if (md.LoadStatus == Esri.ArcGISRuntime.LoadStatus.Loaded)
            {
                Dispatcher.Invoke(() => {
                    debugListBox.Items.Add("Mosaic RasterType: " + md.RasterType);
                    AddRasterStack.Visibility = Visibility.Visible;
                });
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    debugListBox.Items.Add(md.LoadStatus + ": " + md.LoadError);
                });
            }
        }

        private void btnAddRasters_Click(object sender, RoutedEventArgs e)
        {
            string filename = RasterPathTextBox.Text;
            AddRastersParameters addRastersParameters = new AddRastersParameters();
            addRastersParameters.InputDirectory = RasterPathTextBox.Text;
            mdr.AddRastersAsync(addRastersParameters).ContinueWith(t => Dispatcher.Invoke(() => { debugListBox.Items.Add("Rasters loaded"); }));
        }

        private void btnCreateOrOpenGeodatabase_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.AddExtension = true;
            openFileDialog.DefaultExt = ".geodatabase";
            openFileDialog.CheckFileExists = false;
            openFileDialog.Filter = "Runtime Geodatabase (*.geodatabase)|*.geodatabase";

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                geodatabasePath = openFileDialog.FileName;
                if (true == System.IO.File.Exists(geodatabasePath))
                {
                    if (MosaicDatasetRaster.GetNames(geodatabasePath).Count > 0)
                    {
                        MosaicDatasetCombo.Visibility = Visibility.Visible;
                        selectMD.Visibility = Visibility.Visible;
                        foreach (var name in MosaicDatasetRaster.GetNames(geodatabasePath))
                        {
                            AddMosaicToCombo(name);
                        }
                    }
                }
                else
                {
                    enterMDName.Visibility = Visibility.Visible;
                    MosaicDatasetNameTextBox.Visibility = Visibility.Visible;
                }
                btnCreateOrLoadMD.Visibility = Visibility.Visible;
            }
        }

        private void btnCreateOrLoadMD_Click(object sender, RoutedEventArgs e)
        {
            if (selectedMosaicName == null && MosaicDatasetNameTextBox.Text != "")
            {
                debugListBox.Items.Add("Creating new Mosaic Dataset and database");
                selectedMosaicName = MosaicDatasetNameTextBox.Text;
                mdr = MosaicDatasetRaster.Create(geodatabasePath, selectedMosaicName, new SpatialReference(3857));
                mdr.Loaded += new EventHandler<EventArgs>(this.OnMosaicLoaded);
                mdr.LoadAsync();
            }
            else
            {
                debugListBox.Items.Add("Loading MosaicDataset " + selectedMosaicName + " from " + geodatabasePath);
                mdr = new MosaicDatasetRaster(geodatabasePath, selectedMosaicName);
                mdr.Loaded += new EventHandler<EventArgs>(this.OnMosaicLoaded);
                mdr.LoadAsync();
            }
        }

        private void MosaicDatasetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedMosaicName = MosaicDatasetCombo.SelectedValue.ToString();
            btnCreateOrLoadMD.IsEnabled = true;
        }

        private void MosaicDatasetNameTextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (MosaicDatasetNameTextBox.Text.Length > 0)
            {
                btnCreateOrLoadMD.IsEnabled = true;
            }
            else
            {
                btnCreateOrLoadMD.IsEnabled = false;
            }
        }

        private void resetApp_Click(object sender, RoutedEventArgs e)
        {
            selectMD.Visibility = Visibility.Collapsed;
            MosaicDatasetCombo.SelectedItem = null;
            MosaicDatasetCombo.Visibility = Visibility.Collapsed;

            enterMDName.Visibility = Visibility.Collapsed;
            MosaicDatasetNameTextBox.Visibility = Visibility.Collapsed;
            MosaicDatasetNameTextBox.Text = "";

            btnCreateOrLoadMD.IsEnabled = false;
            btnCreateOrLoadMD.Visibility = Visibility.Collapsed;

            AddRasterStack.Visibility = Visibility.Collapsed;
            RasterPathTextBox.Text = "";
            debugListBox.Items.Clear();

            selectedMosaicName = null;
            mdr = null;
            geodatabasePath = null;
        }

        private void DisplayRaster_Click(object sender, RoutedEventArgs e)
        {
            if (mdr != null)
            {
                debugListBox.Items.Add("Displaying raster on map");
                RasterLayer rasterLayer = new RasterLayer(mdr);
                map.OperationalLayers.Add(rasterLayer);
            }
            else
            {
                debugListBox.Items.Add("No MosaicDatasetRaster is loaded");
            }
        }
    }
}
